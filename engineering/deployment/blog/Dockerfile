# syntax=docker/dockerfile:1

# The `blog` service of deploy.yml: the Editorial API, its public renderer and the /studio/ editor. A deployed host runs
# without sign-in until the Ocharo identity server lands (owner decision 2026-10-24); a local run keeps the loopback pilot.
#
# Multi-stage build: ONE image serving the .NET API + the Vue SPA. The build
# context MUST be engineering/codebase/, which holds BOTH ocharo-blog.backend-services
# and ocharo-blog.frontend-services.
#
#   docker build -f engineering/deployment/blog/Dockerfile -t ocharoblog engineering/codebase
#
#   docker run -d --name ocharoblog -p 127.0.0.1:8200:8080 -v ocharoblog-data:/data ocharoblog
#
# On first run the app creates /data/ocharo-blog.db automatically.

# ---- Stage 1: build the Vue workspace ----
FROM node:24.11.0-alpine AS spa
ARG APP_VERSION=0.0.0
ENV APP_VERSION=$APP_VERSION
WORKDIR /spa
RUN corepack enable && corepack prepare pnpm@10.33.2 --activate
COPY ocharo-marketing.frontend-services/ ./
RUN pnpm install --frozen-lockfile
RUN pnpm --filter @ocharomarketing/editorial build

# ---- Stage 2: publish the .NET API (no Node here; the SPA arrives from stage 1) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG APP_VERSION=0.0.0
WORKDIR /src
COPY ocharo-blog.backend-services/ ./
# the built SPA lands in wwwroot so `dotnet publish` bundles it into the output.
COPY --from=spa /spa/apps/editorial/dist ./OcharoBlog.Api/wwwroot/studio
RUN dotnet publish OcharoBlog.Api/OcharoBlog.Api.csproj -c Release -o /app -p:BuildSpa=false -p:Version=${APP_VERSION}

# ---- Stage 3: runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
# curl serves the deployment's /health probe.
RUN apt-get update && apt-get install --yes --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app ./

# SQLite store on a volume so data survives container replacement.
ENV ConnectionStrings__OcharoBlog="Data Source=/data/ocharo-blog.db" \
    Blog__DataRoot=/data
RUN mkdir -p /data && chown app:app /data
VOLUME ["/data"]

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "OcharoBlog.Api.dll"]
