#!/usr/bin/env bash
set -euo pipefail
install_archive() {
  local repo=$1 version=$2 archive=$3 checksums=$4 binary=$5
  local directory
  directory=$(mktemp -d)
  for file in "$archive" "$checksums"; do
    curl --fail --show-error --silent --location --retry 3 --connect-timeout 15 --max-time 180 \
      "https://github.com/$repo/releases/download/v$version/$file" -o "$directory/$file"
  done
  (cd "$directory"; grep -F " $archive" "$checksums" > selected-checksum.txt
    test "$(wc -l < selected-checksum.txt)" -eq 1
    sha256sum --check selected-checksum.txt
    tar -xzf "$archive" "$binary")
  sudo install -m 0755 "$directory/$binary" "/usr/local/bin/$binary"
  "$binary" version
}
install_archive aquasecurity/trivy 0.76.1 trivy_0.76.1_Linux-64bit.tar.gz trivy_0.76.1_checksums.txt trivy
install_archive gitleaks/gitleaks 8.30.1 gitleaks_8.30.1_linux_x64.tar.gz gitleaks_8.30.1_checksums.txt gitleaks
