---
name: commit
description: Stage changes, generate a commitlint-compliant message, and commit. Adds missing scopes to commitlint.config.js automatically. Use when the user says "commit", "commit changes", or wants to create a conventional commit.
disable-model-invocation: true
allowed-tools: Bash, Read, Edit, Grep, Glob, TodoWrite
---

# Commitlint-Compliant Commit

Create a conventional commit that passes the project's commitlint CI check. Automatically detects the correct scope and type from changed files, and adds new scopes to `commitlint.config.js` when needed.

## Context

- Current branch: !`git branch --show-current`
- Git status: !`git status --short`
- Staged changes: !`git diff --cached --stat`
- Recent commits: !`git log --oneline -5`

## Commitlint Rules Reference

All commit messages MUST match: `type(scope): description`

### Valid Types

`build`, `chore`, `ci`, `docs`, `feat`, `fix`, `infra`, `perf`, `refactor`, `revert`, `style`, `test`

### Valid Scopes

Read the current allowed scopes from `commitlint.config.js` at the repo root before committing. The scopes listed there are the source of truth.

### Scope Detection

Map changed files to scopes using these rules:

| File Path Pattern | Scope |
|---|---|
| `src/frontend/` (general) | `ui` or `frontend` |
| `src/Cadence.Core/Features/{Name}/` | lowercase feature name (e.g., `exercises`, `injects`, `msel`) |
| `src/Cadence.WebApi/Controllers/` | `api` |
| `src/Cadence.WebApi/Hubs/` | `signalr` |
| `src/Cadence.Core/Data/` or migrations | `db` or `migrations` |
| `src/Cadence.Core.Tests/` or `*.test.ts` | `tests` |
| `docs/` | `docs` |
| `.github/workflows/` | `ci` |
| `commitlint.config.js`, `.claude/` | `ci` |
| `package.json`, `*.csproj` (deps only) | `deps` |
| `src/frontend/src/theme/` | `ui` |
| `src/frontend/src/contexts/` | `frontend` |
| `src/Cadence.Core/Features/Organizations/` | `organizations` |
| `src/Cadence.Core/Features/Msel/` | `msel` |
| `src/Cadence.Core/Features/Exercises/` | `exercises` |
| `src/Cadence.Core/Features/Injects/` | `injects` |
| `src/Cadence.Core/Features/Observations/` | `observations` |
| `src/Cadence.Core/Features/ExerciseClock/` | `clock` |
| Infrastructure/seeding files | `seeding` |

If changes span multiple scopes, pick the most prominent one or use a broader scope like `core`, `backend`, or `frontend`.

## Steps

### 1. Analyze Changes

Run in parallel:
- `git status` to see all changed files
- `git diff --cached` to see staged changes (if any)
- `git diff` to see unstaged changes
- `git log --oneline -5` to see recent commit message style
- Read `commitlint.config.js` to get the current allowed scopes

### 2. Stage Files

If no files are staged, stage the relevant changed files. Prefer staging specific files over `git add -A`. Never stage files that contain secrets (`.env`, credentials, etc.).

### 3. Determine Type and Scope

Based on the changes:
1. **Type**: Choose from the valid types based on what the changes do (new feature = `feat`, bug fix = `fix`, etc.)
2. **Scope**: Use the scope detection table above. Check that the scope exists in `commitlint.config.js`.

### 4. Add Missing Scope (if needed)

If the detected scope is NOT in `commitlint.config.js`:

1. Read `commitlint.config.js`
2. Add the new scope in the appropriate section (Core areas, Project areas, Feature modules, or Infrastructure)
3. Stage `commitlint.config.js`
4. This scope addition will be included in the same commit (no separate commit needed) — unless the primary change is unrelated, in which case commit the scope addition first:
   ```
   chore(ci): add {scope} to commitlint config
   ```

### 5. Validate Message

Before committing, validate the message:
```bash
echo "type(scope): description" | npx commitlint
```

If validation fails, adjust the message and re-validate.

### 6. Commit

Create the commit using a HEREDOC for proper formatting:
```bash
git commit -m "$(cat <<'EOF'
type(scope): concise description of changes

Optional longer body explaining the why, not the what.

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
EOF
)"
```

### 7. Verify

Run `git status` to confirm the commit succeeded.

## Message Guidelines

- **Subject line**: Max 72 characters, imperative mood ("add feature" not "added feature")
- **Focus on why**: The diff shows what changed; the message explains why
- **No period** at end of subject line
- **Body** (optional): Wrap at 72 characters, explain motivation and contrast with previous behavior

## Examples

```
feat(msel): add bulk inject import from Excel
fix(ui): resolve date picker timezone offset in exercise form
refactor(exercises): extract validation logic to shared service
test(injects): add unit tests for inject firing workflow
chore(deps): update Material-UI to v7.2
style(frontend): fix lint warnings in exercise components
docs(observations): document observation export format
ci(infra): add staging deployment workflow
```
