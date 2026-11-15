const { test } = require('node:test');
const assert = require('node:assert/strict');
const { spawnSync } = require('node:child_process');
const path = require('node:path');
const { findings } = require('../security-gate.cjs');

test('Trivy blocks all critical and high findings, including unfixed vulnerabilities', () => {
  const vulnerabilities = [
    { Severity: 'CRITICAL' },
    { Severity: 'CRITICAL', FixedVersion: '2' },
    { Severity: 'HIGH', FixedVersion: '2' },
    { Severity: 'HIGH' },
    { Severity: 'MEDIUM', FixedVersion: '2' }
  ];
  assert.equal(findings('trivy', { SchemaVersion: 2, Results: [{ Vulnerabilities: vulnerabilities }] }).length, 4);
  assert.deepEqual(findings('trivy', { SchemaVersion: 2, Results: [] }), []);
});

test('NuGet includes transitive packages and blocks high or critical severity', () => {
  const report = { version: 1, projects: [{ frameworks: [{
    topLevelPackages: [{ vulnerabilities: [{ severity: 'Moderate' }] }],
    transitivePackages: [{ vulnerabilities: [{ severity: 'High' }, { severity: 'Critical' }] }]
  }] }] };
  assert.equal(findings('nuget', report).length, 2);
  assert.throws(() => findings('nuget', { ...report, problems: [{ level: 'error' }] }));
});

test('ZAP blocks high alerts and preserves lower severity findings for triage', () => {
  assert.equal(findings('zap', { site: [{ alerts: [
    { riskcode: '0' }, { riskcode: '1' }, { riskcode: '2' }, { riskcode: '3' }
  ] }] }).length, 1);
  assert.deepEqual(findings('zap', { site: [{ alerts: [] }] }), []);
});

test('missing reports, empty sites and unsupported formats never pass', () => {
  for (const kind of ['nuget', 'trivy', 'zap', 'unknown']) {
    assert.throws(() => findings(kind, {}));
  }
  assert.throws(() => findings('zap', { site: [] }));
  assert.throws(() => findings('zap', { site: [{}] }));
  assert.throws(() => findings('zap', { site: [{ alerts: [{}] }] }));
  assert.throws(() => findings('nuget', { version: 1, projects: [] }));
  const result = spawnSync(process.execPath, [path.join(__dirname, '../security-gate.cjs'), 'zap', 'missing-report.json']);
  assert.equal(result.status, 1);
});
