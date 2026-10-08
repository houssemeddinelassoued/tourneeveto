# TournéeVéto — instructions communes aux agents (Copilot, Claude Code)
Source unique : ce fichier est importé par .claude/CLAUDE.md. Toute règle commune se modifie ici.
Règles par type de fichier : .github/instructions/*.instructions.md (tests, razor, domain), importées par .claude/rules/*.md.
Produit : voir PRODUCT.md. Périmètre : docs/mvp.md. Architecture : docs/architecture.md. Décisions : docs/adr/. Backlog : GitHub Issues (critères Gherkin).

## Stack
.NET 10 · C# · Blazor WebAssembly autonome (PWA) · IndexedDB via JS interop · xUnit + bUnit · Playwright pour .NET.
Application 100 % statique publiée sur GitHub Pages sous /tourneeveto/. Hôtes WPF et MAUI (Blazor Hybrid) au module 10.
SDK figé par global.json (10.0.x) ; Nullable, ImplicitUsings et TreatWarningsAsErrors dans Directory.Build.props.

## Commandes
dotnet build · dotnet test · dotnet format --verify-no-changes · dotnet publish src/TourneeVeto.Web -c Release
Ne lance jamais dotnet run ni dotnet watch sur l'application : ils ne rendent pas la main (l'humain lance l'application).
Seule exception : les scripts de vérification des skills (dotnet run .github/skills/<skill>/check.cs -- <args>), qui se terminent seuls.

## Skills
create-component (.github/skills/create-component, version Claude dans .claude/skills) : composant ou page Razor + CSS isolé + test bUnit à partir des gabarits, vérifiés par check.cs.
indexeddb-interop (.github/skills/indexeddb-interop, version Claude dans .claude/skills) : accès à IndexedDB (module JS isolé + IVisitRepository), exemples qui fonctionnent.

## Agents
architect (choix techniques, ADR, schémas) · developer (réalise une tâche du plan) · tester (écrit et lance les tests, uniquement dans tests/) · refactorer (simplifie, comportement inchangé) · security-reviewer (audite les fichiers modifiés, ne corrige rien).
Tous terminent par le même compte rendu de 5 lignes : Statut, Fichiers, Tests, Points ouverts, Recommandation.

## Structure (ADR 0002)
src/TourneeVeto.Domain/  règles métier pures, sans accès au stockage (aucune dépendance à Blazor, au JS ni au navigateur)
src/TourneeVeto.Ui/      Razor Class Library : pages, mise en page, composants, accès IndexedDB (Data/, wwwroot/js/)
src/TourneeVeto.Web/     hôte Blazor WebAssembly : Program.cs, App.razor, index.html, service worker ; aucune page @page
tests/TourneeVeto.Tests/ xUnit (domaine, architecture) + bUnit (composants) · tests/TourneeVeto.E2E/ Playwright (à créer avec le premier test de parcours)
Dépendances : Web → Ui → Domain ; Tests → Domain, Ui. Vérifiées par tests/TourneeVeto.Tests/ArchitectureTests.cs.

## Principes non négociables
1. Zéro backend, zéro secret : tout ce qui est dans wwwroot/ est public.
2. Hors ligne d'abord : une visite complète se fait sans réseau.
3. Tests d'abord : toute règle métier a son test xUnit ; chaque parcours clé, son test Playwright.
4. Accessibilité AA, tablette d'abord : cibles tactiles d'au moins 44 px (gants, lumière vive).
5. Budget : téléchargement initial < 4 Mo compressés (trimming actif) ; Lighthouse accessibilité ≥ 90.

## Règles du projet
- Dates : DateOnly ; « aujourd'hui » vient d'un TimeProvider injecté côté Ui/Web et est passé au domaine en paramètre DateOnly ; jamais de DateTime.Now ni DateTime.Today.
- Données fictives uniquement (DemoData) ; règles métier simplifiées, documentées dans PRODUCT.md.
- Aucune logique métier dans un .razor : appeler TourneeVeto.Domain.
- Stockage : IndexedDB derrière IVisitRepository (TourneeVeto.Ui/Data), module visitStore.js ; photos en Blob (ADR 0001, skill indexeddb-interop).
- JavaScript uniquement dans src/TourneeVeto.Ui/wwwroot/js/, chargé en module isolé (IJSObjectReference).
- index.html garde <base href="/" /> : la réécriture en /tourneeveto/, 404.html et .nojekyll sont faits à la publication (ADR 0003).
- Textes d'interface en français.

## Interdits
- Pas de backend, pas de secret, pas de clé d'API, aucun appel réseau sortant.
- Ne jamais lire les fichiers exclus de l'IA : secrets (.env, .env.*, *.pfx, *.snk, appsettings.*.local.json, secrets/), données réelles (data/reel/, exports/, *-reel.csv). Liste de référence : .copilotignore, reprise dans .claude/settings.json.
- Pas de nouveau paquet NuGet ou npm sans le signaler dans la réponse.
