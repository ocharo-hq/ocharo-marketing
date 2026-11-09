# Product context

*Last updated: 2026-10-24*

## Current state

Ocharo Marketing was scaffolded independently and is in an active continuous build. Its developer authorized the app for Maftuna first, with later merge/generalization and SDK extraction decisions. Its delivery boundaries are [below](#delivery-boundaries); evidence files are in [operations](../engineering/operations/operations.md).

## Decisions

- Preserve Brand Workspace and Ocharo Studio as independent work lanes.
- Reuse real existing product records and front cutouts; retain draft copy status.
- Start with T02 bone/charcoal and T01 right-fabric candidates.
- Separate WhatsApp customer messaging from public-channel publishing.
- Keep unknown metrics and delivery outcomes explicit.
- Both hosts sign in through Ocharo's Identity, with published pages and `/api/public/*` anonymous; live account verification remains separate work.

## Review

The user plans to review and brainstorm after the continuous build. Specific presets, final copy, generated-image fidelity, and hosted access remain review topics. Implementation authorization does not imply approval of those outputs.

## Delivery boundaries

Binding decisions carried from the retired build handoff; evidence lives in [operations](../engineering/operations/operations.md).

- Manual scheduling never authorizes external publication.
- Telegram and X publish only to a verified server-owned destination, with execution enabled, the current approved
  revision and a separate queue confirmation; the frozen operation keeps copy, artwork hashes and identity.
- Disabling execution or failing or changing account verification revokes unstarted authorization; re-enabling does not
  revive it.
- An unknown delivery outcome never reposts automatically; missing ids and Telegram unknowns need a manual provider
  history check.
- Instagram stays out of automatic queueing until reviewed JPEG public-hosting delivery exists; WhatsApp needs its own
  consent, template and window workflow.
- Weekly posting slots plan only and never queue delivery; a standing auto-queue consent is an owner decision.
- Source-image analysis keeps the original hashes and suggestions; reviewing it changes neither catalogue facts nor post
  approval.
- Paid generation (OpenAI, Gemini) is configured explicitly and stays unexercised until the owner authorizes it.
