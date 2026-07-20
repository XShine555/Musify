#!/bin/sh
# Generates the RS256 key pair used to sign (API) and verify (Gateway) stream
# tickets. No-op if both keys already exist.
set -eu

KEYS_DIR=${1:?usage: stream-keys.sh <keys-dir>}
PRIVATE_KEY="$KEYS_DIR/stream_private.pem"
PUBLIC_KEY="$KEYS_DIR/stream_public.pem"

if [ -f "$PRIVATE_KEY" ] && [ -f "$PUBLIC_KEY" ]; then
    echo "Stream-ticket keys already present in $KEYS_DIR"
    exit 0
fi

mkdir -p "$KEYS_DIR"
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out "$PRIVATE_KEY" 2>/dev/null
openssl rsa -in "$PRIVATE_KEY" -pubout -out "$PUBLIC_KEY" 2>/dev/null
chmod 644 "$PUBLIC_KEY"
chmod 600 "$PRIVATE_KEY"

echo "Generated stream-ticket keys in $KEYS_DIR"
