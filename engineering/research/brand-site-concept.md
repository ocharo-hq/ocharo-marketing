# Brand site concept — "Abr"

*Last updated: 2026-10-19*

A hook-level, high-resolution, animated concept site for the root domain: one recognisable motion
language, the way Apple's pinned product stage is Apple's. Research only — no build is authorized
by this document. Facts marked *(unverified)* or *(estimate)* rest on inference or one weak source.

## Status

- ✅ Reference teardown, technique catalogue, performance budget — `2026-10-19`
- ⬜ Host app: new root app vs reuse of `apps/wear` / `apps/atlas` stub
- ⬜ Concept approval — signature motion, chapter list, type
- ⬜ Asset shoot — macro cloth, warp/binding video, flatbed scans
- ⬜ MVP build

## Signature motion — the "abr register"

*Abr* ("cloud") names ikat's blurred motif edges, produced when resist-dyed warp threads sit
slightly out of register on the loom ([NGV](https://ngv.vic.gov.au/explore/collection/work/103998),
[LACMA](https://unframed.lacma.org/node/2530)). The site turns that mechanism into its one move:

- Every image enters cut into thin vertical warp strips, each offset by its own noise and colour-smeared.
- Scroll progress → 1 locks the strips into register; the motif resolves into cloth.
- Exits run the same effect in reverse.
- One OGL fragment shader. No WebGL → SVG `feDisplacementMap` or stepped `clip-path`.
  Reduced motion → plain crossfade.
- The abr softness also masks transition compression artifacts on high-frequency ikat.

Ikat colour appears only inside cloth images. The interface stays charcoal (~`#1F1E1C`) and ivory
(~`#F3EEE4`), matching the approved wordmark; each chapter may take one accent sampled from its own textile.

## Chapters

| # | Chapter | Moment | Assets needed |
|---|---|---|---|
| 1 | Thread (*ip*) | Hero: a silk thread stitches the needle mark into the wordmark (SVG stroke draw, Hermès-style) | Macro still of silk hanks (LCP image); mark as SVG |
| 2 | Bind & dye (*abrband*) | Colour blooms along bound threads — abr shader at full strength | Macro video of binding/dyeing, or shader over a flat scan |
| 3 | Weave | Pinned frame sequence: the pattern registers as strips align; tap → deep zoom of adras, khan-atlas, velvet ikat | 600+ dpi flatbed scans; 60–120-frame sequence |
| 4 | Cut | Line drawing of band collar, gathered waist, mid-calf over the front cutout; 3 sticky callouts | Existing front cutouts; technical line drawing |
| 5 | Garment | Six-mannequin group on a gutterless "wall" (Loewe); later a 3D turntable | Group shot (exists); optional new GLB |
| 6 | Atelier | Founder portrait → hands and needle in short loops | Portrait (exists, 640 px — too small for hero); three 4–6 s loops |
| 7 | Contact / wholesale | Lookbook request, buyer email, Telegram, WhatsApp | Form wired to Marketing lead intake |

Total scroll ≈ 12–15 viewport heights; one moving element per viewport; no scroll hijacking.

## References to study

| Site | Technique to take | Why |
|---|---|---|
| [Apple AirPods Pro](https://www.apple.com/airpods-pro/) | Pinned `<canvas>` frame sequence on a 200–500vh stage; one still on slow networks ([CSS-Tricks](https://css-tricks.com/?p=308477)) | User controls time; cheap fallback |
| [Hermès](https://www.hermes.com) | Hand-drawn line (Linda Merad) through navigation, early 2026 ([source](https://bigwalldecor.com/blogs/blog/hermes-hand-drawn-website-age-of-ai)) | The drawn line = brand thread; Ocharo's needle mark does the same |
| [Lacoste Polo Factory](https://www.awwwards.com/sites/lacoste-polo-factory) | Awwwards SOTD Jul 2026 — "where polos are born", Three.js | Making-process as story = thread → garment spine |
| [Max Mara Jacket Circle](https://www.awwwards.com/sites/max-mara-jacket-circle) | SOTD Apr 2026, GSAP + PixiJS (2D) | One garment + 2D canvas wins without heavy 3D |
| [The Tie-break](https://www.awwwards.com/sites/the-tie-break) | SOTD Sep 2026 — real photo/video treated inside WebGL | Photo first, CG second — right order for real product |
| [Miu Miu house](https://www.awwwards.com/sites/miu-miu-a-house-that-we-shaped) | SOTD Aug 2026 — products discovered inside a world | Model for the atelier chapter |
| [Loewe](https://www.work.co/news/inside-the-redesigned-loewe-site/) | 2025 Work & Co gutterless "shop walls" grid *(unverified — snippet only)* | Product as a dense object wall |
| [Loro Piana Workshop of Wonders](https://www.awwwards.com/sites/loro-piana-workshop-of-wonders) | WebGL story tied to physical QR displays | QR on hangtags/atelier wall → site, for tourists |
| [Bottega Veneta "Craft is our Language"](https://aeworld.com/fashion/bottega-veneta-marks-50-years-of-iconic-weave-with-craft-is-our-language-campaign/) | Hands and gesture over product | Atelier chapter direction |
| [Coutumes](https://www.awwwards.com/sites/coutumes) | Quiet, image-led Shopify | Later shop phase |

## Techniques

| Technique | Notes | Cost |
|---|---|---|
| Scroll-scrubbed frame sequence | Scroll → frame index → canvas in `requestAnimationFrame`. One decoded 1080×1350 RGBA frame ≈ 5.8 MB; iOS caps total canvas memory at 384 MB ([WebKit 195325](https://bugs.webkit.org/show_bug.cgi?id=195325)) → keep a sliding window of ~20 decoded frames. WebP for sequences (AVIF decodes slower on phones). *(Estimate: 6–12 MB per desktop sequence, 2–4 MB for 60 × 720 px mobile)* | Medium |
| Scroll-linked video | All-keyframe (`-g 1`) or every 6th; smaller than frames, less consistent seeking ([muffinman](https://muffinman.io/blog/scrubbing-videos-using-javascript/)) | Low–medium |
| WebGL shader (OGL) | Abr register + fabric displacement as one fragment shader ([Codrops](https://tympanus.net/codrops/2024/07/18/how-to-create-distortion-and-grain-effects-on-scroll-with-shaders-in-three-js/)); [OGL](https://github.com/oframe/ogl) is minimal, still alpha | Medium |
| GSAP + ScrollTrigger + Lenis | GSAP 100% free for commercial use incl. SplitText/MorphSVG since 30 Apr 2025 ([Webflow](https://webflow.com/blog/gsap-becomes-free)); [Lenis](https://github.com/darkroomengineering/lenis) respects reduced motion by default, native touch on phones | Low |
| CSS scroll-driven animations | Chrome 115+, Safari 26+; stable Firefox still lacks it ([caniuse](https://caniuse.com/mdn-css_properties_animation-timeline_scroll)) → minor reveals in `@supports` only | Low |
| View Transitions | Same-document Baseline incl. Firefox 144 ([web.dev](https://web.dev/blog/same-document-view-transitions-are-now-baseline-newly-available)) — garment detail open | Low |
| Deep zoom | libvips `dzsave` tiles + OpenSeadragon ([TIL](https://til.simonwillison.net/javascript/openseadragon)) — macro ikat | Low |
| 3D garment | glTF + Meshopt/Draco + KTX2. Existing GLBs in `ocharo-assets` (nimcha, baska, kostyum, korset, abaya) — none is the signature band-collar/gathered-waist/mid-calf cut | High |
| Gaussian splats | [Spark](https://github.com/sparkjsdev/spark) (MIT, three.js) — atelier room capture | High; phone risk |
| Kinetic type | GSAP SplitText + variable `wght`/`opsz`; fonts must carry U+02BB for Uzbek oʻ/gʻ | Low |

## Performance and accessibility budget

| Metric | Target |
|---|---|
| LCP (p75) | ≤2.5 s; aim ≤2.0 s on 4G phones ([web.dev](https://web.dev/articles/defining-core-web-vitals-thresholds)) |
| INP / CLS | ≤200 ms / ≤0.1 |
| First paint payload | ≤500 KB; JS ≤120 KB gz; three.js / OpenSeadragon lazy per chapter *(own targets)* |
| Whole scroll | ≤8–12 MB mobile, ≤20–25 MB desktop *(own targets)* |

Instagram in-app browser — where most traffic lands:

- iOS = customised WKWebView, Android = Chromium WebView; Meta injects JS.
- Low Power Mode blocks autoplay even for muted inline video; WebKit closed it WONTFIX ([219889](https://bugs.webkit.org/show_bug.cgi?id=219889)) → poster behind every video.
- WebGL context loss reported on iOS 18 ([289601](https://bugs.webkit.org/show_bug.cgi?id=289601)) → `webglcontextlost` falls back to stills.
- Toolbars shift the viewport → `svh` units. Referrer stripped → UTM on the bio link.
- Lite tier on the `Instagram` UA: no Lenis, 60-frame 720 px sequence, posters, no 3D.

Reduced motion: honour `prefers-reduced-motion` in CSS and JS; sequences → key stills, abr → crossfade;
never hide content behind motion ([WCAG C39](https://w3.org/WAI/WCAG22/Techniques/css/C39)).

Encoding:

- Hero stills AVIF at 4:4:4 chroma — 4:2:0 smears saturated ikat edges.
- Loops: HEVC/AV1 first, H.264 MP4 fallback; alpha = HEVC-alpha (Safari) + VP9-alpha (Chrome); AV1 has no alpha ([Jake Archibald](https://jakearchibald.com/2024/video-with-transparency/)).
- HLS only for a founder film over ~20 s.
- Ikat moiré: Lanczos downscale, no sharpening, pattern repeat ≥ ~8 px on screen *(estimate)*.

Hosting: Cloudflare Pages (25 MiB per-file cap, [changelog](https://developers.cloudflare.com/changelog/post/2026-01-23-pages-file-limit-increase/)) + R2 for video; Bunny CDN/Stream as alternative *(prices unverified)*.

## Stack

- Vue 3 + strict TS + Vite + Tailwind v4 — the WoW2 frontend standard; static prerendered page, no Nuxt.
- Vue 3.6 Vapor skipped — still a release candidate; the bottleneck is media/GPU, not the VDOM.
- GSAP 3.13+ (ScrollTrigger, SplitText) · Lenis desktop-only · OGL for 2D shaders.
- three.js r186 only if a 3D chapter ships — the Assets Motion lab already runs Vue 3.5 + three 0.186.
- Frame-sequence player, reduced-motion composable and in-app-browser tiering are reusable → build in
  `@wow-two-beta/fe` per the SDK doctrine.

## Effort

| Scope | Contents | Dev days |
|---|---|---|
| MVP | 5 chapters, abr shader + fallbacks, 1 frame sequence, reduced motion, IG lite tier, contact links, asset pipeline (sharp, avifenc, ffmpeg), device QA | 12–16 |
| Full | 7 chapters, 3 sequences, deep zoom, 3D signature cut, founder film (HLS), EN/RU/UZ, kinetic type, award polish | 35–40 + 5–10 days 3D modelling |

Asset production (shoot + post) is separate: ~5–8 days. The hook depends more on macro cloth
photography than on code — the existing 640 px portrait and phone product shots cannot carry a
4K hero.

## Risks

- Motion overload → one moving element per viewport, 12–15 vh total.
- IG in-app browser memory, context loss, Low Power Mode → test on an iPhone in Low Power Mode and a mid-range Android.
- AI video drifts collars, patterns and silhouettes → camera moves on real stills only, never garment motion; disclose. See [Seedance](seedance-video.md).
- GSAP licence is Webflow's to change; already-released versions stay usable.
