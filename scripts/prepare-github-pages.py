"""Prépare la sortie de `dotnet publish` pour GitHub Pages (ADR 0003, docs/adr/0003-hebergement.md).

Usage : python scripts/prepare-github-pages.py <dossier wwwroot publié> <base href, ex. /tourneeveto/>

1. réécrit <base href="/" /> d'index.html avec le chemin de publication ;
2. copie index.html en 404.html (liens profonds : GitHub Pages sert 404.html, Blazor affiche la bonne route) ;
3. crée .nojekyll (sinon _framework et _content seraient ignorés en publication depuis une branche) ;
4. recalcule l'empreinte d'index.html dans service-worker-assets.js, sinon le service worker refuse de s'installer.
"""
import base64
import hashlib
import pathlib
import re
import shutil
import sys


def main(wwwroot: pathlib.Path, base_href: str) -> None:
    if not (base_href.startswith("/") and base_href.endswith("/")):
        sys.exit(f"Le base href doit commencer et finir par « / » : {base_href}")

    index = wwwroot / "index.html"
    html, count = re.subn(rb'<base href="/"\s*/>', f'<base href="{base_href}" />'.encode(), index.read_bytes())
    if count != 1:
        sys.exit('<base href="/" /> introuvable (ou présent plusieurs fois) dans index.html.')
    index.write_bytes(html)

    # Les versions précompressées d'index.html ne correspondent plus au fichier réécrit.
    for stale in (wwwroot / "index.html.br", wwwroot / "index.html.gz"):
        stale.unlink(missing_ok=True)

    shutil.copyfile(index, wwwroot / "404.html")
    (wwwroot / ".nojekyll").touch()

    manifest = wwwroot / "service-worker-assets.js"
    digest = "sha256-" + base64.b64encode(hashlib.sha256(html).digest()).decode()
    text, count = re.subn(
        r'("hash":\s*")sha256-[^"]+("\s*,\s*"url":\s*"index\.html")',
        lambda match: match.group(1) + digest + match.group(2),
        manifest.read_text(encoding="utf-8"),
    )
    if count != 1:
        sys.exit("Entrée index.html introuvable dans service-worker-assets.js.")
    manifest.write_text(text, encoding="utf-8")

    print(f"Prêt pour GitHub Pages sous {base_href} : index.html ({digest}), 404.html, .nojekyll.")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    main(pathlib.Path(sys.argv[1]), sys.argv[2])
