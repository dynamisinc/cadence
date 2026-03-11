/**
 * Role Orientation Content
 *
 * Defines per-role welcome messages, quick action cards,
 * and exercise role primers for the HomePage dashboard.
 */

import type { IconDefinition } from '@fortawesome/fontawesome-svg-core'
import {
  faUsers,
  faPlus,
  faGear,
  faFolderOpen,
  faClipboardList,
  faStar,
  faGamepad,
  faBinoculars,
  faEye,
} from '@fortawesome/free-solid-svg-icons'

import { cobraTheme } from '../../theme/cobraTheme'
import type { OrgRole } from '../../features/organizations/types'

export interface OrientationCard {
  title: string
  description: string
  icon: IconDefinition
  path: string
}

export interface RoleOrientation {
  headline: string
  description: string
  cards: OrientationCard[]
}

export const ORG_ROLE_ORIENTATIONS: Record<OrgRole, RoleOrientation> = {
  OrgAdmin: {
    headline: 'You manage this organization',
    description:
      'As an Organization Admin, you can configure settings, manage members, create exercises, and oversee all activity.',
    cards: [
      {
        title: 'Manage Members',
        description: 'Invite and manage team members',
        icon: faUsers,
        path: '/organization/members',
      },
      {
        title: 'Create Exercise',
        description: 'Set up a new HSEEP exercise',
        icon: faPlus,
        path: '/exercises/new',
      },
      {
        title: 'Organization Settings',
        description: 'Configure org-level preferences',
        icon: faGear,
        path: '/organization/details',
      },
      {
        title: 'View Exercises',
        description: 'Browse all exercises',
        icon: faFolderOpen,
        path: '/exercises',
      },
    ],
  },
  OrgManager: {
    headline: 'You coordinate exercises',
    description:
      'As a Manager, you can create exercises, manage participants, and oversee exercise conduct.',
    cards: [
      {
        title: 'Create Exercise',
        description: 'Set up a new HSEEP exercise',
        icon: faPlus,
        path: '/exercises/new',
      },
      {
        title: 'My Assignments',
        description: 'View your exercise role assignments',
        icon: faClipboardList,
        path: '/assignments',
      },
      {
        title: 'View Exercises',
        description: 'Browse all exercises',
        icon: faFolderOpen,
        path: '/exercises',
      },
    ],
  },
  OrgUser: {
    headline: 'You participate in exercises',
    description:
      'As a team member, you can view your exercise assignments and participate in exercises you are assigned to.',
    cards: [
      {
        title: 'My Assignments',
        description: 'See which exercises you are assigned to',
        icon: faClipboardList,
        path: '/assignments',
      },
      {
        title: 'View Exercises',
        description: 'Browse exercises in your organization',
        icon: faFolderOpen,
        path: '/exercises',
      },
    ],
  },
}

export interface ExerciseRolePrimer {
  role: string
  icon: IconDefinition
  color: string
  summary: string
}

export const EXERCISE_ROLE_PRIMERS: ExerciseRolePrimer[] = [
  {
    role: 'Exercise Director',
    icon: faStar,
    color: cobraTheme.palette.roleColor.exerciseDirector,
    summary:
      'Overall exercise authority. Makes Go/No-Go decisions and manages all aspects.',
  },
  {
    role: 'Controller',
    icon: faGamepad,
    color: cobraTheme.palette.roleColor.controller,
    summary:
      'Fires injects and manages scenario flow. Guides the exercise narrative.',
  },
  {
    role: 'Evaluator',
    icon: faBinoculars,
    color: cobraTheme.palette.roleColor.evaluator,
    summary:
      'Records observations and documents player performance against objectives.',
  },
  {
    role: 'Observer',
    icon: faEye,
    color: cobraTheme.palette.roleColor.observer,
    summary: 'Watches exercise conduct without interfering. Read-only access.',
  },
]
