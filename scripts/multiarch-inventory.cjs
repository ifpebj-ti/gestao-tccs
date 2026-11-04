const { execFileSync } = require('node:child_process');
const crypto = require('node:crypto');

const images = [
  'postgres:15', 'dxflrs/garage:v1.0.1', 'jc21/nginx-proxy-manager:latest',
  'portainer/portainer-ee:latest', 'prom/prometheus:latest',
  'grafana/loki:latest', 'grafana/tempo:latest', 'grafana/grafana:latest',
  'axllent/mailpit:latest', 'node:24-alpine',
  'mcr.microsoft.com/dotnet/sdk:8.0', 'mcr.microsoft.com/dotnet/aspnet:8.0-alpine'
];
let failed = false;
for (const image of images) {
  try {
    const raw = execFileSync('docker', ['buildx', 'imagetools', 'inspect', '--raw', image], { encoding: 'utf8', maxBuffer: 4 * 1024 * 1024 });
    const manifest = JSON.parse(raw);
    const platforms = (manifest.manifests || []).map(item => item.platform)
      .filter(item => item?.os === 'linux').map(item => `${item.os}/${item.architecture}`);
    const supported = ['linux/amd64', 'linux/arm64'].every(item => platforms.includes(item));
    console.log(JSON.stringify({ image, checkedAt: new Date().toISOString(), inspectedJsonSha256: crypto.createHash('sha256').update(raw).digest('hex'), platforms: [...new Set(platforms)], supported }));
    if (!supported) failed = true;
  } catch (error) {
    console.error(`${image}: ${error.message}`);
    failed = true;
  }
}
process.exitCode = failed ? 1 : 0;
