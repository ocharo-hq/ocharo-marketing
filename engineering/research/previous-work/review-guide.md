# Ocharo Marketing — local app review

*Last updated: 2026-09-27*

## Open the working app

Use [the local marketing desk](https://localhost:8242/). The backend is on HTTPS 8276 / HTTP 8277. Runtime ownership and final verification are in [handoff](handoff.md). This is a single-operator local pilot, with no Brand Workspace runtime dependency.

## Review the creation flow

1. **Bugun** shows actual draft, review and planning counts. These are work counts, not audience statistics.
2. **Mahsulotlar** offers the existing Nimcha, Abaya and Kostyum product snapshots and their original front cutouts. Catalogue facts remain draft review inputs; the app does not rewrite canonical business records.
3. **Yangi post** opens the composer. Select a product, title, short subtitle, preset and framing. Keep detailed material, sizing, price and order information in the caption.
4. Compare **T02 bone** and **T02 charcoal** with the same title, image and framing. **T01 half fabric** needs a separate matching fabric image; the app does not invent a photographed swatch.
5. Generate the local card, inspect the finished output, and explicitly select it. The selected PNG is the artwork approved/exported; a pending or stale render cannot silently replace it.
6. Save and approve the revision. Download its ZIP containing caption, selected original image bytes and a provenance manifest. Editing clears current approval and preserves earlier snapshots.
7. Add a time in the Tashkent calendar. A saved entry remains a **manual plan**. For Telegram/X, automatic delivery additionally needs a verified configured destination, enabled execution and a separate confirmation of the exact post, image and destination. Current flags remain off; adding credentials never dispatches older plans.
8. **Atlas** compares exact completed PNGs and exports a PDF journal with labels and review notes outside the customer artwork.

## Discuss the design

- Is the hierarchy understandable without explanation: product, artwork, caption, review, plan?
- Which starting surface works best for Ocharo: bone, charcoal, or the half-fabric composition?
- Does the existing front cutout give enough shape information, or should the next supporting slide show a worn image/detail?
- Is the Uzbek wording natural for Maftuna? Source catalogue material fields are not yet fully localized.
- Which account should be connected first? Instagram is the main brand page; Telegram can support a channel; WhatsApp is customer messaging with recipient permission and template/window rules; X is optional.

These are review topics, not blocking implementation approvals. They create no new obligation for Maftuna.

## Implemented integration boundary

OpenAI image/edit and text/vision brokers, Gemini Nano Banana reference generation, and Telegram/Instagram/X/WhatsApp publication brokers exist behind neutral contracts. Provider payloads and failure paths are verified with fake transports. Image/text execution is reachable only after server-side configuration and an explicit operator action. Telegram/X have an explicit authorized queue and due-job worker. Account verification, queueing and provider execution remain disabled in the current local runtime. Fake transports verify their actual code paths.

Credentials, OAuth onboarding, live acceptance, public Instagram media hosting and WhatsApp consent/template operator flows remain setup/review seams. The local pipeline records X observations and can reconcile a known final X receipt through explicit bounded reads; missing receipt IDs cannot safely trigger an automatic lookup or repost. Results show unavailable values until actual observations exist. Pinterest and selected-file Google Drive access are researched follow-on integrations; see [provider research](../providers.md).

## Review artifacts

- [Six card comparisons](../../../output/verification/ocharo-card-comparison.pdf): three products, identical bone/charcoal geometry, three landscape pages.
- [Runtime assertions](../../../output/verification/runtime-evidence.json): selected media integrity, stable ZIP, revision safety, manual planning and cancellation.
- [Garment framing assertions](../../../output/verification/card-shape-evidence.json): long, wide and asymmetric front cutouts.
- [Browser navigation evidence](../../codebase/ocharo-marketing.frontend-services/evidence/navigation-qa.json).
- [Browser creation workflow evidence](../../codebase/ocharo-marketing.frontend-services/evidence/pipeline-qa.json).
- [Upload, scanning, provider controls and atlas evidence](../../codebase/ocharo-marketing.frontend-services/evidence/features-qa.json).
- [QA plan cleanup](../../codebase/ocharo-marketing.frontend-services/evidence/qa-plan-cleanup.json).

Local QA records are review fixtures. No actual post or message was sent, and no paid image-generation request was made.

## Review the new workflow

1. In **Mahsulotlar → Tasvirni tahlil qilish**, choose 1–3 existing source images. Analysis is an explicit remote action and stays disabled until OpenAI is configured. Refreshing readiness only reads the local workspace.
2. A completed analysis retains its original suggestion and image hashes. Edit **Siz tekshirgan matn**, save the review, then deliberately choose **Postda ishlatish**. The composer offers the reviewed wording for acceptance. None of these actions changes catalogue facts or approves a post.
3. In **Ulanishlar**, distinguish configured credentials, verified account identity and enabled execution. X identity verification alone does not prove write scopes or paid-read access.
4. In **Taqvim**, inspect a future approved plan. Eligible Telegram/X entries offer a confirmation displaying the frozen title/revision, caption, artwork, account, destination and time. Changes invalidate the acknowledgement. Cancelling before dispatch does not claim to unsend an existing post.
5. In **Natijalar**, cached observations retain their timestamps. A separate button requests a provider refresh and may consume X API credits. Observed zero, missing data and failed refresh are distinct; older measurements remain visible after a failed read. History exposes the latest 250 immutable observations and marks truncation.
6. For an unknown publication with an actual saved final X ID, explicit read-only reconciliation verifies its owner and existence. It never reposts. Unknown outcomes without that evidence require provider-history inspection.

New evidence: [isolated desktop/mobile workflow checks](../../codebase/ocharo-marketing.frontend-services/evidence/delivery-simulated-ui-qa.json). Simulated screenshots show explicitly fake accounts/counters to exercise enabled UI; they are not evidence of real account connection, publication or engagement. Actual [disconnected-runtime checks](../../codebase/ocharo-marketing.frontend-services/evidence/delivery-disconnected-ui-qa.json) and [final package/runtime evidence](../../../output/verification/delivery-increment.json) are recorded separately.

See [exact remaining setup and acceptance actions](external-setup.md). The working local app is the review artifact; no live account setup or remote deployment is required to inspect it.
