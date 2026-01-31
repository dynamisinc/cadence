/**
 * Parses CHANGELOG.md and outputs JSON for build-time injection.
 * Run during build: npx tsx scripts/parse-changelog.ts
 */

import { readFileSync, writeFileSync } from 'fs'
import { resolve, dirname } from 'path'
import { fileURLToPath } from 'url'

const __dirname = dirname(fileURLToPath(import.meta.url))

interface ReleaseNote {
  version: string
  date: string
  features: string[]
  fixes: string[]
  breaking?: string[]
}

function parseChangelog(content: string): ReleaseNote[] {
  const releases: ReleaseNote[] = []
  const lines = content.split('\n')

  let currentRelease: ReleaseNote | null = null
  let currentSection: 'features' | 'fixes' | 'breaking' | null = null

  for (const line of lines) {
    // Match version header: ## [1.0.0] - 2026-01-30
    const versionMatch = line.match(/^## \[(\d+\.\d+\.\d+)\]\s*-\s*(\d{4}-\d{2}-\d{2})/)
    if (versionMatch) {
      if (currentRelease) {
        releases.push(currentRelease)
      }
      currentRelease = {
        version: versionMatch[1],
        date: versionMatch[2],
        features: [],
        fixes: [],
      }
      currentSection = null
      continue
    }

    // Match section headers
    if (line.match(/^### Features?/i)) {
      currentSection = 'features'
      continue
    }
    if (line.match(/^### (Bug )?Fixes?/i)) {
      currentSection = 'fixes'
      continue
    }
    if (line.match(/^### Breaking Changes?/i) || line.match(/^### BREAKING CHANGE/i)) {
      currentSection = 'breaking'
      if (currentRelease && !currentRelease.breaking) {
        currentRelease.breaking = []
      }
      continue
    }
    // Skip other sections (Technical, etc.)
    if (line.match(/^### /)) {
      currentSection = null
      continue
    }

    // Match list items: * Item or - Item
    const itemMatch = line.match(/^\s*[*-]\s+(.+)$/)
    if (itemMatch && currentRelease && currentSection) {
      const item = itemMatch[1].trim()
      if (currentSection === 'features') {
        currentRelease.features.push(item)
      } else if (currentSection === 'fixes') {
        currentRelease.fixes.push(item)
      } else if (currentSection === 'breaking') {
        currentRelease.breaking!.push(item)
      }
    }
  }

  // Don't forget the last release
  if (currentRelease) {
    releases.push(currentRelease)
  }

  return releases
}

// Main execution
const changelogPath = resolve(__dirname, '../CHANGELOG.md')
const outputPath = resolve(__dirname, '../src/features/version/data/release-notes.json')

try {
  const changelog = readFileSync(changelogPath, 'utf-8')
  const releases = parseChangelog(changelog)

  // Ensure output directory exists
  const outputDir = dirname(outputPath)
  const { mkdirSync } = await import('fs')
  mkdirSync(outputDir, { recursive: true })

  writeFileSync(outputPath, JSON.stringify(releases, null, 2))
  console.log(`Parsed ${releases.length} releases from CHANGELOG.md`)
  console.log(`   Output: ${outputPath}`)
} catch (error) {
  console.error('Failed to parse CHANGELOG:', error)
  process.exit(1)
}
