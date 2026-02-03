import { Chip } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPencil,
  faClock,
  faCheck,
  faCalendarCheck,
  faPaperPlane,
  faCircleCheck,
  faBan,
  faArchive,
} from '@fortawesome/free-solid-svg-icons'
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core'
import type { InjectStatus } from '../../../types'

interface InjectStatusChipProps {
  status: InjectStatus
  size?: 'small' | 'medium'
}

/**
 * HSEEP-compliant status chip configuration.
 * Colors and icons per FEMA PrepToolkit visual standards.
 */
interface StatusConfig {
  label: string
  bgColor: string
  textColor: string
  icon: IconDefinition
}

const statusConfig: Record<string, StatusConfig> = {
  Draft: {
    label: 'Draft',
    bgColor: '#E0E0E0',
    textColor: '#616161',
    icon: faPencil,
  },
  Submitted: {
    label: 'Submitted',
    bgColor: '#FFE0B2',
    textColor: '#F57C00',
    icon: faClock,
  },
  Approved: {
    label: 'Approved',
    bgColor: '#C8E6C9',
    textColor: '#388E3C',
    icon: faCheck,
  },
  Synchronized: {
    label: 'Synchronized',
    bgColor: '#BBDEFB',
    textColor: '#1976D2',
    icon: faCalendarCheck,
  },
  Released: {
    label: 'Released',
    bgColor: '#E1BEE7',
    textColor: '#7B1FA2',
    icon: faPaperPlane,
  },
  Complete: {
    label: 'Complete',
    bgColor: '#A5D6A7',
    textColor: '#1B5E20',
    icon: faCircleCheck,
  },
  Deferred: {
    label: 'Deferred',
    bgColor: '#FFCC80',
    textColor: '#E65100',
    icon: faBan,
  },
  Obsolete: {
    label: 'Obsolete',
    bgColor: '#F5F5F5',
    textColor: '#9E9E9E',
    icon: faArchive,
  },
}

/**
 * Get inject status chip configuration
 */
const getStatusConfig = (status: string): StatusConfig => {
  return (
    statusConfig[status] || {
      label: status,
      bgColor: '#E0E0E0',
      textColor: '#616161',
      icon: faPencil,
    }
  )
}

/**
 * HSEEP-compliant status chip for injects.
 *
 * Status colors per FEMA PrepToolkit visual standards:
 * - Draft: Gray (initial authoring)
 * - Submitted: Amber (awaiting approval)
 * - Approved: Green (director approved)
 * - Synchronized: Blue (scheduled)
 * - Released: Purple (delivered to players)
 * - Complete: Dark Green (delivery confirmed)
 * - Deferred: Orange (cancelled)
 * - Obsolete: Light Gray (audit trail)
 */
export const InjectStatusChip = ({
  status,
  size = 'small',
}: InjectStatusChipProps) => {
  const config = getStatusConfig(status)

  return (
    <Chip
      label={config.label}
      size={size}
      icon={
        <FontAwesomeIcon
          icon={config.icon}
          style={{ color: config.textColor, marginLeft: 8 }}
        />
      }
      sx={{
        backgroundColor: config.bgColor,
        color: config.textColor,
        fontWeight: 500,
        minWidth: 90,
        '& .MuiChip-icon': {
          color: config.textColor,
        },
      }}
    />
  )
}

export default InjectStatusChip
