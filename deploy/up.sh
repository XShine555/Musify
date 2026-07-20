#!/bin/sh
# Brings up (or updates) the Musify production stack on the server.
#
#   ./up.sh              build + start everything, provision Zitadel, migrate
#   ./up.sh --no-build   same, reusing the images that are already built
#   ./up.sh --down       stop and remove the containers (volumes are kept)
#
# Everything it needs lives in deploy/.env.prod (see .env.prod.example) and in
# deploy/nginx/certs/ (origin.pem + origin.key).
set -eu

DEPLOY_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
ENV_FILE="$DEPLOY_DIR/.env.prod"
INFRA="postgres zitadel rabbitmq seaweedfs seaweedfs-init jaeger"

. "$DEPLOY_DIR/scripts/lib.sh"

require_command docker
require_command openssl

compose() {
    docker compose --env-file "$ENV_FILE" \
        -f "$DEPLOY_DIR/compose.yml" -f "$DEPLOY_DIR/compose.prod.yml" \
        --profile apps "$@"
}

BUILD=--build
for arg in "$@"; do
    case "$arg" in
        --no-build) BUILD= ;;
        --down) step "Stopping the stack (volumes are kept)"; compose down --remove-orphans; exit 0 ;;
        *) fail "unknown option: $arg" ;;
    esac
done

# Preflight ------------------------------------------------------------------
if [ ! -f "$ENV_FILE" ]; then
    cp "$DEPLOY_DIR/.env.prod.example" "$ENV_FILE"
    chmod 600 "$ENV_FILE"
    fail "created $ENV_FILE from the template — fill it in (domain + secrets) and run again."
fi

if grep -q 'CHANGE_ME\|example\.com' "$ENV_FILE"; then
    fail "$ENV_FILE still contains CHANGE_ME or example.com placeholders."
fi

for cert in origin.pem origin.key; do
    [ -f "$DEPLOY_DIR/nginx/certs/$cert" ] \
        || fail "missing nginx/certs/$cert (Cloudflare Origin Certificate)."
done

mkdir -p "$DEPLOY_DIR/secrets" "$DEPLOY_DIR/keys" "$DEPLOY_DIR/zitadel/.output"
case "$(env_get "$ENV_FILE" YTDLP_ADDITIONAL_ARGUMENTS)" in
    *--cookies*)
        [ -f "$DEPLOY_DIR/secrets/youtube_cookies.txt" ] \
            || echo "warning: YTDLP_ADDITIONAL_ARGUMENTS expects secrets/youtube_cookies.txt, which is missing." ;;
esac

step "Ensuring stream-ticket keys"
sh "$DEPLOY_DIR/scripts/stream-keys.sh" "$DEPLOY_DIR/keys"

# Infrastructure -------------------------------------------------------------
step "Starting the infrastructure"
# shellcheck disable=SC2086
compose up -d $INFRA

step "Waiting for Postgres"
deadline=$(( $(date +%s) + 120 ))
until [ "$(docker inspect --format '{{.State.Health.Status}}' musify-postgres 2>/dev/null)" = healthy ]; do
    [ "$(date +%s)" -lt "$deadline" ] || fail "Postgres did not become healthy in time."
    sleep 2
done

# nginx has to be up before provisioning: it terminates TLS for the Zitadel
# domain. --no-deps keeps the apps from starting before they have a client id.
step "Starting the edge"
compose up -d --no-deps nginx

# Zitadel + database ---------------------------------------------------------
sh "$DEPLOY_DIR/zitadel/provision.sh"

step "Applying EF Core migrations"
sh "$DEPLOY_DIR/scripts/migrate.sh"

# Applications ---------------------------------------------------------------
step "Starting the applications"
# shellcheck disable=SC2086
compose up -d $BUILD

step "Done"
compose ps
cat <<EOF

  Web player   $(env_get "$ENV_FILE" PUBLIC_WEB_URL)
  API          $(env_get "$ENV_FILE" PUBLIC_API_URL)/scalar/v1
  Streaming    $(env_get "$ENV_FILE" PUBLIC_STREAM_URL)/health
  Zitadel      $(env_get "$ENV_FILE" PUBLIC_AUTH_URL)/ui/console
               $(env_get "$ENV_FILE" ZITADEL_ADMIN_USERNAME)@zitadel.$(env_get "$ENV_FILE" ZITADEL_EXTERNAL_DOMAIN)

  Jaeger collects traces but publishes no port. To look at them, forward its
  container address over SSH from your machine:
    ssh -L 16686:\$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' musify-jaeger):16686 <server>
EOF
