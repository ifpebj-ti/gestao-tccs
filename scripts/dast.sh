#!/usr/bin/env bash
set -euo pipefail
# Preserve container paths when invoked through Git Bash on Windows.
workspace=$PWD
case "${OSTYPE:-}" in
  msys*|cygwin*)
    workspace=$(pwd -W)
    export MSYS_NO_PATHCONV=1
    export MSYS2_ARG_CONV_EXCL='*'
    ;;
esac
prefix="gradus-dast-${GITHUB_RUN_ID:-local}-${GITHUB_RUN_ATTEMPT:-1}"
mkdir -p reports/dast
chmod 777 reports/dast
# shellcheck disable=SC2317,SC2329 # Invoked indirectly by the EXIT trap, including on failure.
cleanup() {
  docker logs "$prefix-api" > reports/dast/api.log 2>&1 || true
  docker logs "$prefix-web" > reports/dast/frontend.log 2>&1 || true
  docker rm -fv "$prefix-api" "$prefix-web" "$prefix-db" >/dev/null 2>&1 || true
  docker network rm "$prefix" >/dev/null 2>&1 || true
}
trap cleanup EXIT
docker build --build-arg TARGETARCH=amd64 -f backend/Dockerfile.multiarch -t "$prefix-api" backend
docker build --build-arg TARGETARCH=amd64 -f frontend/Dockerfile.multiarch -t "$prefix-web" frontend
docker network create "$prefix" >/dev/null
docker run -d --network "$prefix" --name "$prefix-db" \
  -e POSTGRES_PASSWORD=validation-only-password -e POSTGRES_DB=gradus_dast postgres:15 >/dev/null
ready=0
for ((attempt=1; attempt<=60; attempt++)); do
  if docker exec "$prefix-db" pg_isready -h 127.0.0.1 -U postgres -d gradus_dast; then ready=1; break; fi
  sleep 2
done
test "$ready" = 1
docker run -d --network "$prefix" --name "$prefix-api" \
  -e ASPNETCORE_ENVIRONMENT=Development -e ASPNETCORE_URLS=http://+:8080 \
  -e "ConnectionStrings__DefaultConnection=Host=$prefix-db;Database=gradus_dast;Username=postgres;Password=validation-only-password" \
  -e JWT_SETTINGS__TOKEN_KEY=validation-only-key-at-least-thirty-two-characters \
  -e JWT_SETTINGS__ACCESSTOKEN_EXPIRATION_TIME=30 \
  -e STORAGE_SETTINGS__ENDPOINT=http://127.0.0.1:3900 \
  -e STORAGE_SETTINGS__ACCESS_KEY=validation-only -e STORAGE_SETTINGS__SECRET_KEY=validation-only \
  -e STORAGE_SETTINGS__BUCKET_NAME=validation-only \
  -e URL_LOKI=http://127.0.0.1:3100 -e URL_TEMPO=http://127.0.0.1:4318 "$prefix-api" >/dev/null
docker run -d --network "$prefix" --name "$prefix-web" "$prefix-web" >/dev/null
ready=0
for ((attempt=1; attempt<=90; attempt++)); do
  if docker exec "$prefix-api" wget -q -O /dev/null http://127.0.0.1:8080/swagger/v1/swagger.json && \
     docker exec "$prefix-web" node -e 'fetch("http://127.0.0.1:3000").then(r=>{if(r.status!==200)process.exit(1)}).catch(()=>process.exit(1))'; then
    ready=1; break
  fi
  sleep 2
done
test "$ready" = 1
# Scan both targets even when one fails. Exit 2 means WARN; JSON severity decides the gate.
status=0
for service in web api; do
  # A report from an earlier local run must never satisfy this scan's gate.
  rm -f "reports/dast/$service.json" "reports/dast/$service.html"
  set +e
  if [ "$service" = web ]; then
    docker run --rm --network "$prefix" -v "$workspace/reports/dast:/zap/wrk:rw" \
      ghcr.io/zaproxy/zaproxy:2.17.0 zap-baseline.py \
      -t "http://$prefix-web:3000" -m 1 -T 5 -J web.json -r web.html
  else
    docker run --rm --network "$prefix" -v "$workspace/reports/dast:/zap/wrk:rw" \
      ghcr.io/zaproxy/zaproxy:2.17.0 zap-api-scan.py \
      -t "http://$prefix-api:8080/swagger/v1/swagger.json" -f openapi -T 5 -J api.json -r api.html
  fi
  scan_status=$?
  set -e
  if [ "$scan_status" -gt 2 ]; then status=1; fi
  node scripts/security-gate.cjs zap "reports/dast/$service.json" || status=1
done
exit "$status"
