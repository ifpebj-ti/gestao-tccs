const fs = require('node:fs');
function findings(kind, report) {
  if (kind === 'trivy') {
    if (!Array.isArray(report.Results) || !report.SchemaVersion) throw new Error('Invalid Trivy report');
    return report.Results.flatMap(r => r.Vulnerabilities || []).filter(v =>
      v.Severity === 'CRITICAL' || v.Severity === 'HIGH');
  }
  if (kind === 'nuget') {
    if (!Array.isArray(report.projects) || !report.projects.length || !report.version || report.problems?.length) throw new Error('Invalid or incomplete NuGet report');
    return report.projects.flatMap(p => p.frameworks || []).flatMap(f =>
      [...(f.topLevelPackages || []), ...(f.transitivePackages || [])]).flatMap(p =>
      (p.vulnerabilities || []).filter(v => /^(high|critical)$/i.test(v.severity)));
  }
  if (kind === 'zap') {
    if (!Array.isArray(report.site) || !report.site.length) throw new Error('Missing ZAP site results');
    if (report.site.some(s => !Array.isArray(s.alerts))) throw new Error('Missing ZAP alerts');
    const alerts = report.site.flatMap(s => s.alerts);
    if (alerts.some(a => !/^[0-3]$/.test(String(a.riskcode)))) throw new Error('Invalid ZAP severity');
    return alerts.filter(a => Number(a.riskcode) >= 3);
  }
  throw new Error('Unknown report kind');
}
if (require.main === module) {
  try {
    const [kind, file] = process.argv.slice(2);
    const blocked = findings(kind, JSON.parse(fs.readFileSync(file, 'utf8')));
    console.log(`${kind}: ${blocked.length} blocking findings; review the retained report for all severities.`);
    if (blocked.length) process.exitCode = 1;
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}
module.exports = { findings };
