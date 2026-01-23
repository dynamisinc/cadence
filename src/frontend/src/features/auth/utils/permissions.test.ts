/**
 * Permission Utilities Tests
 *
 * @module features/auth
 */
import { describe, it, expect } from 'vitest'
import {
  hasPermission,
  getRoleDisplayName,
  getRoleDescription,
  getRoleColor,
} from './permissions'
import type { ExerciseRole } from '../constants/rolePermissions'

describe('hasPermission', () => {
  it('grants view_exercise to all roles', () => {
    expect(hasPermission('Observer', 'view_exercise')).toBe(true)
    expect(hasPermission('Evaluator', 'view_exercise')).toBe(true)
    expect(hasPermission('Controller', 'view_exercise')).toBe(true)
    expect(hasPermission('ExerciseDirector', 'view_exercise')).toBe(true)
    expect(hasPermission('Administrator', 'view_exercise')).toBe(true)
  })

  it('grants add_observation only to Evaluator and above', () => {
    expect(hasPermission('Observer', 'add_observation')).toBe(false)
    expect(hasPermission('Evaluator', 'add_observation')).toBe(true)
    expect(hasPermission('Controller', 'add_observation')).toBe(true)
    expect(hasPermission('ExerciseDirector', 'add_observation')).toBe(true)
    expect(hasPermission('Administrator', 'add_observation')).toBe(true)
  })

  it('grants fire_inject only to Controller and above', () => {
    expect(hasPermission('Observer', 'fire_inject')).toBe(false)
    expect(hasPermission('Evaluator', 'fire_inject')).toBe(false)
    expect(hasPermission('Controller', 'fire_inject')).toBe(true)
    expect(hasPermission('ExerciseDirector', 'fire_inject')).toBe(true)
    expect(hasPermission('Administrator', 'fire_inject')).toBe(true)
  })

  it('grants manage_participants only to Director and above', () => {
    expect(hasPermission('Observer', 'manage_participants')).toBe(false)
    expect(hasPermission('Evaluator', 'manage_participants')).toBe(false)
    expect(hasPermission('Controller', 'manage_participants')).toBe(false)
    expect(hasPermission('ExerciseDirector', 'manage_participants')).toBe(true)
    expect(hasPermission('Administrator', 'manage_participants')).toBe(true)
  })

  it('grants delete_exercise only to Administrator', () => {
    expect(hasPermission('Observer', 'delete_exercise')).toBe(false)
    expect(hasPermission('Evaluator', 'delete_exercise')).toBe(false)
    expect(hasPermission('Controller', 'delete_exercise')).toBe(false)
    expect(hasPermission('ExerciseDirector', 'delete_exercise')).toBe(false)
    expect(hasPermission('Administrator', 'delete_exercise')).toBe(true)
  })
})

describe('getRoleDisplayName', () => {
  it('returns user-friendly names for all roles', () => {
    expect(getRoleDisplayName('Administrator')).toBe('Administrator')
    expect(getRoleDisplayName('ExerciseDirector')).toBe('Exercise Director')
    expect(getRoleDisplayName('Controller')).toBe('Controller')
    expect(getRoleDisplayName('Evaluator')).toBe('Evaluator')
    expect(getRoleDisplayName('Observer')).toBe('Observer')
  })

  it('returns Unknown for invalid roles', () => {
    expect(getRoleDisplayName('InvalidRole' as unknown as ExerciseRole)).toBe('Unknown')
  })
})

describe('getRoleDescription', () => {
  it('returns description for Observer', () => {
    const desc = getRoleDescription('Observer')
    expect(desc).toContain('watch')
    expect(desc.toLowerCase()).toContain('observe')
  })

  it('returns description for Evaluator', () => {
    const desc = getRoleDescription('Evaluator')
    expect(desc.toLowerCase()).toContain('observation')
  })

  it('returns description for Controller', () => {
    const desc = getRoleDescription('Controller')
    expect(desc.toLowerCase()).toContain('inject')
  })

  it('returns description for ExerciseDirector', () => {
    const desc = getRoleDescription('ExerciseDirector')
    expect(desc.toLowerCase()).toContain('exercise')
  })

  it('returns description for Administrator', () => {
    const desc = getRoleDescription('Administrator')
    expect(desc.toLowerCase()).toContain('system')
  })
})

describe('getRoleColor', () => {
  it('returns distinct colors for each role', () => {
    expect(getRoleColor('Administrator')).toBe('error')
    expect(getRoleColor('ExerciseDirector')).toBe('error')
    expect(getRoleColor('Controller')).toBe('primary')
    expect(getRoleColor('Evaluator')).toBe('success')
    expect(getRoleColor('Observer')).toBe('default')
  })

  it('returns default color for invalid roles', () => {
    expect(getRoleColor('InvalidRole' as unknown as ExerciseRole)).toBe('default')
  })
})
