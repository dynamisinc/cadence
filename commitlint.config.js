module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'scope-enum': [2, 'always', ['api', 'ui', 'offline', 'auth', 'docs', 'ci', 'deps']],
    'scope-empty': [1, 'never'],
  },
};
