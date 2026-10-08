---
name: create-component
description: Crée un composant ou une page Razor de TournéeVéto dans src/TourneeVeto.Ui, avec son CSS isolé et son test bUnit, à partir des gabarits du projet. Utiliser dès qu'on demande de créer, ajouter ou générer un composant, une page, un écran ou un élément d'interface.
---
<!-- Version Claude Code du skill .github/skills/create-component (Copilot). Les gabarits et le script de
     vérification n'existent qu'une fois, dans .github/skills/create-component/ : ne pas les recopier ici.
     Le nom et la description doivent rester identiques des deux côtés (vérifié par AgentInstructionsTests). -->
# Créer un composant TournéeVéto
1. Déduire le nom (PascalCase) et le rôle ; demander si c'est ambigu. Page (avec @page) dans src/TourneeVeto.Ui/Pages/, composant dans src/TourneeVeto.Ui/Components/.
2. Copier .github/skills/create-component/template/Component.razor.txt, Component.razor.css.txt et ComponentTests.cs.txt en remplaçant __NOM__ (le test va dans tests/TourneeVeto.Tests/Ui/).
3. Styles : uniquement les variables de src/TourneeVeto.Ui/wwwroot/tokens.css, dans le .razor.css.
4. Logique métier : dans TourneeVeto.Domain (avec test xUnit), jamais dans le composant.
5. Lancer dotnet run .github/skills/create-component/check.cs -- <Nom> puis dotnet test --filter <Nom> ; corriger jusqu'à succès.
6. Répondre avec la liste des fichiers créés, sans recopier leur contenu.
