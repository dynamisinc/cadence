/**
 * Core Contexts
 *
 * Exports all context providers and hooks used throughout the application.
 *
 * @module core/contexts
 */

export { BreadcrumbProvider, useBreadcrumbContext, useBreadcrumbs } from './BreadcrumbContext'
export {
  ConnectivityProvider,
  useConnectivity,
  type ConnectivityState,
  type SignalRState,
} from './ConnectivityContext'

export {
  OfflineSyncProvider,
  useOfflineSyncContext,
} from './OfflineSyncContext'
