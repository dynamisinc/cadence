/**
 * Shared SignalR type definitions
 *
 * Extracted to avoid duplication between `useSignalR` and `useExerciseSignalR`.
 * Both hooks represent the connection lifecycle with the same set of states.
 *
 * @module shared/hooks
 */

/**
 * Represents the current state of a SignalR connection.
 *
 * - `disconnected`  — not connected (initial or after explicit disconnect)
 * - `connecting`    — connection attempt in progress
 * - `connected`     — successfully connected to the hub
 * - `reconnecting`  — lost connection, automatic reconnect in progress
 * - `error`         — connection failed with an unrecoverable error
 */
export type ConnectionState =
  | 'disconnected'
  | 'connecting'
  | 'connected'
  | 'reconnecting'
  | 'error'
