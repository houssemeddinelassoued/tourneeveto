# TournéeVéto — instructions pour Claude Code

Les règles communes à Copilot et à Claude Code sont dans un seul fichier, importé ci-dessous. Ne les recopie pas ici : modifie .github/copilot-instructions.md.

@../.github/copilot-instructions.md

## Spécifique à Claude Code

- Lectures refusées : .claude/settings.json (permissions deny), aligné sur .copilotignore. Toute nouvelle exclusion s'ajoute dans les deux fichiers.
- Agent architect : @agent-architect (.claude/agents/architect.md) ; son équivalent Copilot est .github/agents/architect.agent.md. Seule la ligne tools diffère entre les deux.
- Règles par type de fichier : .claude/rules/*.md (champ paths) importent .github/instructions/*.instructions.md (champ applyTo). Les globs doivent rester identiques des deux côtés ; le contenu ne se modifie que côté .github/instructions.
- Réglages personnels (approbation des serveurs MCP) : .claude/settings.local.json, jamais versionné.
