# Enquiry follow-up desk

*Last updated: 2026-09-29*

## Implemented scope

Marketing's existing enquiry inbox now owns explicit next steps, a named follow-up owner,
an optional date, and immutable operator activity. This is an internal conversation workflow;
saving a follow-up sends no message, creates no order, and changes no product or price.

- `InquiriesPage.vue` provides status, search, due/scheduled/unassigned filters and 20-item pagination.
- Search matches name, contact, message, product reference, reference ID, note, owner and next step.
- A date requires an owner and next step; no date or frequency is preselected.
- Dates display in the operator browser's stated timezone and persist as UTC.
- Past dates are valid overdue work. Closed enquiries require clearing their scheduled date.
- Assignment is a free-text person's name, not an identity/role change or notification subscription.
- Edits block refresh, filters, page changes and navigation until explicitly saved or discarded.
- Failed requests, stale revisions and revoked sessions preserve in-memory edits.
- History reads serialize with writes so stale responses cannot overwrite the newly saved activity.

## Contract and storage

`GET /api/marketing/inquiries` accepts `status`, `search`, `followUp`, `offset` and `limit`.
It returns the filtered total, bounded page and `observedAt`, which defines the due comparison.

`PUT /api/marketing/inquiries/{id}` keeps existing revision/status/note fields and adds optional
`followUp: { assignedTo, followUpAt, nextStep }`. Omitting `followUp` preserves existing scheduling
for older clients. An explicit object with null fields clears the plan. A no-op creates no revision.

`GET /api/marketing/inquiries/{id}/activity` returns paginated snapshots in reverse revision order.
Each entry retains status, note, assigned name, date, next step, timestamp and server-authenticated actor.
The UI follows `nextBeforeRevision` as `beforeRevision` for earlier pages, so another operator appending
an event cannot shift or duplicate the remaining history. Offset pagination remains for older API clients.
Legacy notes remain on the enquiry; missing earlier history is never reconstructed.

The existing SQLite metadata storage gains additive JSON fields and separate
`inquiry-activity:{id}:{revision}` rows. The current record and its activity row commit in the same
transaction; failure to insert history rolls back the current record. No schema migration is required.

The configured Marketing instance still serves one organization. Connected access requires a current,
organization-bound Platform ticket; only editor roles mutate. The controller reads the actor from trusted
middleware context, never the request body. Missing connected identity fails closed. Explicit local mode
records `local-operator`. Public submission consent, idempotency and product validation remain unchanged.

## Verification

- Backend full suite: 251 passed, including enquiry legacy compatibility, atomic rollback,
  authenticated actor, due/search pagination, stable history during concurrent appends, optimistic conflict
  and role/organization boundaries.
- Frontend full suite: 47 passed, including six enquiry workflow tests.
- Frontend production build, typecheck, lint and source whitespace checks passed before final browser review.
- Review SQLite backup completed by the coordinating agent before its runtime restart.
- Browser verification passed: explicit owner/date/next step, UTC persistence, due filtering,
  authenticated actors, four-event history, conflict retention, revoked-session retention,
  reconnect/retry and closed-fixture exclusion. Created fixtures are explicitly synthetic and closed.
- Desktop and mobile screenshots were visually inspected; mobile search is 316 px wide at a 390 px viewport.
  Both layouts have no horizontal overflow. Final images: `.local/inquiry-review/{desktop,mobile}-final.png`.
- Final-runtime cursor API and Earlier activity UI passed with a two-event page-size override.
  All four existing events appeared once; the earlier-page button disappeared at the end.
- Browser receipts and repeatable QA scripts remain private in ignored `.local/inquiry-review/`.
- Authenticated QA logged out on completion. No publication, outbound message or order was created.

## Boundaries

The workflow does not send reminders or buyer messages. It does not infer contact frequency,
payment status, order placement, availability, or an organizational user from an assigned name.
Operator drafts are retained in the mounted page; browser refresh or closing the tab does not persist them.
