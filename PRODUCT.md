# TournéeVéto — PRODUCT.md

> **Statut : preuve de concept (POC).** Toutes les données (fermes, vaches, producteurs, visites) sont **fictives**. Les règles métier sont **simplifiées** et ne remplacent ni le jugement clinique, ni les protocoles officiels (proAction, Lactanet, OMVQ).
> **Marché de référence :** Québec (Canada) — vocabulaire, unités et repères réglementaires québécois.
> **Contraintes techniques :** PWA 100 % navigateur sur GitHub Pages, aucun backend, aucun compte, données stockées localement dans le navigateur, fonctionnement complet hors ligne. Les écrans doivent rester transposables en WPF (Windows) et MAUI (mobile) : logique métier séparée de l'interface, pas de dépendance à une API propre au navigateur dans les règles métier.

## 1. Problème

- Le Québec compte **4 250 fermes expédiant du lait** (1er août 2024, [CCIL](https://agriculture.canada.ca/fr/secteur/production-animale/centre-canadien-information-laitiere/statistiques-informations-marches/statistiques-ferme/nombre-fermes-vaches)), suivies par un nombre restreint de médecins vétérinaires en pratique bovine ; il manquerait **500 à 700 vétérinaires** au Québec, tous secteurs confondus (*à vérifier* auprès de l'[OMVQ](https://www.omvq.qc.ca/DATA/TEXTEDOC/2024---Portrait-de-la-profession-veterinaire---Document.pdf)).
- Chaque visite exige de préparer la liste des vaches à voir, de saisir des résultats dans l'étable sans réseau, puis de produire un rapport ; ces étapes reposent souvent sur du papier et une ressaisie au bureau.
- proAction impose une **évaluation des risques de biosécurité avec un vétérinaire tous les deux ans** ([PLC](https://dairyfarmersofcanada.ca/en/node/57626)), qui s'ajoute à la charge de visite.

## 2. Personas

| Persona | Objectif | Frustration | Contexte d'usage |
| --- | --- | --- | --- |
| **Dre Mélanie, vétérinaire rurale en pratique mixte** (petite clinique, 4 à 8 fermes/jour, urgences fréquentes) | Enchaîner les visites sans oublier d'animal ni d'action, rentrer sans ressaisie | Notes papier illisibles ou perdues, tournée réorganisée en cours de journée, pas de réseau dans les étables | Téléphone ou tablette dans l'étable, mains sales ou gantées ; ordinateur le soir pour les rapports |
| **Dr Julien, vétérinaire en suivi de troupeau** (visites planifiées toutes les 2 à 4 semaines, axées reproduction) | Disposer d'une grille de régie fiable par ferme et suivre l'évolution d'une visite à l'autre | Liste de régie à reconstituer à partir de plusieurs sources ; difficulté à voir ce qui reste en suspens depuis la dernière visite | Préparation la veille sur ordinateur ; tablette pendant la visite ; rapport remis au producteur |
| **Marc, producteur laitier** (60 vaches en stabulation libre, adhérent au contrôle laitier) | Savoir clairement quoi faire après la visite (vaches à tarir, à revoir, mesures de biosécurité) | Recommandations orales oubliées, rapport reçu tard ou incomplet | Ne touche pas l'application ; lit le rapport imprimé ou en PDF |

## 3. Proposition de valeur et parcours clés

**Proposition de valeur :** TournéeVéto permet au médecin vétérinaire de préparer, réaliser et documenter ses visites de fermes laitières sur un seul appareil, sans réseau, sans compte et sans ressaisie.

**Parcours 1 — Préparer la tournée du jour**
1. Le vétérinaire ouvre la tournée du jour (liste ordonnée des visites).
2. Il consulte la fiche de chaque ferme : coordonnées, effectif, dernière visite, points en suspens.
3. L'application génère la grille de régie de chaque ferme à partir des événements enregistrés (vêlages, inséminations, diagnostics).

**Parcours 2 — Réaliser la visite hors ligne**
1. Dans l'étable, le vétérinaire parcourt la grille de régie vache par vache (recherche par numéro).
2. Il saisit le résultat de chaque action (ex. diagnostic de gestation positif/négatif, vache tarie, examen post-vêlage) et des notes libres.
3. Les saisies sont enregistrées localement à chaque modification ; la visite peut être interrompue et reprise.

**Parcours 3 — Bilan de biosécurité et rapport de visite**
1. Le vétérinaire remplit un questionnaire de biosécurité simplifié (oui/non/partiel par rubrique) et obtient un score par rubrique.
2. Il rédige ses recommandations.
3. Il génère un rapport de visite imprimable (impression ou PDF via le navigateur) regroupant résultats de régie, bilan de biosécurité et recommandations.

## 4. Hors périmètre du POC

- Backend, synchronisation entre appareils, comptes, partage multi-utilisateurs.
- Connexion à des systèmes externes (Lactanet, ATQ, logiciels de gestion de troupeau ou de clinique) ; seul un jeu de données fictif embarqué est chargé au premier lancement, puis complété par saisie.
- Import de fichiers de producteurs réels, données nominatives ou réelles : l'import CSV du troupeau (#49) existe, mais n'est testé et démontré qu'avec des fichiers fictifs.
- Reproduction du questionnaire officiel proAction ou valeur de certification du bilan de biosécurité.
- Prescriptions, ordonnances, registre de traitements, gestion des médicaments, facturation.
- Optimisation d'itinéraire, cartographie, géolocalisation.
- Notifications, envoi de courriels, signature électronique.
- Développement des applications WPF et MAUI (seule la transposabilité des écrans est visée).

## 5. Glossaire

*Les valeurs chiffrées sont des règles simplifiées pour le POC, paramétrables, non des recommandations cliniques.*

| Terme | Définition |
| --- | --- |
| **Élevage / ferme** | Exploitation laitière suivie par le vétérinaire : un producteur, une adresse, un troupeau de vaches identifiées par un numéro. Au Québec, on parle couramment de « ferme ». |
| **Visite** | Passage planifié du vétérinaire dans une ferme, à une date donnée, qui produit des résultats de régie, éventuellement un bilan de biosécurité, et un rapport. Une tournée est l'ensemble ordonné des visites d'une journée. |
| **Régie** | Gestion courante du troupeau. La **grille (ou liste) de régie** est la liste des vaches sur lesquelles une action est attendue lors de la visite, avec le motif (à confirmer gestante, à tarir, post-vêlage, à inséminer, etc.). |
| **Vêlage** | Mise bas d'une vache ; marque le début d'une lactation. Règle POC : vêlage prévu = insémination fécondante + 280 jours, signalé dans les 14 jours qui précèdent ; examen post-vêlage entre 21 et 35 jours après le vêlage ; vache vide (non inséminée) plus de 60 jours après le vêlage à signaler. |
| **Tarissement** | Arrêt volontaire de la traite avant le vêlage suivant pour laisser la mamelle se régénérer. Règle POC : à prévoir 60 jours avant la date de vêlage prévue (gestation comptée à 280 jours après l'insémination fécondante). |
| **Insémination (IA)** | Dépôt de semence dans l'utérus de la vache (insémination artificielle). La date de la dernière IA sert de base au diagnostic de gestation et au calcul du vêlage prévu. |
| **Diagnostic de gestation (DG)** | Examen (palpation transrectale ou échographie) confirmant ou non qu'une vache est gestante. Règle POC : à faire entre 30 et 45 jours après la dernière IA ; au-delà de 45 jours sans DG, la vache reste dans la grille, signalée « DG en retard » ; résultat positif, négatif ou douteux (à revoir). |
| **CCS** | Comptage des cellules somatiques du lait, en cellules/mL ; indicateur de mammite subclinique. Norme canadienne pour le lait de réservoir : 400 000 cellules/mL. Règle POC : vache signalée si CCS individuel > 200 000 cellules/mL au dernier contrôle (seuil indicatif ; valeur stockée en milliers, donc signalée à partir de 201). |
| **Biosécurité** | Ensemble des mesures qui limitent l'entrée et la propagation des maladies dans la ferme (introduction d'animaux, visiteurs, véhicules, quarantaine, hygiène du vêlage, gestion des animaux morts). Évaluée dans TournéeVéto par un questionnaire simplifié par rubriques. |

### Motifs de la grille de régie (règles POC, seuils par défaut de `RegieThresholds`)

| Motif | Condition (J = jours par rapport à la date de la visite) | Urgence |
| --- | --- | --- |
| CCS élevé | Dernier CCS > 200 (milliers de cellules/mL) | Urgent |
| Vêlage prévu | Gestante ou tarie, vêlage prévu (IA + 280 j) dans 14 jours ou moins, date dépassée comprise | À surveiller |
| Vache vide | Vide, vêlée depuis plus de 60 jours (J61 et au-delà) | À surveiller |
| DG en retard | Inséminée sans DG depuis plus de 45 jours | À surveiller |
| Diagnostic de gestation | Inséminée sans DG depuis 30 à 45 jours | À faire |
| Post-vêlage | Vêlée depuis 21 à 35 jours | À faire |
| Tarissement | Gestante non tarie, vêlage prévu dans 60 jours ou moins | Normal |

Une vache qui répond à plusieurs motifs apparaît une seule fois, avec l'urgence la plus élevée. Une date incohérente (IA ou vêlage postérieurs à la visite) ou une gestante sans date d'IA est signalée comme anomalie (urgence au moins « À surveiller ») ; seuls les motifs qui dépendent de cette date sont ignorés, le CCS reste toujours évalué.
