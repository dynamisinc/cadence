/**
 * StatusChip - Display organization status with color coding
 *
 * Shows organization status (Active, Archived, Inactive) with
 * semantic colors following MUI theme patterns.
 *
 * @module shared/components
 */
import type { FC } from 'react';
import { Chip, type ChipProps } from '@mui/material';
import type { OrgStatus } from '@/features/organizations/types';

interface StatusChipProps {
  /** Organization status */
  status: OrgStatus;
  /** Chip size */
  size?: 'small' | 'medium';
}

/** Valid status values */
const VALID_STATUSES: OrgStatus[] = ['Active', 'Archived', 'Inactive'];

/**
 * Normalize status value - handles invalid/numeric values
 */
function normalizeStatus(status: OrgStatus | string | number): OrgStatus {
  if (VALID_STATUSES.includes(status as OrgStatus)) {
    return status as OrgStatus;
  }
  // Default invalid values to 'Inactive' so restore button appears
  return 'Inactive';
}

/**
 * Map status to MUI color
 */
function getStatusColor(status: OrgStatus): ChipProps['color'] {
  switch (status) {
    case 'Active':
      return 'success';
    case 'Archived':
      return 'warning';
    case 'Inactive':
      return 'error';
    default:
      return 'default';
  }
}

/**
 * StatusChip component
 */
export const StatusChip: FC<StatusChipProps> = ({ status, size = 'small' }) => {
  const normalizedStatus = normalizeStatus(status);
  return (
    <Chip
      label={normalizedStatus}
      color={getStatusColor(normalizedStatus)}
      size={size}
      sx={{ fontWeight: 500 }}
    />
  );
};
