---
name: indexeddb-interop
description: Modèle d'accès à IndexedDB depuis Blazor pour TournéeVéto (module JS isolé + repository C#). Utiliser pour tout code qui lit, écrit ou supprime des visites, des vaches ou des photos dans le navigateur.
---
- Interface dans TourneeVeto.Ui/Data (IVisitRepository) ; implémentation IndexedDbVisitRepository, enregistrée en Scoped par services.AddTourneeVeto() de la RCL (commun aux hôtes Web, WPF et MAUI).
- JS : un seul module ES, wwwroot/js/visitStore.js, base « tourneeveto », version incrémentée à chaque changement de schéma (onupgradeneeded) ; stores farms, visits, cows (clé [farmId, id] depuis la version 2 : deux fermes peuvent avoir le même numéro), photos, visitRecords (clé [visitId, cowId], version 3 : saisies de la visite) et biosecurity (clé visitId, version 3) ; index sur farmId et date. Remplacement d'un troupeau : replaceCows, en une transaction. Jeu de démonstration : seedIfEmpty (une transaction, seulement si la base est vide), appelé par DemoDataSeeder.EnsureSeededAsync() avant toute lecture.
- C# : import paresseux via IJSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/TourneeVeto.Ui/js/visitStore.js"), libéré dans DisposeAsync (ignorer JSDisconnectedException).
- Erreurs : QuotaExceededError et base indisponible remontées en exception typée (StorageUnavailableException), affichées par l'interface.
- Tests : bUnit, JSInterop.SetupModule avec le chemin ci-dessus ; jamais la vraie base.
- Exemples (copies de l'implémentation, qui fonctionnent) : exemples/visitStore.js → src/TourneeVeto.Ui/wwwroot/js/visitStore.js ; exemples/IndexedDbVisitRepository.cs → src/TourneeVeto.Ui/Data/ (un fichier par type).
