"""Prépare la sortie de `dotnet publish` pour GitHub Pages (ADR 0003, docs/adr/0003-hebergement.md).

Usage : python scripts/prepare-github-pages.py <dossier wwwroot publié> <base href, ex. /tourneeveto/>

1. réécrit <base href="/" /> d'index.html avec le chemin de publication ;
2. copie index.html en 404.html (liens profonds : GitHub Pages sert 404.html, Blazor affiche la bonne route) ;
3. crée .nojekyll (sinon _framework et _content seraient ignorés en publication depuis une branche) ;
4. recalcule l'empreinte d'index.html dans service-worker-assets.js, sinon le service worker refuse de s'installer.
5. ajoute la Content-Security-Policy (scripts en ligne autorisés par leur empreinte).
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
    html = add_content_security_policy(html)
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


def add_content_security_policy(html: bytes) -> bytes:
    """5. Ajoute la CSP (aucun backend, aucun appel réseau sortant). Les scripts en ligne d'index.html (table d'imports
    générée à la publication, enregistrement du service worker) sont autorisés par leur empreinte, jamais par 'unsafe-inline'."""
    inline_scripts = re.findall(rb"<script(?![^>]*\bsrc=)[^>]*>(.*?)</script>", html, re.S)
    # Le navigateur calcule l'empreinte après normalisation des fins de ligne (CRLF → LF, norme HTML).
    normalized = [script.replace(b"\r\n", b"\n").replace(b"\r", b"\n") for script in inline_scripts]
    hashes = " ".join(f"'sha256-{base64.b64encode(hashlib.sha256(script).digest()).decode()}'" for script in normalized)
    policy = (
        "default-src 'self'; "
        f"script-src 'self' 'wasm-unsafe-eval' {hashes}; "
        "style-src 'self'; img-src 'self' data: blob:; font-src 'self'; connect-src 'self'; "
        "worker-src 'self'; manifest-src 'self'; object-src 'none'; base-uri 'self'; form-action 'none'"
    )
    meta = f'<meta http-equiv="Content-Security-Policy" content="{policy}" />'.encode()
    html, count = re.subn(rb'(<meta charset="utf-8" />)', lambda match: match.group(1) + b"\n    " + meta, html, count=1)
    if count != 1:
        sys.exit('<meta charset="utf-8" /> introuvable dans index.html : CSP non ajoutée.')
    return html


if __name__ == "__main__":
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    main(pathlib.Path(sys.argv[1]), sys.argv[2])
