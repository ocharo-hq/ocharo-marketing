"""Build one standalone app from its revisioned published export."""
import hashlib, html, io, json, os, re, shutil, ssl, sys, urllib.parse, urllib.request, zipfile
from pathlib import Path
version=os.environ.get('APP_VERSION','dev')
if version=='0.0.0': version='dev'
app=sys.argv[1]
if app not in ('atlas','wear','blog','catalogue'): raise SystemExit('Unknown publication')
root=Path(__file__).resolve().parents[1]; target=root/'apps'/app/'dist'
archive=os.environ.get('OCHARO_EXPORT_ARCHIVE')
locale=os.environ.get('OCHARO_CATALOGUE_LOCALE') if app=='catalogue' else None
if archive: data=Path(archive).read_bytes()
else:
    # A live fetch reads Editorial's export as the public-build client, with a token bound to that one Editorial.
    secret=os.environ.get('OCHARO_EXPORT_CLIENT_SECRET')
    if not secret: raise SystemExit('Published export unavailable. Set OCHARO_EXPORT_ARCHIVE, or OCHARO_EXPORT_CLIENT_SECRET for a live export.')
    origin=os.environ.get('OCHARO_EDITORIAL_URL','https://localhost:8280').rstrip('/')
    authority=os.environ.get('OCHARO_IDENTITY_URL','https://localhost:8404').rstrip('/')
    if not origin.startswith('https://') or not authority.startswith('https://'): raise SystemExit('Use HTTPS Editorial and Identity origins')
    endpoint='/api/blog/export/site.zip' if app=='blog' else '/api/blog/export/'+app+'.zip'
    if locale: endpoint+='?'+urllib.parse.urlencode({'locale':locale})
    context=ssl.create_default_context(cafile=os.environ.get('OCHARO_CA_FILE'))
    form=urllib.parse.urlencode({'grant_type':'client_credentials','client_id':os.environ.get('OCHARO_EXPORT_CLIENT_ID','public-build'),'client_secret':secret,'resource':origin+'/'}).encode()
    try:
        with urllib.request.urlopen(urllib.request.Request(authority+'/connect/token',data=form),context=context,timeout=30) as response: token=json.load(response)['access_token']
        with urllib.request.urlopen(urllib.request.Request(origin+endpoint,headers={'Authorization':'Bearer '+token}),context=context,timeout=60) as response: data=response.read()
    except Exception as e: raise SystemExit('Published export unavailable. Start Editorial and Identity, or set OCHARO_EXPORT_ARCHIVE. '+str(e))
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
