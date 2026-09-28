#!/usr/bin/env bash
set -euo pipefail
NAMESPACE="${1:-lab}"

ensure_secret() {
  local name="$1"; shift
  if kubectl -n "$NAMESPACE" get secret "$name" >/dev/null 2>&1; then
    echo "secret/$name exists, keeping it"; return
  fi
  local args=()
  for kv in "$@"; do args+=("--from-literal=$kv"); done
  kubectl -n "$NAMESPACE" create secret generic "$name" "${args[@]}"
}

PG_PASSWORD="$(openssl rand -hex 24)"
API_PASSWORD="$(openssl rand -hex 24)"
CHECKER_PASSWORD="$(openssl rand -hex 24)"

ensure_secret postgres-credentials POSTGRES_USER=lab "POSTGRES_PASSWORD=$PG_PASSWORD" POSTGRES_DB=payments
ensure_secret db-roles "API_PASSWORD=$API_PASSWORD" "CHECKER_PASSWORD=$CHECKER_PASSWORD"
ensure_secret api-db "ConnectionStrings__Payments=Host=postgres;Database=payments;Username=api_app;Password=$API_PASSWORD;Timeout=3"
ensure_secret audit-checker-db "PGPASSWORD=$CHECKER_PASSWORD"
ensure_secret keycloak-admin username=admin "password=$(openssl rand -hex 12)"
