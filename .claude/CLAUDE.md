# TournéeVéto — instructions pour Claude Code

Les règles communes à Copilot et à Claude Code sont dans un seul fichier, importé ci-dessous. Ne les recopie pas ici : modifie .github/copilot-instructions.md.

@../.github/copilot-instructions.md

## Spécifique à Claude Code

- Lectures refusées : .claude/settings.json (permissions deny), aligné sur .copilotignore. Toute nouvelle exclusion s'ajoute dans les deux fichiers.
- Agents (architect, developer, tester, refactorer, security-reviewer) : .claude/agents/<nom>.md ↔ .github/agents/<nom>.agent.md ; seules les lignes tools et model diffèrent, AgentInstructionsTests vérifie le reste.
- Règles par type de fichier : .claude/rules/*.md (champ paths) sont des copies de .github/instructions/*.instructions.md (champ applyTo), car un import @ chargerait la règle partout. Modifier d'abord le fichier Copilot ; AgentInstructionsTests vérifie que les deux restent identiques.
- Skills : `.claude/skills/<skill>/SKILL.md` reprend le nom et la description de `.github/skills/<skill>/SKILL.md` ; gabarits et check.cs n'existent que dans .github/skills.
- Réglages personnels (approbation des serveurs MCP) : .claude/settings.local.json, jamais versionné.
