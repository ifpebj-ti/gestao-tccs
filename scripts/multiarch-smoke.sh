#!/usr/bin/env bash
set -euo pipefail
service=${1:?frontend or backend}
platform=${2:?linux/amd64 or linux/arm64}
image=${3:?local image tag}
case "$service" in frontend|backend) ;; *) exit 2 ;; esac
case "$platform" in linux/amd64) arch=amd64; machine=x86_64 ;; linux/arm64) arch=arm64; machine=aarch64 ;; *) exit 2 ;; esac
test "$(docker image inspect --format '{{.Architecture}}' "$image")" = "$arch"
test "$(docker run --rm --platform "$platform" --entrypoint uname "$image" -m)" = "$machine"
name="gradus-multiarch-${service}-${arch}-$$"
# shellcheck disable=SC2329 # Invoked by the EXIT trap below.
cleanup() {
  docker logs --tail 80 "$name" 2>/dev/null || true
  # Only these uniquely named test containers and their anonymous test volumes.
  docker rm -fv "$name" "${name}-db" >/dev/null 2>&1 || true
  docker network rm "$name" >/dev/null 2>&1 || true
}
trap cleanup EXIT
docker network create "$name" >/dev/null
if [ "$service" = frontend ]; then
  docker run --rm --platform "$platform" --entrypoint node "$image" -e '
    require("@next/swc-linux-" + (process.arch === "arm64" ? "arm64" : "x64") + "-musl");
    require("lightningcss").transform({ filename: "smoke.css", code: Buffer.from("a { color: red; }") });
    require("sharp")({ create: { width: 1, height: 1, channels: 3, background: "red" } }).png().toBuffer().then(() => console.log(process.arch + ": native addons OK")).catch(e => { console.error(e); process.exit(1); });'
  docker run -d --platform "$platform" --network "$name" --name "$name" "$image" >/dev/null
  for ((attempt=1; attempt<=60; attempt++)); do
    if docker exec "$name" node -e 'fetch("http://127.0.0.1:3000").then(r => { if (r.status !== 200) process.exit(1); }).catch(() => process.exit(1))'; then
      echo "PASS: frontend $platform native addons and HTTP 200"
      exit 0
    fi
    sleep 2
  done
else
  docker run --rm --platform "$platform" --entrypoint dotnet "$image" --info
  docker run -d --platform "$platform" --network "$name" --name "${name}-db" \
    -e POSTGRES_PASSWORD=validation-only-password -e POSTGRES_DB=gradus_multiarch postgres:15 >/dev/null
  ready=0
  for ((attempt=1; attempt<=60; attempt++)); do
    if docker exec "${name}-db" pg_isready -h 127.0.0.1 -U postgres -d gradus_multiarch; then
      ready=1
      break
    fi
    sleep 2
  done
  test "$ready" = 1
  docker run -d --platform "$platform" --network "$name" --name "$name" \
    -e ASPNETCORE_ENVIRONMENT=Production \
    -e "ConnectionStrings__DefaultConnection=Host=${name}-db;Database=gradus_multiarch;Username=postgres;Password=validation-only-password" \
    -e JWT_SETTINGS__TOKEN_KEY=validation-only-key-at-least-thirty-two-characters \
    -e JWT_SETTINGS__ACCESSTOKEN_EXPIRATION_TIME=30 \
    -e STORAGE_SETTINGS__ENDPOINT=http://127.0.0.1:3900 \
    -e STORAGE_SETTINGS__ACCESS_KEY=validation-only -e STORAGE_SETTINGS__SECRET_KEY=validation-only \
    -e STORAGE_SETTINGS__BUCKET_NAME=validation-only \
    -e URL_LOKI=http://127.0.0.1:3100 -e URL_TEMPO=http://127.0.0.1:4318 "$image" >/dev/null
  for ((attempt=1; attempt<=90; attempt++)); do
    if docker exec "$name" wget -q -O /dev/null http://127.0.0.1:8080/metrics; then
      echo "PASS: backend $platform migrations, seed, Hangfire and HTTP metrics"
      exit 0
    fi
    sleep 2
  done
fi
echo "HTTP smoke failed: $service on $platform" >&2
exit 1
