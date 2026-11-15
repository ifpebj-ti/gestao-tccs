#!/usr/bin/env bash
set -euo pipefail
# Official standalone release; the pinned hash also rejects replaced release assets.
version=1.33.1
checksum=46e8133cc490f9f50518061590ce2f6be5a08afaeda034471c0ceda4b3899808
directory=$(mktemp -d)
trap 'rm -f "$directory/opengrep"; rmdir "$directory"' EXIT
curl --fail --show-error --silent --location --retry 3 --connect-timeout 15 --max-time 180 \
  "https://github.com/opengrep/opengrep/releases/download/v$version/opengrep_manylinux_x86" \
  -o "$directory/opengrep"
printf '%s  %s\n' "$checksum" "$directory/opengrep" | sha256sum --check
sudo install -m 0755 "$directory/opengrep" /usr/local/bin/opengrep
opengrep --version
