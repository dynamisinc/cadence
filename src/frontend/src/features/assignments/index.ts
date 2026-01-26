/**
 * Assignments Feature
 *
 * My Assignments feature for viewing exercise assignments.
 */

// Pages
export { MyAssignmentsPage } from './pages/MyAssignmentsPage'

// Components
export { AssignmentCard } from './components/AssignmentCard'
export { AssignmentSection } from './components/AssignmentSection'

// Hooks
export { useMyAssignments, ASSIGNMENTS_QUERY_KEY } from './hooks/useMyAssignments'

// Utils
export {
  getDefaultRouteForRole,
  getRoleLabel,
  getRoleColor,
  isActiveStatus,
} from './utils/roleRouting'

// Types
export type {
  AssignmentDto,
  MyAssignmentsResponse,
  AssignmentSectionType,
  AssignmentSectionProps,
} from './types'
