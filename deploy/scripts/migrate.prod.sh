#!/bin/sh
# Applies EF Core migrations to the prod Postgres, from a throwaway SDK
# container attached to the compose network (postgres is not published to the host).
set -eu

DEPLOY_DIR="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
BACKEND_DIR="$(CDPATH= cd -- "$DEPLOY_DIR/../backend" && pwd)"
ENV_FILE="$DEPLOY_DIR/.env.prod"

. "$ENV_FILE"

NETWORK="musify-prod_default"
CONN="Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"

docker run --rm \
  --network "$NETWORK" \
  -v "$BACKEND_DIR":/src \
  -w /src/Musify.Infrastructure \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  sh -c "dotnet tool install --global dotnet-ef >/dev/null 2>&1 || true; \
    export PATH=\$PATH:/root/.dotnet/tools; \
    dotnet restore Musify.Infrastructure.csproj; \
    dotnet user-secrets set 'Database:ConnectionString' '$CONN' >/dev/null; \
    dotnet ef database update --context Database"
