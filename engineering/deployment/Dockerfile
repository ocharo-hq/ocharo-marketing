# syntax=docker/dockerfile:1

# Unverified future-host packaging reference. Current application deliberately
# rejects Production startup and non-loopback callers. Authentication and a
# Linux browser/native-image runtime must be implemented before deployment.
#
# Multi-stage build: ONE image serving the .NET API + the Vue SPA. The build
# context MUST be engineering/codebase/, which holds BOTH ocharo-marketing.backend-services
# and ocharo-marketing.frontend-services.
#
#   docker build -f engineering/deployment/Dockerfile -t ocharomarketing engineering/codebase
#
# No supported container run command is provided for this local-only pilot.

# ---- Stage 1: build the Vue workspace ----
FROM node:24.11.0-alpine AS spa
WORKDIR /spa
RUN corepack enable && corepack prepare pnpm@10.33.2 --activate
COPY ocharo-marketing.frontend-services/package.json ocharo-marketing.frontend-services/pnpm-workspace.yaml ocharo-marketing.frontend-services/pnpm-lock.yaml ./
COPY ocharo-marketing.frontend-services/apps/web/package.json ./apps/web/
RUN pnpm install --frozen-lockfile
# Copy authored inputs explicitly; host node_modules and generated output stay out.
COPY ocharo-marketing.frontend-services/apps/web/index.html ocharo-marketing.frontend-services/apps/web/vite.config.ts ocharo-marketing.frontend-services/apps/web/vitest.config.ts ocharo-marketing.frontend-services/apps/web/tsconfig*.json ./apps/web/
COPY ocharo-marketing.frontend-services/apps/web/src/ ./apps/web/src/
COPY ocharo-marketing.frontend-services/apps/web/scripts/ ./apps/web/scripts/
COPY ocharo-marketing.frontend-services/apps/web/tests/ ./apps/web/tests/
RUN pnpm build

# ---- Stage 2: publish the .NET API (no Node here; the SPA arrives from stage 1) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ocharo-marketing.backend-services/ ./
# the built SPA lands in wwwroot so `dotnet publish` bundles it into the output.
COPY --from=spa /spa/apps/web/dist ./OcharoMarketing.Api/wwwroot
RUN dotnet publish OcharoMarketing.Api/OcharoMarketing.Api.csproj -c Release -o /app -p:BuildSpa=false

# ---- Stage 3: runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app ./

# SQLite store on a volume so data survives container replacement.
ENV ConnectionStrings__OcharoMarketing="Data Source=/data/ocharomarketing.db"
ENV Marketing__DataDirectory="/data"
RUN mkdir -p /data && chown app:app /data
VOLUME ["/data"]

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "OcharoMarketing.Api.dll"]
