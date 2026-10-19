# Seedance AI video

*Last updated: 2026-10-19*

Whether ByteDance's Seedance can produce Ocharo's Reels, website hero loops and a short brand film
from real product photos. Research only — no account, generation or spend is authorized by this document.
Items marked *(unverified)* could not be confirmed from a primary source.

## Status

- ✅ Versions, access, pricing, fidelity, alternatives, licensing — `2026-10-19`
- ⬜ Access test — ModelArk or fal account, Uzbek Visa card accepted
- ⬜ Pilot — 3 hardest shots through Seedance 2.0, Kling 3.0, Veo 3.1 Fast
- ⬜ Pipeline adoption decision

## Verdict

- Seedance 2.0 for product shots: native 1080p/4K, real photo as first and last frame, cheaper than 2.5.
- Seedance 2.5 only for a 30–60 s one-take brand film.
- Ikat close-ups get no AI motion — fine repeating motifs "swim" between frames.
- Real faces are refused on upload, so the founder portrait cannot be animated without BytePlus
  real-person verification and consent.
- The logo is overlaid in the edit, never generated.

## Versions

| Version | Released | Resolution | Length | Inputs |
|---|---|---|---|---|
| 1.0 | Jun 2025 | — | — | — |
| 1.5 Pro | Dec 2025 | 1080p | — | Native audio |
| 2.0 / Fast / Mini | 12 Feb 2026 | 480p–4K (native 3840×2160 since Jun 2026, vendor source) | 4–15 s | First + last frame, ≤9 images, 3 videos, 3 audio refs; 21:9 → 9:16 |
| 2.5 | 31 Jul 2026 | 480p–1080p (no 4K) | 4–30 s per pass, extendable | ≤30 images, 10 videos, 10 audio refs; multi-shot, camera control, timestamp edits |

- Frame rate ≈ 24 fps *(unverified — inferred from fal's token formula)*.
- 2.0 and 2.5 return HTTP 400 on uploads containing real human faces. Mannequin faces *(unverified)*.

Sources: [Seed blog 2.5](https://seed.bytedance.com/en/blog/one-take-creation-flexible-referencing-introducing-seedance-2-5) ·
[fal 2.0 schema](https://fal.ai/models/bytedance/seedance-2.0/image-to-video/llms.txt) ·
[arXiv 2604.14148](https://arxiv.org/abs/2604.14148) ·
[real-face error](https://seedrouter.ai/blog/seedance-real-person-error) ·
[BytePlus blog](https://www.byteplus.com/en/blog/dreamina-seedance2-5)

## Access from Uzbekistan

| Route | State | Notes |
|---|---|---|
| BytePlus ModelArk (official API) | Uzbekistan on the service country list (`2026-04-21`); Seedance "Restricted Model" status *(unverified)* | Account + auth; card or bank transfer; visible watermark + C2PA |
| fal.ai | Terms exclude only US-embargoed countries | 2.0 up to 4K, 2.5 up to 1080p; ~1.8× ModelArk price |
| Replicate | Available | 2.5 capped at 720p |
| Higgsfield / Freepik / Krea | Credit subscriptions | Account required |
| Dreamina / CapCut | Rolled out to Europe, US, Middle East, Africa, LatAm; Central Asia not named *(unverified)* | Invisible watermark + C2PA |
| Volcengine / Jimeng (China) | Impractical | Chinese real-name ID, RMB payment |

Payment: Uzbek banks issue USD Visa/Mastercard for international use; acceptance per service *(unverified)*.
Sources: [BytePlus availability](https://docs.byteplus.com/en/docs/modelark/availability) ·
[fal ToS](https://www.fal.ai/legal/terms-of-service) ·
[CapCut newsroom](https://www.capcut.com/newsroom/dreamina-seedance-2)

## Pricing — ModelArk list, $ per second

| Model | 480p | 720p | 1080p | 4K |
|---|---|---|---|---|
| 2.0 | 0.070 | 0.152 | 0.374 | 0.778 |
| 2.0 Fast | — | 0.120 | — | — |
| 2.0 Mini | — | 0.076 | — | — |
| 2.5 | 0.103 | 0.231 | 0.569 | — |
| 1.5 Pro (audio) | — | — | 0.116 | — |

- fal: 2.0 $0.30 (720p) / $0.68 (1080p); 2.5 $0.47 / $1.16.
- Higgsfield $19–129/mo incl. commercial use; Dreamina ~$17–520/mo, credit cost per second unpublished.
- No free tokens on ModelArk.

Source: [ModelArk pricing](https://ai.byteplus.com/ark/region:ap-southeast-1/docs/ModelArk/1544106), read `2026-10-19`.

## Fashion fidelity

- No controlled test exists for dense repeating motifs like ikat, for any model.
- Reviewers report 2.0 holds prints and colour "more reliably", still weak on very fine texture,
  layered outfits and structured silhouettes ([AllHipHop](https://allhiphop.com/uncategorized/fashion-video-production-at-scale-maintaining-model-style-consistency-with-seedance-2-0/)).
- Known failures: pattern warp, shifting closures, merged layers, fabric clipping, wrong fabric weight,
  hands mangling collars ([SweetSofties](https://www.sweetsofties.com/2026/08/creating-fashion-films-with-seedance-20-from-lookbook-images-to-cinematic-motion.html)).
- Diffusion re-guesses fine detail every frame → ikat shimmer; upscaling amplifies it → deflicker before upscale.
- Vendor ranking ([Morphed](https://morphed.app/blog/ai-video-generator-for-fashion)): Kling V3 Pro and Veo 3.1 "Excellent", Seedance 2.0 "Very good".
  Flat-lays animate poorly.
- Text/logo rendering: no reliable evidence.

## Alternatives

| Model | Max res / length | Fashion evidence | $/s 1080p | Access from UZ |
|---|---|---|---|---|
| Seedance 2.0 | 4K / 15 s | Very good; strong input-photo adherence | 0.374 (4K 0.778) | ModelArk*, fal |
| Seedance 2.5 | 1080p / 30 s | None found | 0.569 | ModelArk*, fal |
| Kling 3.0 (4.0 rolling out) | 4K / 15 s | #1 in Morphed test | ~0.17; 4K 0.42 | *(unverified)* |
| Veo 3.1 | 4K / 8 s | Excellent; some over-smoothing | 0.40 Std / 0.12 Fast | Gemini API (UZ listed) |
| Runway Gen-4.5 | 720p API / 10 s | None found | 0.12 (720p) | *(unverified)* |
| MiniMax H3 | 2K / 15 s | Hailuo 2.3 "Good" | 0.13 (2K) | *(unverified)* |
| Luma Ray 3.2 | 1080p / 10 s | None found | 0.24 | *(unverified)* |
| Sora 2 | — | API removed `2026-09-24` | — | — |

\*Restricted-model status unconfirmed. Sources: [Gemini pricing](https://ai.google.dev/gemini-api/docs/pricing) ·
[Gemini regions](https://ai.google.dev/available_regions) · [Teamday roundup](https://www.teamday.ai/blog/best-ai-video-models-2026)

## Licensing and disclosure

- BytePlus Playground: internal/non-commercial only. API terms put output responsibility on the
  customer; ownership clause *(unverified — page did not render)*.
- fal claims no IP in outputs; upstream model terms may apply. Kling free tier excludes commercial rights.
- Instagram: photorealistic AI video must be disclosed; Meta reads C2PA and applies "AI info"
  automatically — Seedance outputs carry C2PA, so expect the label ([TechCrunch](https://techcrunch.com/?p=2660923)).
- EU AI Act Art. 50 (in force `2026-08-02`): deepfake definition covers objects; labelling applies to
  non-EU deployers whose output is used in the EU ([EC](https://digital-strategy.ec.europa.eu/en/factpages/quick-facts-transparency-rules-ai-systems)).
- Caption line: "animated from real product photos". For a craft brand selling to buyers, invented
  cloth or cut is a misrepresentation risk.

## Pipeline

| Shot | Method |
|---|---|
| Ikat close-up | No AI motion — real photo + parallax / Ken Burns in the edit |
| Product Reel 9:16 | Seedance 2.0 1080p; real mannequin/on-body photo as first frame, same or second real angle as last frame; low-motion prompt |
| Front cutouts | Composite into a styled still first, then slow camera move only |
| Hero loop 16:9 | Seedance 2.0 native 4K, first frame = last frame for a seamless loop; or 1080p → deflicker → Topaz upscale |
| Atmosphere b-roll (no product) | Seedance 2.0/2.5 text-to-video: looms, thread, courtyards |
| Founder | Parallax in the edit; Seedance only after consent verification |
| Brand film 30–60 s | Seedance 2.5 with reference images, intercut with real footage |

QC: frame-by-frame check of band collar, gathered waist, mid-calf hem and motif registration;
reject any clip that alters the cut. Logo overlaid in the edit.

## Monthly cost estimate

Assumption: 20 Reels × 15 s + 3 hero loops × 10 s, ~3 generations per keeper ≈ 1,000 s generated.

| Line | $/month |
|---|---|
| All at 2.0 1080p (ModelArk) | ~375 |
| Lean: 480p drafts, ~1.3 finals per keeper at 1080p | ~210 |
| Hero loops at native 4K | +~50 |
| Same volume on fal | ~680 |
| Topaz Video (annual plan) | +39 |
| One-off 60 s brand film, 2.5 1080p, 4 attempts | ~140 once |

Topaz commercial licence is limited to organisations under $1M revenue ([Topaz](https://www.topazlabs.com/pricing)).
