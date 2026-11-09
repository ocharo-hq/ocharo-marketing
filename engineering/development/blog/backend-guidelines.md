# ventures.ocharo-blog — Backend Development Guidelines

Defer to wow-two-ws/conventions/development/backend/. Repo deltas: application services for the document-style store, handlers return `Result` through the SDK mediator, with errors from `BlogErrors` mapped to ProblemDetails, revision-checked saves, all HTML built in `SiteRenderer` with encoding, lazily resolved paths.
