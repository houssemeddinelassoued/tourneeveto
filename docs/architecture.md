# TournéeVéto — Architecture (C4 niveaux 1 et 2)

> Contexte produit : [PRODUCT.md](../PRODUCT.md) · périmètre : [mvp.md](mvp.md).
> Décisions : [ADR 0001 stockage](adr/0001-stockage.md) · [ADR 0002 structure de la solution](adr/0002-structure-solution.md) · [ADR 0003 hébergement](adr/0003-hebergement.md).

Notation : `flowchart` Mermaid aux conventions C4 (type de l'élément entre crochets). Les syntaxes `C4Context` et `C4Container` de Mermaid sont encore expérimentales et leur rendu chevauche souvent les éléments sur GitHub.

## Niveau 1 — Contexte système

```mermaid
flowchart TB
    vet["Médecin vétérinaire<br/>[Personne]<br/>Prépare la tournée, réalise les visites<br/>sur tablette, sans réseau"]
    prod["Producteur laitier<br/>[Personne]<br/>Reçoit le rapport de visite"]
    sys["TournéeVéto<br/>[Système logiciel — PWA]<br/>Tournée, fiche ferme, grille de régie,<br/>biosécurité, photos, rapport de visite"]
    gh["GitHub Pages<br/>[Système externe]<br/>Héberge les fichiers statiques<br/>sous /tourneeveto/"]
    none["Aucune intégration externe dans le POC<br/>(Lactanet, ATQ, logiciels de troupeau<br/>ou de clinique)"]

    vet -->|"Utilise sur tablette, en ligne ou hors ligne"| sys
    sys -->|"Rapport imprimé ou PDF (remis par le vétérinaire)"| prod
    sys -->|"Télécharge l'application et ses mises à jour (HTTPS)"| gh
    sys ~~~ none

    classDef person fill:#08427b,color:#fff,stroke:#052e56
    classDef system fill:#1168bd,color:#fff,stroke:#0b4884
    classDef external fill:#999,color:#fff,stroke:#6b6b6b
    classDef note fill:#fff,color:#333,stroke:#999,stroke-dasharray:5 5
    class vet,prod person
    class sys system
    class gh external
    class none note
```

## Niveau 2 — Conteneurs

```mermaid
flowchart TB
    vet["Médecin vétérinaire<br/>[Personne]"]
    prod["Producteur laitier<br/>[Personne]"]

    subgraph tablette["Navigateur de la tablette — fonctionne hors ligne"]
        subgraph app["Application TournéeVéto [Blazor WebAssembly autonome, .NET 10]"]
            host["TourneeVeto.Web<br/>[Hôte Blazor WebAssembly]<br/>Démarrage, routeur, injection de dépendances,<br/>service worker, manifeste"]
            ui["TourneeVeto.Ui<br/>[Razor Class Library]<br/>Pages, composants, données de démo JSON"]
            adapter["Stockage IndexedDB<br/>[TourneeVeto.Ui/Data + visitStore.js]<br/>IVisitRepository, IndexedDbVisitRepository"]
            domain["TourneeVeto.Domain<br/>[Bibliothèque .NET]<br/>Règles de régie, score biosécurité<br/>(aucun accès au stockage)"]
        end
        idb[("IndexedDB<br/>[Base du navigateur]<br/>Fermes, vaches, visites, saisies, photos")]
        sw["Service worker<br/>[JavaScript]<br/>Mise en cache, fonctionnement hors ligne,<br/>détection des mises à jour"]
        cache[("Cache Storage<br/>[Cache du navigateur]<br/>Fichiers de l'application et JSON")]
    end

    subgraph github["GitHub"]
        ci["Workflow de publication<br/>[GitHub Actions]<br/>Build, tests, réécriture du base href,<br/>404.html, .nojekyll"]
        pages["Site statique<br/>[GitHub Pages, /tourneeveto/]<br/>_framework, wwwroot, _content"]
    end

    futur["Hôtes WPF et MAUI — futur<br/>[Blazor Hybrid, WebView2 / WebView MAUI]<br/>Remplacent uniquement TourneeVeto.Web"]

    vet -->|"Utilise (tactile)"| ui
    host -->|"Héberge et route"| ui
    ui -->|"Appelle les services métier"| domain
    ui -->|"Lit et écrit via IVisitRepository"| adapter
    adapter -->|"Lit et écrit (JS interop)"| idb
    host -->|"Requêtes de fichiers"| sw
    sw -->|"Sert depuis le cache"| cache
    sw -->|"Télécharge en ligne (HTTPS)"| pages
    ci -->|"Publie à chaque push sur main"| pages
    ui -->|"Impression navigateur (Lettre)"| prod
    futur -.->|"Réutilise UI, stockage IndexedDB et Domain"| ui

    classDef person fill:#08427b,color:#fff,stroke:#052e56
    classDef container fill:#438dd5,color:#fff,stroke:#2e6295
    classDef external fill:#999,color:#fff,stroke:#6b6b6b
    classDef future fill:#fff,color:#333,stroke:#438dd5,stroke-dasharray:5 5
    class vet,prod person
    class host,ui,adapter,domain,sw,idb,cache container
    class ci,pages external
    class futur future
```

Au sens strict, l'application Blazor WebAssembly est un seul conteneur. Ses projets (niveau 3, composants) sont montrés à l'intérieur pour rendre visible la frontière de réutilisation : seul `TourneeVeto.Web` change pour WPF et MAUI.

## Rôle de chaque conteneur

- **TourneeVeto.Web** : hôte propre au navigateur (routeur, injection de dépendances, service worker, manifeste). C'est le seul projet remplacé par un hôte WPF ou MAUI.
- **TourneeVeto.Ui** : pages, composants et données de démo JSON, partagés tels quels avec les futurs hôtes Blazor Hybrid.
- **Stockage IndexedDB** : interface `IVisitRepository` et implémentation `IndexedDbVisitRepository` (TourneeVeto.Ui/Data), via le module isolé `visitStore.js` (skill indexeddb-interop). Il vit dans la Razor Class Library, car IndexedDB existe aussi dans WebView2 et dans les WebView de MAUI.
- **TourneeVeto.Domain** : règles métier pures, sans accès au stockage ni dépendance à l'interface ni au navigateur ; testé par xUnit.
- **IndexedDB** : seule source de vérité des données sur l'appareil, photos comprises (en `Blob`). Rien n'est copié ailleurs.
- **Service worker** : met l'application en cache après le 1er chargement, la sert hors ligne et signale les nouvelles versions sans les appliquer d'office.
- **Cache Storage** : copie locale des fichiers `_framework`, des ressources `_content` et des JSON (données de démo, seuils, questionnaire de biosécurité).
- **Site statique (GitHub Pages)** : unique point de distribution, sous `/tourneeveto/`. Il ne reçoit aucune donnée métier.
- **Workflow de publication (GitHub Actions)** : compile, teste, réécrit le base href, crée `404.html` et `.nojekyll`, puis publie, uniquement si les tests passent.
- **Hôtes WPF et MAUI (futur)** : hors POC. Ils remplacent `TourneeVeto.Web` et réutilisent tout le reste.
