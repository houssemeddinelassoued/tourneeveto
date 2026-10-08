---
name: indexeddb-interop
description: Modèle d'accès à IndexedDB depuis Blazor pour TournéeVéto (module JS isolé + repository C#). Utiliser pour tout code qui lit, écrit ou supprime des visites, des vaches ou des photos dans le navigateur.
---
<!-- Version Claude Code du skill .github/skills/indexeddb-interop (Copilot). Les exemples n'existent qu'une fois,
     dans .github/skills/indexeddb-interop/exemples/ : ne pas les recopier ici.
     Le nom et la description doivent rester identiques des deux côtés (vérifié par AgentInstructionsTests). -->
- Interface dans TourneeVeto.Ui/Data (IVisitRepository) ; implémentation IndexedDbVisitRepository, enregistrée en Scoped par services.AddTourneeVeto() de la RCL (commun aux hôtes Web, WPF et MAUI).
- JS : un seul module ES, src/TourneeVeto.Ui/wwwroot/js/visitStore.js, base « tourneeveto », version incrémentée à chaque changement de schéma (onupgradeneeded) ; stores farms, visits, cows, photos ; index sur farmId et date. Jeu de démonstration : seedIfEmpty (une transaction, seulement si la base est vide), appelé par DemoDataSeeder.EnsureSeededAsync() avant toute lecture.
- C# : import paresseux via IJSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/TourneeVeto.Ui/js/visitStore.js"), libéré dans DisposeAsync (ignorer JSDisconnectedException).
- Erreurs : QuotaExceededError et base indisponible remontées en exception typée (StorageUnavailableException), affichées par l'interface.
- Tests : bUnit, JSInterop.SetupModule avec le chemin ci-dessus ; jamais la vraie base.
- Exemples (copies de l'implémentation, qui fonctionnent) : .github/skills/indexeddb-interop/exemples/visitStore.js → src/TourneeVeto.Ui/wwwroot/js/visitStore.js ; .github/skills/indexeddb-interop/exemples/IndexedDbVisitRepository.cs → src/TourneeVeto.Ui/Data/ (un fichier par type).
