# Plan — Génération de la grille de régie (epic #6, stories #25 à #28)

> Règles et urgences : [PRODUCT.md](../../PRODUCT.md), section « Motifs de la grille de régie ». Critère de succès : [mvp.md](../mvp.md).
> Chaque sous-tâche tient en 30 minutes au plus. Ordre imposé : tests du domaine → domaine → page Razor → test bUnit.
> Code de régie dans `src/TourneeVeto.Domain/Regie` (avec `RegieAction` et `Urgency`) ; `today` toujours en paramètre `DateOnly`.

## Décisions prises (écarts tranchés le 2026-10-08)

| # | Décision |
| --- | --- |
| 1 | DG : J30 à J45 « à faire » (Info) ; au-delà de J45, même motif « DG en retard » (Warning). La vache ne sort jamais de la grille. |
| 2 | Post-vêlage conservé : J21 à J35 (Info). |
| 3 | Vêlage prévu (IA + 280 j) dans 14 jours ou moins, date dépassée comprise : motif `CalvingSoon` (Warning). |
| 4 | Seuils dans `RegieThresholds` (domaine), valeurs par défaut de PRODUCT.md ; seuil invalide → valeurs par défaut et indicateur `IsFallback`. Pas de fichier JSON dans le POC. |
| 5 | Urgences : CCS élevé → Urgent ; vêlage prévu, vache vide, DG en retard → Warning ; DG, post-vêlage → Info ; tarissement → Ok. Plusieurs motifs → urgence la plus élevée. |
| 6 | CCS en milliers : 200 absent, 201 présent. |
| 7 | Emplacement : `Domain/Regie` (et non `Domain/Herd`). |
| 8 | `DemoData` complété (sous-tâche 2.4) pour fournir au moins une vache par motif, dont « vache vide » et « DG en retard ». |
| 9 | `CowCard` reçoit un paramètre d'anomalie (sous-tâche 3.4). |
| 10 | Test Playwright du parcours : tâche 5, après le lot 3 (crée `tests/TourneeVeto.E2E`). |
| 11 | La page lit ses données via `DemoDataSeeder` + `IVisitRepository`, jamais directement via `DemoData`. |

## Tâche 1 — Tests du domaine (en échec)

| # | Fichiers | Scénario | Preuve |
| --- | --- | --- | --- |
| 1.1 | `Domain/Regie/DailyActions.cs`, `RegieItem.cs`, `RegieThresholds.cs`, `RegieAnomaly.cs` (signatures seules) ; `tests/TourneeVeto.Tests/Domain/Regie/PregnancyCheckRuleTests.cs` | DG J29 absent, J30 et J45 Info, J46 Warning ; vache `Pregnant` exclue ; IA future → « Date incohérente » (#25) | `dotnet test --filter "FullyQualifiedName~Domain.Regie"` : compile, en échec |
| 1.2 | `DryOffRuleTests.cs`, `CalvingSoonRuleTests.cs` | Tarissement J-61 absent, J-60 présent, vache tarie exclue ; vêlage prévu J-15 absent, J-14 présent ; gestante sans IA → « Date d'IA manquante » (#26) | même filtre : en échec |
| 1.3 | `OpenCowRuleTests.cs`, `PostCalvingRuleTests.cs` | Vache vide J60 absente, J61 présente ; post-vêlage J20 absent, J21 et J35 présents, J36 absent ; vêlage futur → « Date incohérente » ; 2 motifs → une seule ligne, urgence max (#27) | même filtre : en échec |
| 1.4 | `HighSccRuleTests.cs`, `RegieThresholdsTests.cs`, `DailyActionsDemoDataTests.cs` | CCS 200 absent, 201 Urgent ; seuil DG à 35 → J34 absent, J35 présent ; seuil négatif → valeurs par défaut (#28) ; au moins une vache par motif dans `DemoData.Generate(today, 42)` | même filtre : en échec |

## Tâche 2 — Domaine (passage au vert)

| # | Fichiers | Scénario | Preuve |
| --- | --- | --- | --- |
| 2.1 | `RegieThresholds.cs` (`Default`, `Validate`) | Tests de seuils de 1.4 | `dotnet test --filter RegieThresholds` : vert |
| 2.2 | `DailyActions.cs` (DG, tarissement, vêlage prévu), `RegieItem.cs` (vache, motifs, urgence, jours jusqu'à l'échéance ou depuis l'événement, anomalie) | Tests de 1.1 et 1.2 | `dotnet test --filter "FullyQualifiedName~Domain.Regie"` : 1.1 et 1.2 au vert |
| 2.3 | `DailyActions.cs` (vache vide, post-vêlage, CCS, anomalies, regroupement, tri par urgence) | Tests de 1.3 et 1.4 (hors DemoData) | même filtre : vert sauf le test DemoData |
| 2.4 | `DemoData.cs`, `tests/TourneeVeto.Tests/Domain/DemoDataTests.cs` | Au moins une vache par motif, quantités exactes conservées | `dotnet test` : tout au vert |

## Tâche 3 — Page `Regie.razor` (skill create-component)

| # | Fichiers | Scénario | Preuve |
| --- | --- | --- | --- |
| 3.1 | `Ui/Pages/Regie.razor`, `Regie.razor.css`, `tests/TourneeVeto.Tests/Ui/RegieTests.cs` (squelette), route `/regie/{FarmId}` | Structure conforme aux règles Razor | `dotnet run .github/skills/create-component/check.cs -- Regie` puis `dotnet build` |
| 3.2 | `Regie.razor` : `DemoDataSeeder`, `IVisitRepository.GetCowsByFarmAsync`, `TimeProvider`, `DailyActions` → `CowCard` ; états chargement, vide, erreur (`role="alert"`) | Grille d'une ferme lue dans IndexedDB | `dotnet build` |
| 3.3 | `Regie.razor`, `.css`, `Home.razor` : filtres par motif (boutons `aria-pressed`, nombre par filtre, 44 px), lien depuis chaque visite | Filtrage d'affichage uniquement | `dotnet build` et check.cs |
| 3.4 | `Components/CowCard.razor`, `.css`, `tests/TourneeVeto.Tests/Ui/CowCardTests.cs` | Anomalie affichée en texte (« Date incohérente », « Date d'IA manquante ») | `dotnet test --filter CowCard` |

## Tâche 4 — Test bUnit de la page

| # | Fichiers | Scénario | Preuve |
| --- | --- | --- | --- |
| 4.1 | `tests/TourneeVeto.Tests/Ui/RegieTests.cs` (module JS simulé, `FakeTimeProvider`, `IAsyncLifetime`) | Filtre « DG » ; état vide « Aucune action de régie pour cette visite » ; erreur de stockage | `dotnet test --filter RegieTests` puis `dotnet test` |

## Tâche 5 — Parcours Playwright (après le lot 3)

| # | Fichiers | Scénario | Preuve |
| --- | --- | --- | --- |
| 5.1 | `tests/TourneeVeto.E2E` (Playwright for .NET, sur la sortie de `dotnet publish`) | Tournée → grille de régie hors ligne, filtre DG | `dotnet test tests/TourneeVeto.E2E` |

## Lots

- **Lot 1** : tâches 1 et 2 (1.1 à 2.4) ; montrer les tests en échec, puis le code ; `dotnet test`. Commit `feat(regie): règles du domaine`.
- **Lot 2** : tâche 3 (3.1 à 3.4), skill create-component, données via `IVisitRepository` ; `dotnet build`. Commit.
- **Lot 3** : tâche 4 ; `dotnet test`. Commit, puis fermeture des stories #25 à #28 et de l'epic #6.
