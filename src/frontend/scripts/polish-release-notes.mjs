/**
 * Polishes raw release notes (from parse-changelog.ts) into user-facing text
 * using the Claude API.
 *
 * Usage:
 *   ANTHROPIC_API_KEY=sk-... node scripts/polish-release-notes.mjs
 *
 * Reads and overwrites src/features/version/data/release-notes.json in place.
 */

import { readFileSync, writeFileSync } from 'fs'
import { resolve, dirname } from 'path'
import { fileURLToPath } from 'url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const notesPath = resolve(
  __dirname,
  '../src/features/version/data/release-notes.json'
)

const SYSTEM_PROMPT = `You are a technical writer for Cadence, an emergency management exercise platform used by emergency management professionals.

Rewrite these raw release notes (auto-generated from git commit messages) into polished, user-facing release notes.

Rules:
- Write for end users (emergency management professionals), not developers.
- Merge related items into single, clear descriptions when multiple commits address the same feature or fix.
- Remove internal changes that don't affect users (test fixes, CI changes, refactoring, type fixes, build tooling).
- Use plain language — avoid jargon like "component", "hook", "state", "context", "DTO", "interceptor", "middleware".
- Focus on what users can DO or what was fixed FROM THEIR PERSPECTIVE.
- Features should describe capabilities and benefits, not implementation details.
- Fixes should describe what was wrong or what improved, from the user's point of view.
- If a version has no user-facing changes after filtering, keep it with empty arrays.
- Only include the "breaking" array if it has entries; omit it otherwise.
- Do not invent features or fixes that aren't represented in the source data.
- Preserve version numbers and dates exactly as given.
- Return ONLY the JSON array — no markdown fences, no commentary.
- Keep the same JSON structure: [{version, date, features[], fixes[], breaking?[]}]`

async function polish() {
  const apiKey = process.env.ANTHROPIC_API_KEY
  if (!apiKey) {
    console.error(
      'Error: ANTHROPIC_API_KEY environment variable is required.\n' +
        'Set it before running: export ANTHROPIC_API_KEY=sk-...'
    )
    process.exit(1)
  }

  const rawNotes = readFileSync(notesPath, 'utf-8')
  const rawParsed = JSON.parse(rawNotes)
  console.log(`Polishing ${rawParsed.length} releases via Claude API...`)

  const response = await fetch('https://api.anthropic.com/v1/messages', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'x-api-key': apiKey,
      'anthropic-version': '2023-06-01',
    },
    body: JSON.stringify({
      model: 'claude-sonnet-4-6',
      max_tokens: 8192,
      system: SYSTEM_PROMPT,
      messages: [{ role: 'user', content: rawNotes }],
    }),
  })

  if (!response.ok) {
    const body = await response.text()
    throw new Error(`Claude API request failed (${response.status}): ${body}`)
  }

  const result = await response.json()
  const text = result.content[0].text

  // Extract the JSON array (may or may not be wrapped in markdown fences)
  const jsonMatch = text.match(/\[[\s\S]*\]/)
  if (!jsonMatch) {
    console.error('Response text:', text)
    throw new Error('Could not extract JSON array from Claude response')
  }

  const polished = JSON.parse(jsonMatch[0])

  // Sanity check: same number of versions, same version strings
  const rawVersions = rawParsed.map((r) => r.version)
  const polishedVersions = polished.map((r) => r.version)
  if (rawVersions.join(',') !== polishedVersions.join(',')) {
    console.warn(
      'Warning: version mismatch!\n' +
        `  Raw:      ${rawVersions.join(', ')}\n` +
        `  Polished: ${polishedVersions.join(', ')}`
    )
  }

  writeFileSync(notesPath, JSON.stringify(polished, null, 2) + '\n')
  console.log(
    `Done — polished ${polished.length} releases → ${notesPath}`
  )
}

polish().catch((err) => {
  console.error('Failed to polish release notes:', err.message || err)
  process.exit(1)
})
