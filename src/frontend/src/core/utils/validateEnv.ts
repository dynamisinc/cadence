/**
 * Environment Validation Utility
 *
 * Validates required environment variables on app startup.
 * Throws detailed errors in development, logs warnings in production.
 */
import { devLog, devWarn } from './logger'

interface EnvConfig {
  /** Variable name */
  name: string;
  /** Whether the variable is required */
  required: boolean;
  /** Default value if not required */
  default?: string;
  /** Description for error messages */
  description: string;
}

const ENV_CONFIG: EnvConfig[] = [
  {
    name: 'VITE_API_URL',
    required: true,
    description: 'Backend API URL (Azure Functions endpoint)',
  },
  {
    name: 'VITE_SIGNALR_URL',
    required: false,
    default: '',
    description: 'SignalR hub URL for real-time features',
  },
]

interface ValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
  env: Record<string, string>;
}

/**
 * Validates environment variables and returns results
 */
export function validateEnvironment(): ValidationResult {
  const errors: string[] = []
  const warnings: string[] = []
  const env: Record<string, string> = {}

  for (const config of ENV_CONFIG) {
    const value = import.meta.env[config.name] as string | undefined

    if (config.required && !value) {
      errors.push(
        `Missing required environment variable: ${config.name} - ${config.description}`,
      )
    } else if (!config.required && !value) {
      if (config.default !== undefined) {
        env[config.name] = config.default
        warnings.push(
          `Optional variable ${config.name} not set, using default: "${config.default}"`,
        )
      }
    } else if (value) {
      env[config.name] = value
    }
  }

  return {
    isValid: errors.length === 0,
    errors,
    warnings,
    env,
  }
}

/**
 * Validates environment and logs/throws based on mode
 */
export function checkEnvironment(): void {
  const result = validateEnvironment()
  const isDev = import.meta.env.DEV

  // Log warnings in development only
  for (const warning of result.warnings) {
    devWarn(`[Env Warning] ${warning}`)
  }

  // Handle errors
  if (!result.isValid) {
    const errorMessage = [
      'Environment validation failed:',
      ...result.errors.map(e => `  - ${e}`),
      '',
      'Please check your .env file matches .env.example',
    ].join('\n')

    if (isDev) {
      // In development, throw to make the error visible
      console.error(errorMessage)
      throw new Error(errorMessage)
    } else {
      // In production, log error but don't crash (app may still partially work)
      console.error(`[Env Error] ${errorMessage}`)
    }
  } else if (isDev) {
    devLog('[Env] Environment validation passed')
  }
}

/**
 * Get validated environment variables
 */
export function getEnv() {
  return {
    apiUrl: import.meta.env.VITE_API_URL as string,
    signalRUrl: (import.meta.env.VITE_SIGNALR_URL as string) || '',
    isDev: import.meta.env.DEV,
    isProd: import.meta.env.PROD,
    mode: import.meta.env.MODE,
  }
}

export default checkEnvironment
