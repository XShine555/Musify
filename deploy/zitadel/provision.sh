#!/bin/sh
# Provisions Zitadel for Musify in production (idempotent).
#
# Creates the Musify project, the API OIDC app (public, PKCE, JWT access token)
# and the web player OIDC app (confidential), then writes AUTH_CLIENT_ID,
# WEB_CLIENT_ID and WEB_CLIENT_SECRET into deploy/.env.prod. Authenticates with
# the service-account PAT that Zitadel writes on init. Re-running reuses the
# existing project and apps.
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
DEPLOY_DIR=$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd)
ENV_FILE="$DEPLOY_DIR/.env.prod"
PAT_FILE="$SCRIPT_DIR/.output/admin-sa.pat"

. "$DEPLOY_DIR/scripts/lib.sh"

require_command curl
require_command jq

PROJECT_NAME=${PROJECT_NAME:-Musify}
API_APP_NAME=${API_APP_NAME:-Musify API}
WEB_APP_NAME=${WEB_APP_NAME:-Musify Web}

ISSUER=$(env_get "$ENV_FILE" PUBLIC_AUTH_URL)
API_URL=$(env_get "$ENV_FILE" PUBLIC_API_URL)
WEB_URL=$(env_get "$ENV_FILE" PUBLIC_WEB_URL)
MGMT="$ISSUER/management/v1"

step "Provisioning Zitadel at $ISSUER"

# Zitadel writes the PAT during init; on a cold start it may take a moment.
deadline=$(( $(date +%s) + 180 ))
while [ ! -f "$PAT_FILE" ] && [ "$(date +%s)" -lt "$deadline" ]; do sleep 3; done
[ -f "$PAT_FILE" ] || fail "PAT not found ($PAT_FILE). Check 'docker logs musify-zitadel'."
PAT=$(tr -d '\r\n' <"$PAT_FILE")

api_call() {
    # api_call <method> <path> [json body]
    if [ $# -ge 3 ]; then
        curl -fsS -X "$1" "$MGMT$2" \
            -H "Authorization: Bearer $PAT" -H "Content-Type: application/json" -d "$3"
    else
        curl -fsS -X "$1" "$MGMT$2" -H "Authorization: Bearer $PAT"
    fi
}

deadline=$(( $(date +%s) + 120 ))
until curl -fsS "$ISSUER/.well-known/openid-configuration" >/dev/null 2>&1; do
    [ "$(date +%s)" -lt "$deadline" ] || fail "Zitadel did not respond at $ISSUER."
    sleep 3
done

# The images tagged `latest` require the standalone Login UI v2 container, which
# this stack does not run, so OIDC would redirect to /ui/v2/login and 404.
curl -fsS -X PUT "$ISSUER/v2/features/instance" \
    -H "Authorization: Bearer $PAT" -H "Content-Type: application/json" \
    -d '{"loginV2":{"required":false}}' >/dev/null 2>&1 \
    || echo "  warning: could not disable the required Login UI v2"

name_query() {
    printf '{"queries":[{"nameQuery":{"name":"%s","method":"TEXT_QUERY_METHOD_EQUALS"}}]}' "$1"
}

# Project --------------------------------------------------------------------
PROJECT_ID=$(api_call POST "/projects/_search" "$(name_query "$PROJECT_NAME")" \
    | jq -r --arg n "$PROJECT_NAME" '.result // [] | map(select(.name == $n)) | first | .id // empty')

if [ -n "$PROJECT_ID" ]; then
    echo "  project '$PROJECT_NAME' reused (id $PROJECT_ID)"
else
    PROJECT_ID=$(api_call POST "/projects" "$(printf '{"name":"%s"}' "$PROJECT_NAME")" | jq -r '.id')
    echo "  project '$PROJECT_NAME' created (id $PROJECT_ID)"
fi

find_app() {
    api_call POST "/projects/$PROJECT_ID/apps/_search" "$(name_query "$1")" \
        | jq -r --arg n "$1" '.result // [] | map(select(.name == $n)) | first | .id // empty'
}

# API app: public client used by the Scalar UI to obtain a token -------------
API_APP_ID=$(find_app "$API_APP_NAME")
if [ -n "$API_APP_ID" ]; then
    API_CLIENT_ID=$(api_call GET "/projects/$PROJECT_ID/apps/$API_APP_ID" | jq -r '.app.oidcConfig.clientId')
    echo "  app '$API_APP_NAME' reused (clientId $API_CLIENT_ID)"
else
    API_CLIENT_ID=$(api_call POST "/projects/$PROJECT_ID/apps/oidc" "$(jq -n \
        --arg name "$API_APP_NAME" --arg api "$API_URL" '{
            name: $name,
            redirectUris: [($api + "/scalar/"), ($api + "/scalar/oauth2-redirect.html")],
            postLogoutRedirectUris: [($api + "/scalar/")],
            responseTypes: ["OIDC_RESPONSE_TYPE_CODE"],
            grantTypes: ["OIDC_GRANT_TYPE_AUTHORIZATION_CODE"],
            appType: "OIDC_APP_TYPE_USER_AGENT",
            authMethodType: "OIDC_AUTH_METHOD_TYPE_NONE",
            version: "OIDC_VERSION_1_0",
            devMode: false,
            accessTokenType: "OIDC_TOKEN_TYPE_JWT",
            accessTokenRoleAssertion: true,
            idTokenRoleAssertion: true,
            idTokenUserinfoAssertion: true
        }')" | jq -r '.clientId')
    echo "  app '$API_APP_NAME' created (clientId $API_CLIENT_ID)"
fi

# Web app: confidential client used by the SvelteKit server ------------------
WEB_APP_ID=$(find_app "$WEB_APP_NAME")
WEB_CLIENT_SECRET=$(env_get "$ENV_FILE" WEB_CLIENT_SECRET)
if [ -n "$WEB_APP_ID" ]; then
    WEB_CLIENT_ID=$(api_call GET "/projects/$PROJECT_ID/apps/$WEB_APP_ID" | jq -r '.app.oidcConfig.clientId')
    if [ -z "$WEB_CLIENT_SECRET" ]; then
        # The secret is only returned once, so regenerate it if we no longer have it.
        WEB_CLIENT_SECRET=$(api_call POST "/projects/$PROJECT_ID/apps/$WEB_APP_ID/oidc_config/_generate_client_secret" '{}' \
            | jq -r '.clientSecret')
        echo "  app '$WEB_APP_NAME' reused (clientId $WEB_CLIENT_ID, secret regenerated)"
    else
        echo "  app '$WEB_APP_NAME' reused (clientId $WEB_CLIENT_ID)"
    fi
else
    CREATED=$(api_call POST "/projects/$PROJECT_ID/apps/oidc" "$(jq -n \
        --arg name "$WEB_APP_NAME" --arg web "$WEB_URL" '{
            name: $name,
            redirectUris: [($web + "/auth/callback")],
            postLogoutRedirectUris: [($web + "/")],
            responseTypes: ["OIDC_RESPONSE_TYPE_CODE"],
            grantTypes: ["OIDC_GRANT_TYPE_AUTHORIZATION_CODE", "OIDC_GRANT_TYPE_REFRESH_TOKEN"],
            appType: "OIDC_APP_TYPE_WEB",
            authMethodType: "OIDC_AUTH_METHOD_TYPE_BASIC",
            version: "OIDC_VERSION_1_0",
            devMode: false,
            accessTokenType: "OIDC_TOKEN_TYPE_JWT",
            accessTokenRoleAssertion: true,
            idTokenRoleAssertion: true,
            idTokenUserinfoAssertion: true
        }')")
    WEB_CLIENT_ID=$(echo "$CREATED" | jq -r '.clientId')
    WEB_CLIENT_SECRET=$(echo "$CREATED" | jq -r '.clientSecret')
    echo "  app '$WEB_APP_NAME' created (clientId $WEB_CLIENT_ID)"
fi

env_set "$ENV_FILE" AUTH_CLIENT_ID "$API_CLIENT_ID"
env_set "$ENV_FILE" WEB_CLIENT_ID "$WEB_CLIENT_ID"
env_set "$ENV_FILE" WEB_CLIENT_SECRET "$WEB_CLIENT_SECRET"

echo "  wired into $(basename "$ENV_FILE")"
