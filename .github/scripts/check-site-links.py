#!/usr/bin/env python3
"""Every link to a contract on the Pages site must reach a file the site contains: the landing page's links into
api/, and every relative link and script on the pages under api/. The test-report links are left alone, so the check
also runs locally, where no reports have been downloaded.

usage: check-site-links.py <site-dir>"""
import pathlib, re, sys

site = pathlib.Path(sys.argv[1]).resolve()
checks = [(site / "index.html", lambda ref: ref.startswith("api/"))]
checks += [(page, lambda ref: True) for page in sorted((site / "api").rglob("*.html"))]
missing = []
for page, wanted in checks:
    for ref in re.findall(r'(?:href|src)="([^"#?]+)', page.read_text(encoding="utf-8")):
        if re.match(r"^[a-z]+:|^//", ref) or not wanted(ref):   # absolute URLs, and links that are not ours
            continue
        target = (page.parent / ref).resolve()
        if target.is_dir():
            target = target / "index.html"
        if not target.is_file():
            missing.append(f"{page.relative_to(site)} -> {ref}")
missing = list(dict.fromkeys(missing))   # a page that links a document twice is reported once
print("\n".join(missing) or f"all contract links resolve ({len(checks)} pages)")
sys.exit(1 if missing else 0)
