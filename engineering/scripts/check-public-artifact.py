"""Builds one published app (atlas, wear, blog or catalogue) from an isolated export fixture and checks its manifest.

CI's `Public artifacts` check runs it from the frontend workspace root, after `pnpm install --frozen-lockfile`:

    python3 ../../scripts/check-public-artifact.py atlas

It exercises export packaging only, never authenticated publication or live content.
"""
import hashlib
import json
import os
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path

APPS = ("atlas", "wear", "blog", "catalogue")


def main(app: str) -> None:
    if app not in APPS:
        raise SystemExit(f"Unknown app {app!r}; expected one of {', '.join(APPS)}")
    with tempfile.TemporaryDirectory() as directory:
        archive = Path(directory) / "published-fixture.zip"
        with zipfile.ZipFile(archive, "w") as bundle:
            bundle.writestr("index.html", "<!doctype html><title>CI fixture</title><small data-app-version>dev</small>")
            bundle.writestr("publication.json", json.dumps({"publication": app, "locale": "en"}))
            if app == "blog":
                bundle.writestr("site.css", "body { color: #141416; }")
                bundle.writestr("feed.xml", '<rss version="2.0"></rss>')
        environment = {**os.environ, "OCHARO_EXPORT_ARCHIVE": str(archive), "APP_VERSION": "dev"}
        if app == "catalogue":
            environment["OCHARO_CATALOGUE_LOCALE"] = "en"
        subprocess.run(["pnpm", "--filter", f"@ocharomarketing/{app}", "build"], env=environment, check=True)
        target = Path("apps") / app / "dist"
        manifest = json.loads((target / "build-manifest.json").read_text())
        assert manifest["publication"] == app and manifest["source"] == "published-revisions", manifest
        assert manifest["exportSha256"] == hashlib.sha256(archive.read_bytes()).hexdigest(), manifest
        assert (target / "index.html").is_file(), target
    print(f"{app}: packaged from the export fixture")


if __name__ == "__main__":
    for name in sys.argv[1:] or APPS:
        main(name)
