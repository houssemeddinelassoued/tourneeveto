---
statut: proposé
date: 2026-10-08
décideurs: Product Owner, équipe de développement .NET
consultés: agent architect
---

# ADR 0002 — Comment structurer la solution .NET ?

## Contexte et énoncé du problème

Le POC est une application Blazor WebAssembly réalisée en 5 jours. Ses écrans seront réutilisés dans des hôtes WPF et MAUI (Blazor Hybrid). Les règles métier doivent être testables sans navigateur (epic 1), et le stockage IndexedDB fonctionne dans les trois hôtes ([ADR 0001](0001-stockage.md)).

Comment découper la solution pour que seul l'hôte change d'une plateforme à l'autre, sans alourdir un POC de 5 jours ?

## Facteurs de décision

- Seul l'hôte doit changer pour WPF et MAUI.
- Règles métier testables par xUnit, sans navigateur.
- Peu de projets à créer et à câbler en 5 jours.
- Dépendances simples à vérifier automatiquement.

## Options considérées

1. **Un seul projet Blazor WebAssembly** : règles, écrans et stockage au même endroit.
2. **Domain + Razor Class Library + hôte Web** : 3 projets.
3. **Architecture en couches complète** : Domain, Application, Infrastructure, UI, hôte.

| Critère | 1. Projet unique | 2. Domain + RCL + hôte | 3. Couches complètes |
| --- | --- | --- | --- |
| Réutilisation WPF et MAUI | Non : tout est à extraire plus tard. | Oui : seul l'hôte est remplacé. | Oui. |
| Tests du Domain sans navigateur | Difficile : les règles côtoient les composants. | Oui. | Oui. |
| Coût en 5 jours | Minimal. | Faible (3 projets). | Élevé (5 projets, beaucoup de câblage). |
| Contrôle des dépendances | Aucun. | 2 règles simples. | Nombreuses règles. |

## Décision

**Option retenue : 2. Domain + Razor Class Library + hôte Web. Seul l'hôte change pour WPF et MAUI.**

```text
TourneeVeto.slnx
global.json             SDK .NET 10.0.x (rollForward latestFeature)
Directory.Build.props   Nullable, ImplicitUsings, TreatWarningsAsErrors pour tous les projets
src/
  TourneeVeto.Domain/   Bibliothèque .NET : entités, règles de régie, score biosécurité, IVisitRepository
  TourneeVeto.Ui/       Razor Class Library : pages, mise en page, composants, adaptateur IndexedDB
                        (C# + wwwroot/js/storage.js), données de démo et configuration JSON
  TourneeVeto.Web/      Hôte Blazor WebAssembly autonome (PWA) : Program.cs, App.razor (routeur),
                        index.html, service worker, manifeste
tests/
  TourneeVeto.Tests/    xUnit + bUnit : règles du Domain, composants de l'Ui, tests d'architecture
```

Un seul projet de tests suffit pour le POC. Les tests Playwright for .NET (hors ligne, tablette, publication) iront dans un projet `tests/TourneeVeto.E2E` créé avec le premier test de parcours, car ils s'exécutent sur la sortie de `dotnet publish`.

Règles de dépendance : `Web → Ui → Domain` ; `Tests → Domain, Ui`. Le Domain ne dépend de rien. Les futurs `TourneeVeto.Wpf` et `TourneeVeto.Maui` (BlazorWebView) référenceront `TourneeVeto.Ui`, exactement comme `TourneeVeto.Web`.

- Le routeur de l'hôte déclare l'assembly de l'Ui (`AdditionalAssemblies`) : toutes les pages `@page`, y compris `NotFound`, et la mise en page vivent dans `TourneeVeto.Ui`.
- Le module JS de stockage est servi comme ressource statique de la RCL (`_content/TourneeVeto.Ui/js/storage.js`), chemin identique dans les WebView WPF et MAUI.
- Ce qui n'existe que dans le navigateur (service worker, indicateur « prête hors ligne », manifeste) reste dans l'hôte Web. L'UI y accède par une interface (ex. `IOfflineStatus`) que chaque hôte implémente.

### Conséquences

#### Positives

- Passer à WPF ou MAUI revient à écrire un nouvel hôte (quelques fichiers), sans toucher aux écrans, au stockage ni aux règles.
- Domain testé par xUnit sans navigateur ; composants testés par bUnit.
- 3 projets seulement, créés le premier jour.

#### Négatives

- Discipline nécessaire : rien de métier dans l'hôte, rien de propre au navigateur dans l'UI en dehors du stockage IndexedDB. Les tests d'architecture le vérifient.
- Les fonctions propres au navigateur demandent une interface et une implémentation par hôte.
- L'impression du rapport (`window.print`) devra être revalidée dans les WebView MAUI, où elle n'est pas garantie (notamment sous iOS).

### Confirmation

Tests d'architecture dans la CI (critère de l'epic 1, [mvp.md](../mvp.md)), dans `tests/TourneeVeto.Tests/ArchitectureTests.cs` :

- `TourneeVeto.Domain` ne référence ni `Microsoft.AspNetCore.Components` ni `Microsoft.JSInterop` ;
- `TourneeVeto.Web` ne contient aucun composant routable (`@page`).

## Conditions de révision

1. Les écrans WPF ou MAUI doivent diverger fortement de ceux du web : extraire une couche de présentation partagée et des vues par hôte.
2. Le Domain grossit au point de mélanger cas d'usage et règles : extraire un projet `TourneeVeto.Application`.
3. Le stockage doit différer selon l'hôte (ex. SQLite natif) : sortir l'adaptateur IndexedDB de la RCL vers un projet `TourneeVeto.Storage.IndexedDb`.
