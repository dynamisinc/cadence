import { Chip } from '@mui/material'
import { useTheme } from '@mui/material/styles'
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
  icon: IconDefinition
}

const statusIconConfig: Record<string, StatusConfig> = {
  Draft: { label: 'Draft', icon: faPencil },
  Submitted: { label: 'Submitted', icon: faClock },
  Approved: { label: 'Approved', icon: faCheck },
  Synchronized: { label: 'Synchronized', icon: faCalendarCheck },
  Released: { label: 'Released', icon: faPaperPlane },
  Complete: { label: 'Complete', icon: faCircleCheck },
  Deferred: { label: 'Deferred', icon: faBan },
  Obsolete: { label: 'Obsolete', icon: faArchive },
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
  const theme = useTheme()

  const statusKey = status.toLowerCase() as keyof typeof theme.palette.injectStatus
  const colors =
    theme.palette.injectStatus[statusKey] ?? theme.palette.injectStatus.draft

  const config = statusIconConfig[status] ?? {
    label: status,
    icon: faPencil,
  }

  return (
    <Chip
      label={config.label}
      size={size}
      icon={
        <FontAwesomeIcon
          icon={config.icon}
          style={{ color: colors.text, marginLeft: 8 }}
        />
      }
      sx={{
        backgroundColor: colors.bg,
        color: colors.text,
        fontWeight: 500,
        minWidth: 90,
        '& .MuiChip-icon': {
          color: colors.text,
        },
      }}
    />
  )
}

export default InjectStatusChip
