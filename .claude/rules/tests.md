---
paths:
  - "tests/**/*.cs"
---
<!-- Copie de .github/instructions/tests.instructions.md (les imports @ chargeraient la règle partout). Modifier d'abord le fichier Copilot ; ArchitectureTests vérifie que les deux restent identiques. -->

- Figer « aujourd'hui » avec FakeTimeProvider (Microsoft.Extensions.TimeProvider.Testing) dans tout test qui dépend de la date.
- Noms de test en français, une classe par règle métier (« Tarissement_est_proposé_60_jours_avant_le_vêlage »).
- Pas de mock du domaine : il est pur, on le teste directement.
- bUnit : BunitContext ; déclarer chaque appel JS (JSInterop.SetupModule) ; jamais de vraie base IndexedDB.
- Données : DemoData.Generate(today, seed) avec un seed fixe.
