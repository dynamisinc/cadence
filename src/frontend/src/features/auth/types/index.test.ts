/**
 * Authentication Types Tests
 */
import { describe, it, expect } from 'vitest';
import { validatePassword, isPasswordValid } from './index';

describe('validatePassword', () => {
  it('validates empty password as all requirements unmet', () => {
    const result = validatePassword('');

    expect(result.minLength).toBe(false);
    expect(result.hasUppercase).toBe(false);
    expect(result.hasNumber).toBe(false);
  });

  it('validates password with only lowercase as missing requirements', () => {
    const result = validatePassword('password');

    expect(result.minLength).toBe(true);
    expect(result.hasUppercase).toBe(false);
    expect(result.hasNumber).toBe(false);
  });

  it('validates password with all requirements met', () => {
    const result = validatePassword('Password123');

    expect(result.minLength).toBe(true);
    expect(result.hasUppercase).toBe(true);
    expect(result.hasNumber).toBe(true);
  });

  it('validates short password even with uppercase and number', () => {
    const result = validatePassword('Pass1');

    expect(result.minLength).toBe(false);
    expect(result.hasUppercase).toBe(true);
    expect(result.hasNumber).toBe(true);
  });

  it('validates long password without uppercase', () => {
    const result = validatePassword('password123');

    expect(result.minLength).toBe(true);
    expect(result.hasUppercase).toBe(false);
    expect(result.hasNumber).toBe(true);
  });

  it('validates long password with uppercase but no number', () => {
    const result = validatePassword('Password');

    expect(result.minLength).toBe(true);
    expect(result.hasUppercase).toBe(true);
    expect(result.hasNumber).toBe(false);
  });
});

describe('isPasswordValid', () => {
  it('returns false for empty password', () => {
    expect(isPasswordValid('')).toBe(false);
  });

  it('returns false for password missing requirements', () => {
    expect(isPasswordValid('password')).toBe(false);
    expect(isPasswordValid('Pass1')).toBe(false);
    expect(isPasswordValid('password123')).toBe(false);
  });

  it('returns true for valid password', () => {
    expect(isPasswordValid('Password123')).toBe(true);
    expect(isPasswordValid('SecurePass1')).toBe(true);
    expect(isPasswordValid('MyP@ssw0rd')).toBe(true);
  });
});
