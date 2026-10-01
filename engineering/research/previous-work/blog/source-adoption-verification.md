# Blog source adoption verification

*Verified: 2026-09-29*

## Result

Both Blog implementations are present inside Marketing; full runtime integration remains deferred.
The original source directories were unchanged at verification time, awaiting parent archival.

| Check | Evidence |
|---|---|
| Static build | Homepage, archive, three topics and thirty articles generated successfully |
| Static parity | All 42 generated artifacts byte-identical to donor: 35 HTML pages, CSS, robots and five images |
| Editorial parity | All thirty stories and three topics identical between static module and modern seed |
| Source preservation | All 206 copied source files still matched their untouched donors before adoption edits |
| Runtime data | All nine copied App_Data files matched donor SHA-256 hashes |
| Backend | `dotnet test OcharoBlog.BackendServices.slnx --no-restore`: 19 passed, zero failed |
| Editor | Node24.11.0, pnpm10.33.2; offline frozen install; typecheck passed; six tests passed |
| Native isolation | Backend tests used temporary test databases; no application server started |
| Git scope | App_Data/generated output ignored; index remained empty; no Git writes |
| Deployment | Existing Marketing deploy script unchanged; no public deployment or donor CI activated |
| Credential scan | Candidate source scanned for private-key/GitHub/AWS/OpenAI credential signatures; no matches |

## Migration regression

Development seed assets now resolve relative to the Blog API content root. The new backend
regression verifies the logo, collection hero and actual story-cover bytes reach rendered pages
with a relative asset directory. Existing absolute asset overrides remain supported.
An initial test fixture requested an imageless story; the final test targets `let-one-piece-lead`,
the seed's actual image-bearing story. The final nineteen-test run passed.

## Preservation boundaries

Static authored CSS and five PNGs moved from dist-only inputs to tracked `apps/blog/public/`.
The generator copies those inputs into ignored output; their original hashes remain unchanged.
Editorial research and reference-image rights manifests remain preserved and unpublished.

No runtime database was reseeded or merged. App_Data stays out of Git and Docker contexts.
Default Marketing scripts keep their previous values; only explicit `build:blog` and
`preview:blog` commands were added. Blog retains its own editor workspace, API ports and routes.
The modern seed's historical provenance remains unchanged.

Local machine evidence (ignored, not a portability dependency):
`/private/tmp/ocharo-blog-adoption/mapping.json`, `static-dist-before.json`, and
`source-verification.json`. Parent archival metadata should preserve donor Git/index state
and the final old-to-new path mapping.

## Final link and source freeze

Repaired 32 relative research links after the static app moved deeper. Historical media
research carries a snapshot note; original absolute source paths and line references remain
historical evidence. No unresolved relative Markdown links remained in the static app.
The final mapping records 206 source files and their current destination hashes; donor
source bytes were unchanged. All 42 static output hashes still matched, and Git diff-check passed.
