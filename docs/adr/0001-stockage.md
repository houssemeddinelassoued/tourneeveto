---
statut: proposé
date: 2026-10-08
décideurs: Product Owner, équipe de développement .NET
consultés: agent architect
---

# ADR 0001 — Où stocker les visites, le troupeau et les photos dans le navigateur ?

## Contexte et énoncé du problème

TournéeVéto est une PWA Blazor WebAssembly (.NET 10) sans backend ni compte ([PRODUCT.md](../../PRODUCT.md)). Une visite complète se fait hors ligne sur tablette ; les données n'existent que sur l'appareil. Les écrans seront réutilisés dans des hôtes WPF et MAUI (Blazor Hybrid, WebView).

Volumes à stocker :

- **troupeau** : 5 fermes fictives, au moins 300 vaches et leurs événements (vêlages, IA, DG, CCS), soit quelques centaines de Ko ;
- **visites** : résultats de régie, biosécurité, recommandations, avec un enregistrement automatique en moins de 1 s (story 7.3) ;
- **photos** (Should, [mvp.md](../mvp.md)) : environ 200 à 400 Ko par photo redimensionnée à 1600 px ; 10 photos sur 5 visites représentent 10 à 20 Mo.

## Facteurs de décision

- Capacité suffisante pour les photos.
- Requêtes de la tournée et de la grille de régie : par ferme, par date, tri par date.
- Écriture fiable de chaque saisie.
- Poids ajouté au premier chargement.
- Maturité, testabilité par une équipe .NET.
- Fonctionnement identique dans le navigateur, WPF (WebView2) et MAUI.

## Options considérées

1. **localStorage** via Blazored.LocalStorage.
2. **IndexedDB** via un petit module JavaScript appelé par JS interop.
3. **EF Core + SQLite compilé en WebAssembly** via un paquet communautaire (ex. Bit.Besql).

| Critère | 1. localStorage | 2. IndexedDB + module JS | 3. EF Core + SQLite WASM |
| --- | --- | --- | --- |
| **Limite de stockage (photos comprises)** | Environ 5 Mo par origine, texte uniquement : photos en Base64 (+33 %). Une visite avec photos peut saturer le quota. | Quota de l'origine : jusqu'à environ 60 % du disque (Chrome, Edge), 10 % du disque et 10 Gio au plus (Firefox, mode best-effort), ordre de grandeur comparable sous Safari 17+ (*à vérifier sur la tablette cible*). Photos en `Blob` natif. | Base chargée en mémoire WebAssembly puis copiée vers le stockage du navigateur. Limite pratique : la RAM de la tablette, photos comprises. |
| **Requêtes (index, tri par date)** | Aucune : clé-valeur, filtres et tris en C# après lecture complète. | Index par object store, requêtes par plage (`IDBKeyRange`), parcours triés par date. Pas de jointures : les filtres combinés se font en C#. | SQL complet et LINQ : index, jointures, tri, pagination. |
| **Poids ajouté au téléchargement initial** | Quelques dizaines de Ko. | Quelques Ko (module maison). | Plusieurs Mo : EF Core, fournisseur SQLite et binaire `e_sqlite3` en WebAssembly (*à mesurer*, ordre de grandeur 2 à 5 Mo compressés). Workload `wasm-tools` obligatoire en CI. |
| **Maturité** | API standard ; paquet communautaire répandu et stable. | API standard W3C, prise en charge par tous les navigateurs cibles. La maturité du module dépend de notre code, à garder minimal. | Non pris en charge officiellement par Microsoft en WebAssembly ; paquets peu adoptés, un seul mainteneur ; avertissements de trimming. |
| **Facilité de test** | Très simple : faux `ILocalStorageService`. | Domain pur testé directement (xUnit) ; repository testé avec bUnit (`JSInterop.SetupModule`, sans vraie base) ; module JS testé avec Playwright, déjà prévu. | Logique testable avec SQLite en mémoire (xUnit) ; la persistance WebAssembly ne se teste que dans un navigateur. |
| **WebView (WPF, MAUI)** | Disponible, mais avec le même quota. | **Disponible dans WebView2 (WPF, MAUI Windows), WKWebView (MAUI iOS et macOS) et Android WebView : le même code fonctionne dans les trois hôtes.** | En Blazor Hybrid, il faudrait une autre configuration (SQLite natif) : deux variantes à maintenir. |

## Décision

**Option retenue : 2. IndexedDB via un petit module JavaScript appelé par JS interop, derrière l'interface C# `IVisitRepository` de `TourneeVeto.Ui/Data` (implémentation `IndexedDbVisitRepository`, enregistrée en Scoped).**

> **Bonne nouvelle :** IndexedDB existe aussi dans les WebView de WPF (WebView2) et de MAUI. Le même module JS et le même adaptateur C# fonctionnent donc dans les trois hôtes (navigateur, WPF, MAUI), sans réécrire le stockage.

Modalités :

- Modèle détaillé et exemples qui fonctionnent : skill `indexeddb-interop` (.github/skills/indexeddb-interop).
- Un module ES unique (`wwwroot/js/visitStore.js` de la Razor Class Library, voir [ADR 0002](0002-structure-solution.md)), importé à la demande via `IJSObjectReference` et libéré dans `DisposeAsync`, sans dépendance externe.
- Base « tourneeveto » ; object stores `farms`, `visits` (index `farmId`, `date`), `cows` (clé `[farmId, id]` depuis la version 2 du schéma, index `farmId`), `photos` (index `visitId`) ; jeu de démonstration chargé en une transaction, seulement si la base est vide.
- `QuotaExceededError` et base indisponible remontent en `StorageUnavailableException`, affichée par l'interface.
- Photos stockées en `Blob` et redimensionnées côté JS, sans passer par .NET ; le Domain ne manipule que leur identifiant.
- Schéma versionné (`onupgradeneeded`), une migration par version.
- Demande de stockage persistant (`navigator.storage.persist()`) au premier lancement.

### Conséquences

#### Positives

- Même code de stockage dans le navigateur, WPF et MAUI.
- Capacité suffisante pour des centaines de photos, sans Base64.
- Premier chargement presque inchangé (quelques Ko), CI sans workload natif.
- Tournée du jour et grille de régie lues par index, sans charger tout le troupeau.
- Domain pur, sans accès au stockage : testable sans navigateur ; le repository se teste avec bUnit, sans vraie base.

#### Négatives

- **Les données restent sur un seul appareil** : pas de partage entre tablette et ordinateur, pas de copie de sauvegarde. D'où l'export/import JSON prévu en V2 ([mvp.md](../mvp.md)).
- **Un peu de JavaScript à maintenir** dans une équipe .NET, avec une API IndexedDB verbeuse.
- Pas de SQL : les filtres combinés se font en C#, et les migrations de schéma s'écrivent à la main.
- Le navigateur peut effacer les données (stockage best-effort, purge Safari après 7 jours sans visite pour un site non installé). Cette limite vaut pour toutes les options.

### Confirmation

- Test Playwright : 3 résultats saisis hors ligne, onglet fermé puis rouvert, 3 résultats sur 3 retrouvés (epic 7).
- Test Playwright : 50 photos de 300 Ko enregistrées puis relues.
- Test d'architecture : le Domain ne référence pas `Microsoft.JSInterop` (epic 1).

## Conditions de révision

1. Le produit doit gérer des milliers de vaches par ferme ou des statistiques transversales que les index ne couvrent plus : réévaluer l'option 3.
2. Microsoft prend officiellement en charge EF Core et SQLite dans Blazor WebAssembly.
3. La synchronisation entre appareils devient un besoin : le stockage local ne sera plus qu'un cache, et l'architecture sans backend est à revoir.
4. Le module JS dépasse environ 300 lignes ou accumule des anomalies : adopter une surcouche éprouvée (`idb`, Dexie.js).
5. Des données disparaissent sur les tablettes cibles malgré le stockage persistant : étudier l'Origin Private File System (OPFS) pour les photos et avancer l'export/import.
