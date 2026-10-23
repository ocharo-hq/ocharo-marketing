# deployment/

*Last updated: 2026-10-24*

Wheelhouse deploys this image as the `blog` service and public site of `../deploy.yml`, with `/studio/` and `/api/blog/*` open without sign-in until the Ocharo identity server lands (owner decision 2026-10-24); a local run keeps the loopback boundary. A static copy still comes from Sayt → Statik sayt (ZIP). Details: `../deployment.md` § Wheelhouse.
Rules: wow-two-ws/conventions/development/repo/structure/repo-structure.md

## Local settings

The Editorial API's `https` launch profile (HTTPS 8280) sets `ASPNETCORE_ENVIRONMENT=Development` and
`LOCAL_SETTINGS=true`, so the run reads `OcharoBlog.Api/appsettings.Local.json` through the SDK's `AddSettingsFiles`.
The file is gitignored and excluded from image builds; tests, review scripts and containers never read it.
`appsettings.Development.json` holds deployable defaults only. Machine values belong in the local file:

```json
{
  "PlatformIntegration": { "BaseUrl": "https://localhost:8264", "OrganizationId": "<Desk organization id>" },
  "Blog": {
    "PlatformBaseUrl": "https://localhost:8264",
    "SeedAssetRoot": "../../ocharo-marketing.frontend-services/apps/blog/public/assets"
  }
}
```

Without it the run stays in unconfigured connected mode, catalogue and brand reads use the retained pilots
(`Blog:CatalogueBaseUrl`, `Blog:BrandBaseUrl`), and a first seed of an empty database stores the stories without
their cover images. The OpenAPI document maps only on a local run.
