# TournéeVéto — instructions pour Claude Code

Les règles communes à Copilot et à Claude Code sont dans un seul fichier, importé ci-dessous. Ne les recopie pas ici : modifie .github/copilot-instructions.md.

@../.github/copilot-instructions.md

## Spécifique à Claude Code

- Lectures refusées : .claude/settings.json (permissions deny), aligné sur .copilotignore. Toute nouvelle exclusion s'ajoute dans les deux fichiers.
- Agents (architect, developer, tester, refactorer, security-reviewer) : .claude/agents/<nom>.md ↔ .github/agents/<nom>.agent.md ; seules les lignes tools et model diffèrent, AgentInstructionsTests vérifie le reste.
- Règles par type de fichier : .claude/rules/*.md (champ paths) sont des copies de .github/instructions/*.instructions.md (champ applyTo), car un import @ chargerait la règle partout. Modifier d'abord le fichier Copilot ; AgentInstructionsTests vérifie que les deux restent identiques.
- Skills : `.claude/skills/<skill>/SKILL.md` reprend le nom et la description de `.github/skills/<skill>/SKILL.md` ; gabarits et check.cs n'existent que dans .github/skills.
- feature-lead délègue à d'autres agents : dans Claude Code, un sous-agent ne peut pas en lancer un autre, il faut donc le lancer comme agent principal (claude --agent feature-lead). qa-explorer utilise le serveur MCP playwright (.mcp.json, approuvé dans settings.local.json).
- Hook de formatage : déclaré dans .claude/settings.json (matcher Edit|Write|MultiEdit), il appelle le script commun .github/hooks/format-edited-file.ps1. Si VS Code active chat.useClaudeHooks, Copilot exécute aussi ce hook en plus de .github/hooks/format.json : le formatage est alors fait deux fois (sans effet, mais plus lent).
- Réglages personnels (approbation des serveurs MCP) : .claude/settings.local.json, jamais versionné.
