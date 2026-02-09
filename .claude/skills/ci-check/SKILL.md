---
name: ci-check
description: Run full CI validation locally, fix issues, commit fixes, and push. Use when the user says "ci check", "run ci", "validate ci", "lint and test", or wants to prepare a branch for PR.
disable-model-invocation: true
allowed-tools: Bash, Read, Write, Edit, Grep, Glob, TodoWrite
---

# CI Check

Run the full CI validation pipeline locally, fix any issues found, commit fixes with commitlint-compliant messages, and push when everything passes. This mirrors the GitHub Actions CI pipeline with strict settings.

## Context

- Current branch: !`git branch --show-current`
- Git status: !`git status --short`
- Recent commits on branch: !`git log --oneline main..HEAD 2>/dev/null || git log --oneline -10`

## Commitlint Rules

All commit messages MUST follow conventional commits: `type(scope): description`

- Valid types: `feat`, `fix`, `refactor`, `style`, `test`, `chore`, `docs`, `ci`, `build`, `perf`
- Scope MUST be from the allowed list in `commitlint.config.js` at the repo root
- If a new scope is needed, add it to `commitlint.config.js` first and commit: `chore(ci): add <scope> to commitlint config`
- Before committing, validate: `echo "type(scope): message" | npx commitlint`
- Use `Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>` in all commits

## Pipeline Steps

Execute these steps **in order**. If a step fails, fix the issue, commit the fix, then re-run that step before moving on.

### Step 1: Frontend Lint

```bash
cd src/frontend && npm run lint
```

If errors exist:
1. Run `cd src/frontend && npm run lint:fix` to auto-fix
2. Manually fix any remaining errors
3. Commit: `style(ui): fix lint errors`
4. Re-run lint to confirm clean

### Step 2: Frontend Type Check

```bash
cd src/frontend && npm run type-check
```

If errors exist:
1. Fix all TypeScript type errors
2. Commit: `fix(ui): resolve type errors`
3. Re-run to confirm clean

### Step 3: Backend Build (Release mode)

```bash
dotnet build src/Cadence.WebApi/Cadence.WebApi.csproj --configuration Release
```

If errors exist:
1. Fix build errors
2. Commit: `fix(backend): resolve build errors`
3. Re-run to confirm clean

### Step 4: Backend Tests (Release mode)

```bash
dotnet test src/Cadence.Core.Tests/Cadence.Core.Tests.csproj --configuration Release --verbosity normal
dotnet test src/Cadence.WebApi.Tests/Cadence.WebApi.Tests.csproj --configuration Release --verbosity normal
```

If tests fail:
1. Fix failing tests or the code under test
2. Commit: `fix(tests): resolve failing backend tests`
3. Re-run both to confirm all pass

### Step 5: Frontend Tests (CI mode)

```bash
cd src/frontend && npm run test:ci
```

This runs `vitest run --bail=10` matching CI behavior.

If tests fail:
1. Fix failing tests or the code under test
2. Commit: `fix(tests): resolve failing frontend tests`
3. Re-run to confirm all pass

### Step 6: Frontend Build

On Windows:
```bash
cd src/frontend && set "VITE_API_URL=" && npm run build
```

On Linux/Mac:
```bash
cd src/frontend && VITE_API_URL="" npm run build
```

If build fails:
1. Fix build errors
2. Commit: `fix(ui): resolve frontend build errors`
3. Re-run to confirm clean

### Step 7: Commitlint Validation

```bash
npx commitlint --from main --to HEAD
```

If messages fail validation:
1. Check if scopes need to be added to `commitlint.config.js`
2. Add missing scopes and commit: `chore(ci): add <scope> to commitlint config`
3. For messages that cannot be fixed, flag to the user

### Step 8: Push

```bash
git push
```

If push fails due to remote changes:
1. `git pull --rebase`
2. Re-run Steps 1-7
3. Push again

## Summary

After completion, provide a summary:
- Steps that passed on first try
- Issues found and fixed (with commit hashes)
- Warnings or items needing manual attention
- Final status: PASS or FAIL
