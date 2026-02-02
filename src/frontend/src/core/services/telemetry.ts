/**
 * Application Insights telemetry service for client-side monitoring.
 *
 * Tracks:
 * - Page views and navigation
 * - JavaScript errors and exceptions
 * - Custom events (user actions, feature usage)
 * - Performance metrics (load times, API call durations)
 * - User sessions
 *
 * Respects user privacy - no PII is collected by default.
 */
import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import type { ITelemetryItem } from '@microsoft/applicationinsights-web';
import { ReactPlugin } from '@microsoft/applicationinsights-react-js';

// React plugin for automatic component tracking
export const reactPlugin = new ReactPlugin();

// Application Insights instance
let appInsights: ApplicationInsights | null = null;

/**
 * Initialize Application Insights telemetry.
 * Call this once at app startup (in main.tsx).
 */
export function initializeTelemetry(): ApplicationInsights | null {
  const connectionString = import.meta.env.VITE_APPINSIGHTS_CONNECTION_STRING;

  if (!connectionString) {
    console.debug('[Telemetry] App Insights not configured - skipping initialization');
    return null;
  }

  try {
    appInsights = new ApplicationInsights({
      config: {
        connectionString,
        extensions: [reactPlugin],
        enableAutoRouteTracking: true, // Track page views on route change
        enableCorsCorrelation: true, // Correlate with backend requests
        enableRequestHeaderTracking: true,
        enableResponseHeaderTracking: true,
        enableAjaxPerfTracking: true, // Track API call performance
        maxAjaxCallsPerView: 500,
        disableFetchTracking: false,
        autoTrackPageVisitTime: true,
        enableUnhandledPromiseRejectionTracking: true,

        // Sampling - keep all errors, sample other telemetry
        samplingPercentage: 100, // Start with 100%, can reduce if volume is high

        // Privacy settings
        isStorageUseDisabled: false, // Allow session tracking
        isCookieUseDisabled: false, // Allow correlation cookies
        disableExceptionTracking: false,

        // Performance
        maxBatchInterval: 15000, // Send telemetry every 15 seconds
        maxBatchSizeInBytes: 102400, // 100KB max batch size
      },
    });

    appInsights.loadAppInsights();

    // Add telemetry initializer to enrich all telemetry items
    appInsights.addTelemetryInitializer((item: ITelemetryItem) => {
      // Add custom properties to all telemetry
      item.data = item.data || {};

      // Add app version if available
      const appVersion = import.meta.env.VITE_APP_VERSION;
      if (appVersion) {
        item.data['app_version'] = appVersion;
      }

      // Add environment
      item.data['environment'] = import.meta.env.MODE;

      // Sanitize - remove any accidentally captured sensitive data
      if (item.baseData) {
        // Redact tokens from URLs
        if (item.baseData.uri) {
          item.baseData.uri = sanitizeUrl(item.baseData.uri);
        }
        if (item.baseData.refUri) {
          item.baseData.refUri = sanitizeUrl(item.baseData.refUri);
        }
      }

      return true; // Include this telemetry item
    });

    console.debug('[Telemetry] App Insights initialized successfully');
    return appInsights;
  } catch (error) {
    console.error('[Telemetry] Failed to initialize App Insights:', error);
    return null;
  }
}

/**
 * Get the Application Insights instance.
 * Returns null if not initialized.
 */
export function getAppInsights(): ApplicationInsights | null {
  return appInsights;
}

/**
 * Track a custom event (user action, feature usage, etc.)
 */
export function trackEvent(
  name: string,
  properties?: Record<string, string>,
  measurements?: Record<string, number>
): void {
  if (!appInsights) return;

  appInsights.trackEvent({
    name,
    properties,
    measurements,
  });
}

/**
 * Track an exception/error
 */
export function trackException(
  error: Error,
  properties?: Record<string, string>
): void {
  if (!appInsights) return;

  appInsights.trackException({
    exception: error,
    properties,
  });
}

/**
 * Track a page view manually (automatic tracking is enabled by default)
 */
export function trackPageView(
  name: string,
  uri?: string,
  properties?: Record<string, string>
): void {
  if (!appInsights) return;

  appInsights.trackPageView({
    name,
    uri,
    properties,
  });
}

/**
 * Track a metric (custom measurement)
 */
export function trackMetric(
  name: string,
  average: number,
  properties?: Record<string, string>
): void {
  if (!appInsights) return;

  appInsights.trackMetric({
    name,
    average,
    properties,
  });
}

/**
 * Track an API call duration
 */
export function trackDependency(
  name: string,
  url: string,
  durationMs: number,
  success: boolean,
  resultCode?: number
): void {
  if (!appInsights) return;

  appInsights.trackDependencyData({
    id: crypto.randomUUID(),
    name,
    target: new URL(url).host,
    type: 'HTTP',
    duration: durationMs,
    success,
    responseCode: resultCode ?? 0,
    data: sanitizeUrl(url),
  });
}

/**
 * Set the authenticated user context for correlation
 * Call this after user logs in
 */
export function setAuthenticatedUser(
  userId: string,
  accountId?: string
): void {
  if (!appInsights) return;

  // Use a hash of the user ID for privacy (don't send actual user ID)
  const hashedUserId = hashString(userId);
  appInsights.setAuthenticatedUserContext(hashedUserId, accountId ? hashString(accountId) : undefined, true);
}

/**
 * Clear the authenticated user context
 * Call this after user logs out
 */
export function clearAuthenticatedUser(): void {
  if (!appInsights) return;

  appInsights.clearAuthenticatedUserContext();
}

/**
 * Flush any pending telemetry (call before app unload if needed)
 */
export function flushTelemetry(): void {
  if (!appInsights) return;

  appInsights.flush();
}

// === Helper functions ===

/**
 * Simple hash function for privacy (not cryptographic)
 */
function hashString(str: string): string {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    const char = str.charCodeAt(i);
    hash = (hash << 5) - hash + char;
    hash = hash & hash; // Convert to 32bit integer
  }
  return Math.abs(hash).toString(16);
}

/**
 * Sanitize URLs to remove sensitive query parameters
 */
function sanitizeUrl(url: string): string {
  try {
    const parsed = new URL(url, window.location.origin);
    const sensitiveParams = ['token', 'access_token', 'refresh_token', 'code', 'password', 'secret'];

    sensitiveParams.forEach((param) => {
      if (parsed.searchParams.has(param)) {
        parsed.searchParams.set(param, '[REDACTED]');
      }
    });

    return parsed.toString();
  } catch {
    // If URL parsing fails, return as-is
    return url;
  }
}
