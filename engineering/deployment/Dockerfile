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
ARG APP_VERSION=0.0.0
ENV APP_VERSION=$APP_VERSION
WORKDIR /spa
RUN corepack enable && corepack prepare pnpm@10.33.2 --activate
COPY ocharo-marketing.frontend-services/ ./
RUN pnpm install --frozen-lockfile
RUN pnpm --filter @ocharomarketing/web build

# ---- Stage 2: publish the .NET API (no Node here; the SPA arrives from stage 1) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG APP_VERSION=0.0.0
WORKDIR /src
COPY ocharo-marketing.backend-services/ ./
# the built SPA lands in wwwroot so `dotnet publish` bundles it into the output.
COPY --from=spa /spa/apps/web/dist ./OcharoMarketing.Api/wwwroot
RUN dotnet publish OcharoMarketing.Api/OcharoMarketing.Api.csproj -c Release -o /app -p:BuildSpa=false -p:Version=${APP_VERSION}

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
