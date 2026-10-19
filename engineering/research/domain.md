# Domain

*Last updated: 2026-10-19*

Which domain hosts the Ocharo brand site and, later, the four public apps
(Atlas, Wear, Blog, Catalogue) as subdomains. Nothing is purchased; the user completes checkout.

## Status

- ✅ Availability checked by RDAP / registry whois, `2026-10-19`
- ✅ Prices read from Porkbun's public pricing API, `2026-10-19`
- ✅ Spelling decided by the user, `2026-10-19`: the domain follows `Ocharo`
- ✅ Main domain decided by the user, `2026-10-19`: `ocharo.co` (supersedes `ocharoatelier.com`)
- ✅ `ocharo.co` and `ocharo.uz` re-checked free, `2026-10-19`
- ⬜ Purchase `ocharo.co` — user checkout at Cloudflare
- ⬜ Purchase `ocharo.uz` — user checkout at aHOST
- ⬜ DNS, `ocharo.uz` → `ocharo.co` 301, hosting — after purchase

## Decision

| Domain | Role | Registrar | Price |
|---|---|---|---|
| `ocharo.co` | Main — brand site root, app subdomains | Cloudflare Registrar (at cost; DNS + Pages + R2 in one account) | $30.00/yr flat at Cloudflare, read `2026-10-19` |
| `ocharo.uz` | Defensive only → 301 to `ocharo.co` | aHOST ([cart](https://clients.ahost.uz/cart.php?a=add&domain=register&language=russian)) or another [cctld.uz registrar](https://cctld.uz/reg/?lang=eng) | 27,000 so'm/yr at aHOST, read `2026-10-19` |

Why international over `.uz` for the main domain (user preference, `2026-10-19`):

- `.uz` is run by the state registry UZINFOCOM; disputes go to Uzbek courts. `.co` follows ICANN's UDRP.
- Cloudflare and Porkbun do not sell `.uz`, so it cannot share the DNS/hosting account.
- Google treats `.co` as generic but geotargets `.uz` to Uzbekistan.

Known cost of `.co`: visitors who type `ocharo.com` from memory land on GoDaddy's parked page.
A broker attempt on `ocharo.com` stays a later option.

The Instagram handle `@ocharostile` is unchanged by this decision. Whether the business record's
`och-t-005` closes on `Ocharo` stays with the business workspace.

## Availability, `2026-10-19`

| Domain | State | Porkbun 1st year | Renewal / yr |
|---|---|---|---|
| `ocharo.com` | **Taken** — GoDaddy, registered `2011-04-15`, expires `2027-04-15`, parked; "Get This Domain" routes to GoDaddy's paid broker service, no listed price | — | — |
| `ocharostile.com` | Free — matches Instagram `@ocharostile` | $11.08 | $11.08 |
| `ocharostyle.com` | Free — the likely misspelling | $11.08 | $11.08 |
| `ocharoatelier.com` | Free | $11.08 | $11.08 |
| `wearocharo.com` | Free | $11.08 | $11.08 |
| `ocharo.uz` | Free (`whois.cctld.uz`: not found) | 27,000 so'm at aHOST; ~$28 at international resellers | 27,000 so'm |
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

## Short names, `2026-10-19`

Every `ocharo.*` checked is free except `ocharo.com` (registry whois/RDAP). Porkbun prices, sorted by renewal:

| Domain | Length | 1st year | Renewal / yr | Note |
|---|---|---|---|---|
| `ocharo.uz` | 9 | 27,000 so'm (aHOST) | 27,000 so'm | Origin signal; Google geotargets it to Uzbekistan |
| `ocharo.me` | 9 | $17.27 | $17.27 | Google treats as generic |
| `ocharo.co` | 9 | $15.76 | $31.20 | Google treats as generic; reads as `.com`, so typos leak to the parked `ocharo.com` |
| `ocharo.tv` | 9 | $26.26 | $26.26 | Google treats as generic |
| `ocharo.io` / `.ai` / `.to` / `.am` / `.la` | 9 | $28–83 | $36–83 | Tech/vanity; `.ai` has a 2-year minimum |
| `ocharo.org` / `.net` | 10 | $7.98 / $12.52 | $11.84 / $12.52 | Non-profit / generic connotation |
| `ocharo.art` | 10 | $3.60 | $21.11 | Craft/art signal |
| `ocharo.app` / `.page` | 10–11 | $8.75 / $10.81 | $14.93 / $10.81 | HTTPS-only TLDs; product connotation |
| `ocharo.studio` | 13 | $11.84 | $41.71 | Atelier signal |
| `ocharo.fashion` | 14 | $26.26 | $26.26 | Explicit category |

Also free and low-trust: `.xyz`, `.cc`, `.vip`, `.club`, `.one`, `.biz`, `.info`, `.online`, `.site`.
`ocharo.us` needs a US nexus. Free `.com` prefixes: `byocharo.com` (12), `getocharo.com`,
`myocharo.com`, `ocharohq.com`, `ocharouz.com` (13), `theocharo.com` (13).

Generic-ccTLD list: [Google Search Central](https://developers.google.com/search/docs/specialty/international/managing-multi-regional-sites).

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
| B — wordmark parity | `ocharoatelier.com` | `ocharo.uz` → redirect | ~$13 | Public name becomes `Ocharo`; "atelier" signals craft to Europeans |
| C — short vanity | `ocharo.style` | `ocharostile.com` → redirect | ~$18, then ~$46/yr | Wants the shortest URL; accepts a weaker TLD |
| D — short international (chosen) | `ocharo.co` | `ocharo.uz` → redirect | ~$32 | Shortest generic name; see § Decision |

Superseded: the user first chose B, then asked for shorter names and chose D.

## Purchase path

1. Registrar: Cloudflare Registrar (sells at wholesale cost, 400+ TLDs, DNS stays on Cloudflare,
   which also hosts the static site) or Porkbun (prices above, free WHOIS privacy).
   `.uz` goes through a local registrar — aHOST charges 27,000 so'm/yr and pays in so'm,
   far below the ~$28 international resellers charge.
2. Payment: an internationally enabled Visa/Mastercard. Domestic Humo/Uzcard cards are not
   expected to work on foreign registrars — verify at checkout.
3. Registrant: Maftuna or her `YaTT` as the owner of record, with an email she controls.
   The account and payment are the user's; agents do not create accounts or pay.
4. After purchase: enable auto-renew, registrar lock and WHOIS privacy; point DNS to the site host.

## Subdomain plan

Root `ocharo.co` = brand concept site. `atlas.`, `wear.`, `blog.`, `catalogue.` host the four public apps,
keeping them separately styled and deployed as `docs/architecture.md` (workspace) requires.

## Sources

- RDAP via `rdap.org` and registry whois (`whois.cctld.uz`, `whois.eu`, `whois.nic.store`), `2026-10-19`
- Porkbun pricing API `api.porkbun.com/api/json/v3/pricing/get`, `2026-10-19`
- `.uz` prices: [domainoffer.net .UZ](https://domainoffer.net/tld/uz) (aggregator, Jul 2026)
- `.eu` eligibility: [Spaceship .eu requirements](https://www.spaceship.com/knowledgebase/eu-domain-requirements/), Regulation (EU) 2019/517
- Cloudflare Registrar: [domains.cloudflare.com](https://domains.cloudflare.com/)
- Spelling context: business workspace `och-brand-review.md` § Naming options
