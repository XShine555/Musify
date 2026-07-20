#!/bin/sh
# Shared helpers for the production deploy scripts.

step() { printf '\n==> %s\n' "$1"; }
fail() { printf 'error: %s\n' "$1" >&2; exit 1; }

require_command() {
    command -v "$1" >/dev/null 2>&1 || fail "$1 is required but not installed."
}

# env_get <file> <key> — prints the value, empty if the key is absent.
env_get() {
    grep -E "^$2=" "$1" 2>/dev/null | tail -n 1 | cut -d= -f2-
}

# env_set <file> <key> <value> — replaces the key in place, appends if missing.
env_set() {
    _file=$1; _key=$2; _value=$3
    _tmp=$(mktemp)
    if grep -qE "^$_key=" "$_file"; then
        awk -v k="$_key" -v v="$_value" '
            index($0, k "=") == 1 { print k "=" v; next }
            { print }
        ' "$_file" >"$_tmp"
    else
        cp "$_file" "$_tmp"
        printf '%s=%s\n' "$_key" "$_value" >>"$_tmp"
    fi
    cat "$_tmp" >"$_file"
    rm -f "$_tmp"
}
