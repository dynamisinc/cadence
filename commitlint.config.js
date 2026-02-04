module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'scope-enum': [
      2,
      'always',
      [
        // Core areas
        'api',
        'ui',
        'offline',
        'auth',
        'docs',
        'ci',
        'deps',
        // Project areas
        'frontend',
        'backend',
        'tests',
        'seeding',
        // Feature modules
        'exercises',
        'injects',
        'msel',
        'observations',
        'organizations',
        'clock',
        'conduct',
        'metrics',
        'settings',
        'reports',
        'version',
        'capabilities',
        'approval',
        'inject-approval',
        'feature-flags',
        // Infrastructure
        'migrations',
        'db',
        'signalr',
        'pwa',
        'telemetry',
        'database',
      ],
    ],
    'scope-empty': [1, 'never'],
  },
};
