/**
 * NoOrgBanner - Info banner shown when user has no org selected
 *
 * Non-blocking banner that prompts users to select an organization.
 * For users with memberships, shows org chips for quick selection.
 * For SysAdmins without memberships, points to the header org switcher.
 * Dismissible per session.
 *
 * @module core/components
 */
import React, { useState } from 'react'
import { Alert, Box, Chip } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faBuilding, faArrowRight } from '@fortawesome/free-solid-svg-icons'
import { useOrganization } from '../../contexts/OrganizationContext'
import { useAuth } from '../../contexts/AuthContext'
import { notify } from '@/shared/utils/notify'

/**
 * Shows an info banner when the user hasn't selected an organization.
 * Allows inline org selection or dismissal.
 */
export const NoOrgBanner: React.FC = () => {
  const { needsOrgSelection, currentOrg, isLoading, memberships, switchOrganization } = useOrganization()
  const { user } = useAuth()
  const [dismissed, setDismissed] = useState(false)
  const [switching, setSwitching] = useState(false)

  const isSysAdmin = user?.role === 'Admin'

  // Show banner if: user has memberships but no org (needsOrgSelection),
  // OR SysAdmin with no org selected (they may have 0 memberships but can pick any org)
  const shouldShow = needsOrgSelection || (isSysAdmin && !currentOrg && !isLoading)

  if (!shouldShow || dismissed) return null

  const handleSelect = async (orgId: string) => {
    setSwitching(true)
    try {
      await switchOrganization(orgId)
      // Page will reload after switch
    } catch {
      notify.error('Failed to select organization. Please try again.')
      setSwitching(false)
    }
  }

  return (
    <Alert
      severity="info"
      onClose={() => setDismissed(true)}
      sx={{ mb: 2, borderRadius: 1, alignItems: 'center' }}
    >
      <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 1 }}>
        <FontAwesomeIcon icon={faBuilding} style={{ marginRight: 4 }} />
        {memberships.length > 0 ? (
          <>
            <span>No organization selected. Choose one to access all features:</span>
            {memberships.map(m => (
              <Chip
                key={m.id}
                label={m.organizationName}
                size="small"
                icon={<FontAwesomeIcon icon={faArrowRight} />}
                onClick={() => handleSelect(m.organizationId)}
                disabled={switching}
                clickable
                color="primary"
                variant="outlined"
              />
            ))}
          </>
        ) : (
          <span>No organization selected. Use the organization switcher in the header to choose one.</span>
        )}
      </Box>
    </Alert>
  )
}
