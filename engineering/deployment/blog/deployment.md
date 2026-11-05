# deployment/

*Last updated: 2026-11-04*

Wheelhouse deploys this image as the `blog` service and public site of `../deploy.yml`, with the published pages public and `/studio/` and `/api/blog/*` behind Ocharo's Identity (BFF client `editorial`). A static copy still comes from Sayt → Statik sayt (ZIP). Details: `../deployment.md` § Wheelhouse.
Rules: wow-two-ws/conventions/development/repo/structure/repo-structure.md

## Local settings

The Editorial API's `https` launch profile (HTTPS 8280) sets `ASPNETCORE_ENVIRONMENT=Development` and
`LOCAL_SETTINGS=true`, so the run reads `OcharoBlog.Api/appsettings.Local.json` through the SDK's `AddSettingsFiles`.
The file is gitignored and excluded from image builds; tests, review scripts and containers never read it.
`appsettings.Development.json` holds deployable defaults only. Machine values belong in the local file:

```json
{
  "Identity": {
    "Client": {
      "Authority": "https://localhost:8404",
      "ClientId": "editorial",
      "ClientSecret": "<the editorial client's secret>",
      "CookieName": "__Host-ocharo-editorial",
      "PostSignOutRedirectPath": "/"
    },
    "Resource": { "Uri": "https://localhost:8280/" }
  },
  "Blog": {
    "OrganizationId": "<Ocharo's organization id in that Identity>",
    "PlatformBaseUrl": "https://localhost:8264",
    "DeskBaseUrl": "https://localhost:8264",
    "SeedAssetRoot": "../../ocharo-marketing.frontend-services/apps/blog/public/assets"
  }
}
```

Startup refuses without the client block, `Identity:Resource:Uri` or `Blog:OrganizationId`. Without `Blog:DeskBaseUrl`, background removal reads unavailable. Without `Blog:PlatformBaseUrl`, catalogue and brand
reads use the retained pilots (`Blog:CatalogueBaseUrl`, `Blog:BrandBaseUrl`); without `Blog:SeedAssetRoot`, a first seed
of an empty database stores the stories without their cover images. The OpenAPI document maps only on a local run.
