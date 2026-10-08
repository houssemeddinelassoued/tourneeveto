---
name: create-component
description: Crée un composant ou une page Razor de TournéeVéto dans src/TourneeVeto.Ui, avec son CSS isolé et son test bUnit, à partir des gabarits du projet. Utiliser dès qu'on demande de créer, ajouter ou générer un composant, une page, un écran ou un élément d'interface.
---
# Créer un composant TournéeVéto
1. Déduire le nom (PascalCase) et le rôle ; demander si c'est ambigu. Page (avec @page) dans Pages/, composant dans Components/.
2. Copier template/Component.razor.txt, template/Component.razor.css.txt et template/ComponentTests.cs.txt en remplaçant __NOM__ (le test va dans tests/TourneeVeto.Tests/Ui/).
3. Styles : uniquement les variables de wwwroot/tokens.css, dans le .razor.css.
4. Logique métier : dans TourneeVeto.Domain (avec test xUnit), jamais dans le composant.
5. Lancer dotnet run .github/skills/create-component/check.cs -- <Nom> puis dotnet test --filter <Nom> ; corriger jusqu'à succès.
6. Répondre avec la liste des fichiers créés, sans recopier leur contenu.
