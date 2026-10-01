# Blog text and cover-image editing verification

Verified locally on 2026-09-30. This records implementation and automated checks, not deployment or external publication.

## Delivered

- Existing story sections remain literal plain text. Authors explicitly enable Markdown per section; conversion escapes existing punctuation and preserves headings and citations.
- Formatted sections support headings, emphasis, lists, quotes, code, tables and safe links. Raw HTML is disabled; inline Markdown image embedding is rejected so imagery stays in the reviewed media flow.
- Blog, Atlas and Wear share the same rendering and validation for previews, published pages and static exports. Markdown whitespace is retained. The section limit is 32,000 characters.
- Cover editing consumes the published SDK ImageEditor for crop, quarter-turn rotation, flips, resize, output format/quality and host-enabled background removal.
- Original images are loaded before controls open; upright browser dimensions replace old preview-sized metadata. Preview responses create temporary object URLs. Apply creates a new immutable media asset and changes only the draft cover SHA; captions, attribution and product links remain intact. Saving and publishing remain explicit.
- Stale requests, source changes, permission loss and disposal cannot replace the current cover. Preview URLs are revoked. Session loss preserves the current recipe.
- Pending image changes and processing join story dirty state for Back/reload guards. Explicit Cancel discards image intent; unrelated story changes remain guarded. Saving/publishing/history actions wait until image edits are applied or cancelled.
- JPEG's white transparency fill is explained; PNG/WebP retain alpha. Unavailable background removal is shown as unavailable.

## Published dependencies

- Frontend `@wow-two-beta/ui-vue` 0.0.12, restored from registry.
- Backend `WoW2.Sdk.Backend.Beta` 10.0.63-beta, restored from NuGet.
- No local SDK source aliases, links, or package substitutes were adopted.

## Executed checks

| Check | Result |
|---|---|
| Editorial frontend `pnpm test` | 31/31 tests, 9 files passed |
| Editorial frontend `pnpm build` | TypeScript, 9 Vue SFCs and Vite production build passed |
| Scoped ESLint and Prettier | Passed for the text/image lane files and tests |
| Backend `ArticleFormattingTests` | 15/15 passed |
| SDK `MarkdownServiceTests` | 11/11 passed before publication |

Frontend regressions cover literal-to-formatted conversion, immediate dirty tracking, binary API contracts, unavailable tools, delayed results, source changes, connection loss, original dimensions, JPEG guidance and saved-story rotation/preview followed by Back/reload/Cancel.

Backend regressions cover legacy literal content, safe links/HTML, rejected inline images, preserved whitespace, length/format validation, generic-attribute injection and save-to-preview-to-publish-to-ZIP parity across all three publications.

## Shared SDK security fix

A published Markdown renderer reproduced executable generic attributes despite raw HTML being disabled: `[Link](https://example.com){onclick=alert(1)}`. The safe SDK pipeline now removes author-supplied generic properties/classes before applying renderer-owned link policy, while retaining heading anchors. Trusted raw-HTML mode remains explicit. The fix and regressions shipped in the published dependency above.

## Boundaries

- Root completed integrated local runtime/browser review with trusted TLS: real image processing, save/reopen, mobile layout, original preservation and revoked-session protection. Workspace evidence: `docs/editor-tools-build.md`.
- The backend media agent owns image endpoint/provenance, worker limits, proxy and full backend-suite evidence.
- Automatic background removal requires a configured, approved model on the host. Availability alone does not establish output quality.
- This lane performed no logins, runtime restarts, staging or commits.
