/**
 * Release note entry representing a single version release.
 */
export interface ReleaseNote {
  version: string;
  date: string;
  features: string[];
  fixes: string[];
  breaking?: string[];
}

/**
 * API version information returned from the backend.
 */
export interface ApiVersionInfo {
  version: string;
  commitSha?: string;
  buildDate?: string;
  environment: string;
}
