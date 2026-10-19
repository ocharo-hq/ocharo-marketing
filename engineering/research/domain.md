# Domain

*Last updated: 2026-10-19*

Which domain hosts the Ocharo brand site and, later, the four public apps
(Atlas, Wear, Blog, Catalogue) as subdomains. Nothing is purchased; the user completes checkout.

## Status

- ✅ Availability checked by RDAP / registry whois, `2026-10-19`
- ✅ Prices read from Porkbun's public pricing API, `2026-10-19`
- ⬜ Public spelling decision — `Ocharo` vs `Ocharostile` (`och-t-005` in the business workspace)
- ⬜ Purchase, DNS and hosting — after the spelling decision

## Availability, `2026-10-19`

| Domain | State | Porkbun 1st year | Renewal / yr |
|---|---|---|---|
| `ocharo.com` | **Taken** — GoDaddy, registered `2011-04-15`, expires `2027-04-15`, parked; "Get This Domain" routes to GoDaddy's paid broker service, no listed price | — | — |
| `ocharostile.com` | Free — matches Instagram `@ocharostile` | $11.08 | $11.08 |
| `ocharostyle.com` | Free — the likely misspelling | $11.08 | $11.08 |
| `ocharoatelier.com` | Free | $11.08 | $11.08 |
| `wearocharo.com` | Free | $11.08 | $11.08 |
| `ocharo.uz` | Free (`whois.cctld.uz`: not found) | ~$28 cheapest international registrar (aggregator, Jul 2026) | ~$28 |
| `ocharo.style` | Free | $7.21 | $34.50 |
| `ocharo.studio` | Free | $11.84 | $41.71 |
| `ocharo.fashion` | Free | $26.26 | $26.26 |
| `ocharo.co` | Free | $15.76 | $31.20 |
| `ocharo.store` / `.shop` | Free | $2.57 / $2.06 | $43.77 / $31.41 |
| `ocharo.eu` | Free, **not eligible** — registrant must live in the EU/EEA | — | — |

Also free: `ocharo.design`, `.house`, `.art`, `.net`, `.io`, `.clothing`, `.boutique`, `.moda`,
`ocharo-atelier.com`, `ocharostile.uz`, `ocharostyle.uz`.

Promo first-year prices on new gTLDs (`.store`, `.shop`, `.style`, `.studio`) renew at 4–17×;
budget the renewal column.

## Reading

- A `.com` is the trust default for the European/tourist audience the site targets.
- `ocharo.com` is held by a long-term owner, not a reseller listing. Acquisition means a GoDaddy
  broker request plus an unknown price; worth one attempt later, not a launch dependency.
- `.uz` signals a local business and helps Uzbek search; it does not replace a `.com` for Europe.
- The domain follows the public spelling. The logo reads `OCHARO`; Instagram reads `ocharostile`;
  the brand review leaves that unresolved and flags the stile/style ambiguity.

## Options

| Option | Primary | Companions | Year 1 | Fits |
|---|---|---|---|---|
| A — handle parity | `ocharostile.com` | `ocharostyle.com` → redirect | ~$22 | Public name stays `Ocharostile` |
| B — wordmark parity | `ocharoatelier.com` | `ocharo.uz` → redirect | ~$39 | Public name becomes `Ocharo`; "atelier" signals craft to Europeans |
| C — short vanity | `ocharo.style` | `ocharostile.com` → redirect | ~$18, then ~$46/yr | Wants the shortest URL; accepts a weaker TLD |

Recommendation: decide the spelling first. With `Ocharo` → B. With `Ocharostile` → A.
Either way, register `ocharo.uz` as a cheap defensive/local redirect.

## Purchase path

1. Registrar: Cloudflare Registrar (sells at wholesale cost, 400+ TLDs, DNS stays on Cloudflare,
   which also hosts the static site) or Porkbun (prices above, free WHOIS privacy).
   `.uz` needs a registrar that sells `.uz`; neither of these is confirmed to.
2. Payment: an internationally enabled Visa/Mastercard. Domestic Humo/Uzcard cards are not
   expected to work on foreign registrars — verify at checkout.
3. Registrant: Maftuna or her `YaTT` as the owner of record, with an email she controls.
   The account and payment are the user's; agents do not create accounts or pay.
4. After purchase: enable auto-renew, registrar lock and WHOIS privacy; point DNS to the site host.

## Subdomain plan

Root = brand concept site. `atlas.`, `wear.`, `blog.`, `catalogue.` host the four public apps,
keeping them separately styled and deployed as `docs/architecture.md` (workspace) requires.

## Sources

- RDAP via `rdap.org` and registry whois (`whois.cctld.uz`, `whois.eu`, `whois.nic.store`), `2026-10-19`
- Porkbun pricing API `api.porkbun.com/api/json/v3/pricing/get`, `2026-10-19`
- `.uz` prices: [domainoffer.net .UZ](https://domainoffer.net/tld/uz) (aggregator, Jul 2026)
- `.eu` eligibility: [Spaceship .eu requirements](https://www.spaceship.com/knowledgebase/eu-domain-requirements/), Regulation (EU) 2019/517
- Cloudflare Registrar: [domains.cloudflare.com](https://domains.cloudflare.com/)
- Spelling context: business workspace `och-brand-review.md` § Naming options
