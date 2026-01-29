/**
 * CapabilityLibraryPage
 *
 * Admin page for managing organizational capabilities.
 * Allows viewing, creating, editing, and deactivating capabilities.
 * Capabilities can be imported from predefined frameworks or created custom.
 */

import { useState } from 'react'
import {
  Box,
  Typography,
  Paper,
  Stack,
  CircularProgress,
  Alert,
  FormControlLabel,
  Switch,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faShieldHalved,
  faPlus,
  faHome,
} from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton } from '@/theme/styledComponents'
import CobraStyles from '@/theme/CobraStyles'
import { useBreadcrumbs } from '@/core/contexts'
import { useCapabilities } from '../hooks/useCapabilities'
import { getUniqueCategories } from '../types'
import type { CapabilityDto, CreateCapabilityRequest, UpdateCapabilityRequest } from '../types'
import CapabilityList from '../components/CapabilityList'
import CapabilityDialog from '../components/CapabilityDialog'
import ImportLibraryMenu from '../components/ImportLibraryMenu'

/**
 * Main capability library management page
 */
export const CapabilityLibraryPage = () => {
  // State
  const [showInactive, setShowInactive] = useState(false)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingCapability, setEditingCapability] = useState<CapabilityDto | null>(null)

  // Set breadcrumbs
  useBreadcrumbs([
    { label: 'Home', path: '/', icon: faHome },
    { label: 'Admin', path: '/admin' },
    { label: 'Capability Library' },
  ])

  // Fetch capabilities
  const {
    capabilities,
    loading,
    error,
    createCapability,
    updateCapability,
    deleteCapability,
    reactivateCapability,
    isCreating,
    isDeleting,
    isReactivating,
  } = useCapabilities(showInactive)

  // Get unique categories for the dialog dropdown
  const existingCategories = getUniqueCategories(capabilities).filter(
    cat => cat !== 'Uncategorized',
  )

  // Handlers
  const handleAddClick = () => {
    setEditingCapability(null)
    setDialogOpen(true)
  }

  const handleEditClick = (capability: CapabilityDto) => {
    setEditingCapability(capability)
    setDialogOpen(true)
  }

  const handleDialogClose = () => {
    setDialogOpen(false)
    setEditingCapability(null)
  }

  const handleCreate = async (request: CreateCapabilityRequest) => {
    await createCapability(request)
  }

  const handleUpdate = async (id: string, request: UpdateCapabilityRequest) => {
    await updateCapability(id, request)
  }

  const handleDeactivate = async (id: string) => {
    await deleteCapability(id)
  }

  const handleReactivate = async (id: string) => {
    await reactivateCapability(id)
  }

  // Loading state
  if (loading) {
    return (
      <Box
        sx={{
          padding: CobraStyles.Padding.MainWindow,
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          minHeight: 400,
        }}
      >
        <CircularProgress size={48} />
      </Box>
    )
  }

  return (
    <Box sx={{ padding: CobraStyles.Padding.MainWindow }}>
      {/* Header */}
      <Stack direction="row" spacing={2} alignItems="flex-start" sx={{ mb: 3 }}>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'primary.main',
            fontSize: 32,
          }}
        >
          <FontAwesomeIcon icon={faShieldHalved} />
        </Box>
        <Box sx={{ flex: 1 }}>
          <Typography variant="h4" fontWeight={600}>
            Capability Library
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Define the capabilities your organization can evaluate during exercises.
            These can be tagged to observations for performance tracking.
          </Typography>
        </Box>
      </Stack>

      {/* Error display */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {/* Actions bar */}
      <Paper sx={{ p: 2, mb: 3 }}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          justifyContent="space-between"
          alignItems={{ xs: 'stretch', sm: 'center' }}
          spacing={2}
        >
          <Stack direction="row" spacing={2} alignItems="center" flexWrap="wrap">
            <CobraPrimaryButton
              onClick={handleAddClick}
              startIcon={<FontAwesomeIcon icon={faPlus} />}
              disabled={isCreating}
            >
              Add Capability
            </CobraPrimaryButton>
            <ImportLibraryMenu />
            <Typography variant="body2" color="text.secondary">
              {capabilities.filter(c => c.isActive).length} active capabilities
              {showInactive && ` (${capabilities.length} total)`}
            </Typography>
          </Stack>

          <FormControlLabel
            control={
              <Switch
                checked={showInactive}
                onChange={e => setShowInactive(e.target.checked)}
                size="small"
              />
            }
            label="Show inactive"
          />
        </Stack>
      </Paper>

      {/* Capability list */}
      <Paper sx={{ p: 2 }}>
        <CapabilityList
          capabilities={capabilities}
          onEdit={handleEditClick}
          onDeactivate={handleDeactivate}
          onReactivate={handleReactivate}
          isDeleting={isDeleting}
          isReactivating={isReactivating}
        />
      </Paper>

      {/* Add/Edit Dialog */}
      <CapabilityDialog
        open={dialogOpen}
        capability={editingCapability}
        existingCategories={existingCategories}
        onClose={handleDialogClose}
        onCreate={handleCreate}
        onUpdate={handleUpdate}
      />
    </Box>
  )
}

export default CapabilityLibraryPage
