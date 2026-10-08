---
statut: proposé
date: 2026-10-08
décideurs: Product Owner, équipe de développement .NET
consultés: agent architect
---

# ADR 0003 — Comment héberger l'application sur GitHub Pages ?

## Contexte et énoncé du problème

L'application est 100 % statique, sans backend ni secret ([PRODUCT.md](../../PRODUCT.md)). Le code est sur GitHub et la démo doit être accessible par URL (epic 12). Un site de projet GitHub Pages est servi sous un sous-chemin (`https://<compte>.github.io/<dépôt>/`), ce qui pose trois problèmes à Blazor WebAssembly :

- **l'adresse de base** : `<base href="/" />` du modèle ne correspond pas au sous-chemin publié ;
- **les liens profonds** : GitHub Pages ne connaît pas les routes Blazor et répond 404 à `/tourneeveto/fermes/F001` rechargé directement ;
- **Jekyll** : GitHub Pages ignore par défaut les dossiers qui commencent par `_`, dont `_framework`, qui contient le runtime .NET.

## Facteurs de décision

- Gratuit, sans compte ni secret en dehors de GitHub.
- Liens profonds fonctionnels après rechargement.
- Déploiement lié aux tests (story 12.1).
- Compatible avec le service worker et le fonctionnement hors ligne.

## Options considérées

1. **GitHub Pages via GitHub Actions** : base href réécrit à la publication, `404.html` copié d'`index.html`, fichier `.nojekyll`.
2. **GitHub Pages avec routage par fragment** (`/tourneeveto/#/fermes/F001`).
3. **Azure Static Web Apps** : repli SPA natif, en-têtes configurables.

| Critère | 1. Pages + réécriture | 2. Pages + fragment | 3. Azure Static Web Apps |
| --- | --- | --- | --- |
| Coût, comptes | Gratuit, GitHub seul. | Gratuit, GitHub seul. | Gratuit (offre Free), compte Azure et jeton de déploiement en secret. |
| Liens profonds | Oui, via `404.html`. | Oui, mais non natif dans Blazor : routeur à adapter. | Oui, natif. |
| URL lisibles | Oui. | Non (`#/`). | Oui. |
| En-têtes (CSP, cache) | Non configurables. | Non configurables. | Configurables. |

## Décision

**Option retenue : 1. GitHub Pages via GitHub Actions, publié sous `/tourneeveto/`.**

Le workflow de publication, déclenché sur `main` après les tests, enchaîne :

1. `dotnet publish src/TourneeVeto.Web -c Release -o publish` ;
2. **réécrire le base href** dans `publish/wwwroot/index.html` : `<base href="/" />` devient `<base href="/tourneeveto/" />`. En local, l'application garde `/` ;
3. **copier `index.html` en `404.html`** : GitHub Pages sert ce fichier pour toute page inconnue, Blazor démarre et affiche la bonne route ;
4. **créer `.nojekyll`** à la racine publiée, pour que `_framework` et `_content` soient servis ;
5. **recalculer l'empreinte d'`index.html`** dans `service-worker-assets.js` : la réécriture du base href change son contenu, et le service worker refuse de s'installer si l'empreinte ne correspond plus ;
6. **ajouter la Content-Security-Policy** (`<meta http-equiv>`, GitHub Pages ne permettant pas d'en-têtes) : `script-src 'self' 'wasm-unsafe-eval'` plus l'empreinte de chaque script en ligne d'index.html (table d'imports, enregistrement du service worker), calculée après normalisation des fins de ligne comme le fait le navigateur ;
7. publier avec `actions/upload-pages-artifact` puis `actions/deploy-pages` (source Pages : GitHub Actions).

**Prérequis :** l'URL publiée doit correspondre exactement à l'adresse de base, casse comprise. Le dépôt a donc été renommé `tourneeveto` le 2026-10-08 ; URL publiée : `https://houssemeddinelassoued.github.io/tourneeveto/`.

### Conséquences

#### Positives

- Gratuit, sans compte ni secret en dehors de GitHub ; code et site dans le même dépôt.
- Déploiement uniquement si les tests passent.
- URL lisibles et liens profonds fonctionnels.

#### Négatives

- Les liens profonds rechargés en ligne répondent avec un statut HTTP 404, même si la page s'affiche correctement. Une fois le service worker installé, les navigations sont servies depuis le cache.
- Étapes de post-traitement (base href, `404.html`, empreinte) fragiles : un oubli casse le site ou le mode hors ligne. Le test de fumée les couvre.
- Pas d'en-têtes configurables : la CSP passe par une balise `<meta>` (sans `frame-ancestors`), pas de réglage de la durée de cache.
- GitHub Pages ne sert pas les fichiers Brotli précompressés de Blazor : premier chargement plus lourd (*à mesurer*).

### Confirmation

- Test de fumée Playwright sur l'URL publiée : `/tourneeveto/fermes/F001` rechargé directement affiche la fiche F001, et une route inconnue affiche « Page introuvable » (story 12.2).
- Test Playwright hors ligne sur le site publié : le service worker s'installe et les 3 parcours s'exécutent (epic 3).

## Conditions de révision

1. Des en-têtes de sécurité (CSP) ou de cache deviennent nécessaires : passer à Azure Static Web Apps ou Cloudflare Pages.
2. Le premier chargement est trop lent sur les tablettes cibles faute de Brotli : même piste, ou décompression Brotli côté client.
3. Un domaine personnalisé est adopté : l'adresse de base devient `/` et la réécriture disparaît.
4. Le dépôt doit devenir privé sans offre GitHub payante : GitHub Pages n'est plus disponible.
