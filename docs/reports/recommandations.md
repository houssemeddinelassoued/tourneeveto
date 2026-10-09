# Rapport de feature — Recommandations au producteur

Epic #9 (Must) · stories #34 (Rédiger des recommandations) et #35 (Organiser les recommandations) · 9 octobre 2026.
Pilotage : feature-lead (joué par la session principale, car un sous-agent Claude Code ne peut pas en lancer d'autres).

## Tâches et statuts

| Tâche | developer | tester | refactorer | security-reviewer | Statut |
|---|---|---|---|---|---|
| T1 — Domaine et stockage | OK | OK (+ scénarios) | OK (rien à simplifier) | OK, 3 mineurs | Fait |
| T2 — Écran et rapport | OK, puis 3 reprises | OK | OK (`IsFull` déplacé dans le domaine) | OK, 3 + 2 mineurs corrigés | Fait |
| QA exploratoire | — | — | — | — | Bloqué (MCP Playwright absent), remplacé par un script Playwright |

## Ce qui est livré

- **Domaine** (`Domain/Visits/VisitRecommendations.cs`) : liste ordonnée par visite.
  - Ajout, modification et suppression immuables, numérotation 1..n.
  - Texte rogné ; vide ou fait d'espaces refusé ; 1000 caractères et 20 recommandations au plus.
  - Caractères de contrôle et de format Unicode retirés, sauf `\n`, `\r` et `\t`.
  - `Sanitize` assainit les données relues.
- **Stockage** : `IVisitRepository.Get/SaveRecommendationsAsync`.
  - Base IndexedDB v4 avec un nouveau store `recommendations`, migration sans perte.
  - `deleteVisit` nettoie aussi les recommandations.
- **Interface** (`Components/RecommendationsEditor`, section « Recommandations au producteur » du rapport, lien depuis la grille de régie) :
  - enregistrement automatique ;
  - « Enregistrement impossible » avec le texte conservé ;
  - « Lecture impossible » sans risque d'écrasement ;
  - confirmation de suppression accessible au clavier ;
  - compteur « n / 1000 caractères » ;
  - impression de la seule liste numérotée.

## Décisions

- Les recommandations vivent dans le rapport, car c'est le document remis au producteur. Les maquettes n'ont pas d'écran dédié.
- Plafond de 20 recommandations et filtrage Unicode : suite de l'audit (rapport imprimé lisible, pas de texte inversé ou caché).
- Lecture en échec : la saisie est masquée plutôt que de risquer d'écraser des données existantes.
- Focus géré avec `ElementReference.FocusAsync()`, sans nouvelle interop JavaScript.

## Résultat des tests

- 531 tests xUnit et bUnit réussis : chaque scénario Gherkin de #34 et #35 a son test bUnit.
- 8 parcours Playwright réussis contre la version publiée avec CSP, en 768×1024 et 1280×800. Le nouveau parcours rédige deux recommandations, recharge la page et les retrouve numérotées.
- `dotnet format --verify-no-changes` propre.
- Exploration navigateur (script Playwright, 390, 768 et 1280 px) :
  - aucun défilement horizontal, aucune erreur console, cibles ≥ 44 px ;
  - focus dans le dialogue, Échap le ferme et rend le focus à « Supprimer » ;
  - compteur affiché.

## Points de sécurité

- Texte libre rendu uniquement en texte Razor (`white-space: pre-line`), jamais en `MarkupString`. Un test vérifie que `<script>` est affiché comme du texte.
- Données relues revalidées : liste nulle, textes trop longs, plus de 20 éléments, identifiants vides ou en double, JSON illisible.
- Aucun appel réseau, aucun `style=""` en ligne (CSP inchangée), aucun secret, aucune donnée réelle, aucun nouveau paquet.

## Risques restants

- La migration IndexedDB v4 est vérifiée par lecture du script, pas sur une vraie base v3. La vérifier une fois à la main sur un navigateur qui a déjà des données.
- L'exploration qa-explorer, via le serveur MCP Playwright, n'a pas tourné. La relancer dans une session où le serveur est chargé.
- Sans visite du jour pour l'élevage, la section affiche un message au lieu du champ : les recommandations ne s'ajoutent qu'à une visite de la tournée.
