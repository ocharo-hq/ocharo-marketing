"""Build one standalone app from its revisioned published export."""
import hashlib, html, io, json, os, re, shutil, sys, zipfile
from pathlib import Path
version=os.environ.get('APP_VERSION','dev')
if version=='0.0.0': version='dev'
app=sys.argv[1]
if app not in ('atlas','wear','blog','catalogue'): raise SystemExit('Unknown publication')
root=Path(__file__).resolve().parents[1]; target=root/'apps'/app/'dist'
archive=os.environ.get('OCHARO_EXPORT_ARCHIVE')
locale=os.environ.get('OCHARO_CATALOGUE_LOCALE') if app=='catalogue' else None
# Editorial's export routes need a signed-in session, so a build takes an archive an editor downloaded.
if not archive: raise SystemExit('Published export unavailable. Download it from Editorial and set OCHARO_EXPORT_ARCHIVE.')
data=Path(archive).read_bytes()
with zipfile.ZipFile(io.BytesIO(data)) as bundle:
    if app != 'blog' and ('publication.json' not in bundle.namelist() or json.loads(bundle.read('publication.json')).get('publication') != app): raise SystemExit('Export belongs to another publication')
    if app=='catalogue' and locale and json.loads(bundle.read('publication.json')).get('locale')!=locale: raise SystemExit('Catalogue export language does not match the requested language')
    if app == 'blog' and ('site.css' not in bundle.namelist() or 'feed.xml' not in bundle.namelist()): raise SystemExit('Choose the Blog published export')
    if 'index.html' not in bundle.namelist(): raise SystemExit('Export has no home page')
    if any(p.startswith('/') or '..' in Path(p).parts for p in bundle.namelist()): raise SystemExit('Unsafe export path')
    staging=target.with_name('dist-building'); shutil.rmtree(staging,ignore_errors=True); staging.mkdir(parents=True)
    bundle.extractall(staging)
# Optional public origins let every artifact deploy on its own host without merging visual identity.
origins={key:os.environ.get('OCHARO_PUBLIC_'+key.upper()+'_URL','').rstrip('/') for key in ('atlas','wear','blog','catalogue')}
for page in staging.rglob('*.html'):
    text=page.read_text()
    text=re.sub(r'(<small[^>]*data-app-version[^>]*>)[^<]*(</small>)', lambda m: re.sub(r'title="[^"]*"', 'title="'+html.escape(version, quote=True)+'"', m[1])+html.escape(version)+m[2], text)
    for key,origin in origins.items():
        if not origin: continue
        if not origin.startswith('https://'): raise SystemExit('Public origins must use HTTPS')
        if key=='blog':
            text=text.replace('href="/"','href="'+origin+'/"').replace('href="/journal/','href="'+origin+'/journal/').replace('href="/topics/','href="'+origin+'/topics/')
        else: text=text.replace('href="/'+key+'/', 'href="'+origin+'/'+key+'/')
    page.write_text(text)
(staging/'build-manifest.json').write_text(json.dumps({'publication':app,'version':version,'source':'published-revisions','exportSha256':hashlib.sha256(data).hexdigest()},indent=2)+'\n')
shutil.rmtree(target,ignore_errors=True); staging.rename(target)
print(json.dumps({'publication':app,'pages':len(list(target.rglob('*.html'))),'artifact':str(target),'source':'published-revisions'}))
