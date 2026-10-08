---
paths:
  - "src/TourneeVeto.Domain/**"
---
<!-- Copie de .github/instructions/domain.instructions.md (les imports @ chargeraient la règle partout). Modifier d'abord le fichier Copilot ; ArchitectureTests vérifie que les deux restent identiques. -->

- C# pur : aucune référence à Blazor, Microsoft.JSInterop ni aux API du navigateur.
- Recevoir « aujourd'hui » en paramètre (DateOnly) ; jamais DateTime.Now ni DateTime.Today.
- Records immuables, aucun état global ; toute règle métier a son test.
