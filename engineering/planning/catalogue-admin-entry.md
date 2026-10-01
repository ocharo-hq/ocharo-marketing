# Catalogue admin entry

*Last updated: 2026-09-30*

## Scope

The live Catalogue footer contains a small `Catalogue admin` link when the host explicitly supplies
`Blog:CatalogueAdminUrl` (`Blog__CatalogueAdminUrl` in environment configuration).
The setting has no default URL. Atlas, Wear and editorial previews do not display the link.

The destination must be an absolute HTTPS URL without user information, a query, fragment or control characters.
Invalid configuration omits the entry. The link supplies no organization identifier, credentials or access grant;
Platform authenticates the operator and determines membership after navigation.

Every static-export artifact omits the admin entry, including an explicitly configured public or loopback URL.
The live link remains available after an export. Existing public navigation and catalogue rendering are preserved.

## Verification

- 21 new admin-entry cases passed, covering live Catalogue routes, default/invalid configuration,
  publication boundaries, preview omission, and complete static-export exclusion.
- 45 focused publication/catalogue regression tests passed; source whitespace checks passed.
- Anonymous browser verification on the local review runtime followed the footer entry to
  `https://localhost:8340/catalogue`, which retained `/catalogue` while displaying operator sign-in.
- Atlas and Wear omitted the link; browser JavaScript errors were empty.
- Footer and sign-in screenshots were visually inspected.
- Receipt and screenshots: ignored `.local/catalogue-admin-review/{result.json,footer.png,sign-in.png}`.
- No sign-in, source edit, export publication, or authenticated mutation was performed by this verification.

## Edition baseline

All three existing public edition identities, source revisions, page metadata and file links matched exactly
before and after anonymous browser verification. Each edition still contains 16 pages at source revision 1.
This comparison reads the published edition; it does not inspect or edit private saved-design documents.

| Locale | Edition ID | Source design ID |
|---|---|---|
| UZ | `53d8b70a-73f3-4651-9e24-68c0fcb59278` | `74039bfe-5c80-48e4-9d89-016507f06760` |
| RU | `ce698b1e-4ad8-4f94-ae3a-9835ba7439b4` | `c02d7dfc-e84f-47eb-bffa-6a60e1dc82ce` |
| EN | `88b1d32f-9d6b-403a-a1a8-589ee4907ce0` | `a1994839-fb48-423e-a80a-271a1967434f` |
