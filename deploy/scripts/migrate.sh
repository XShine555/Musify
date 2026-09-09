#!/bin/sh
# Applies the EF Core migrations to the production database from a throwaway SDK
# container attached to the compose network (Postgres is not published to the host).
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
DEPLOY_DIR=$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd)
BACKEND_DIR=$(CDPATH= cd -- "$DEPLOY_DIR/../backend" && pwd)
ENV_FILE="$DEPLOY_DIR/.env.prod"
NETWORK=${MUSIFY_NETWORK:-musify-prod_default}

. "$SCRIPT_DIR/lib.sh"

CONN="Host=postgres;Port=5432;Database=$(env_get "$ENV_FILE" POSTGRES_DB);Username=$(env_get "$ENV_FILE" POSTGRES_USER);Password=$(env_get "$ENV_FILE" POSTGRES_PASSWORD)"

# Musify.Infrastructure is both the migrations and the startup project: it owns
# the IDesignTimeDbContextFactory and the EFCore.Design package.
docker run --rm \
    --network "$NETWORK" \
    -v "$BACKEND_DIR":/src \
    -w /src/Core/Musify.Infrastructure \
    -e CONN="$CONN" \
    mcr.microsoft.com/dotnet/sdk:10.0 \
    sh -c 'dotnet tool install --global dotnet-ef >/dev/null 2>&1 || true
           export PATH=$PATH:/root/.dotnet/tools
           dotnet restore Musify.Infrastructure.csproj
           dotnet user-secrets set "Database:ConnectionString" "$CONN" >/dev/null
           dotnet ef database update --context Database'
