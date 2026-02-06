/**
 * CapabilityTargetList Component
 *
 * Main component for the EEG Setup page.
 * Displays list of Capability Targets with CRUD operations.
 */

import { useState } from 'react'
import type { FC } from 'react'
import {
  Box,
  Typography,
  Stack,
  Paper,
  Skeleton,
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus, faLightbulb } from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
} from '@/theme/styledComponents'
import { useCapabilityTargets } from '../hooks/useCapabilityTargets'
import { useCapabilities } from '@/features/capabilities/hooks/useCapabilities'
import { useOrganization } from '@/contexts/OrganizationContext'
import { CapabilityTargetCard } from './CapabilityTargetCard'
import { CapabilityTargetFormDialog } from './CapabilityTargetFormDialog'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import type {
  CapabilityTargetDto,
  CreateCapabilityTargetRequest,
  UpdateCapabilityTargetRequest,
} from '../types'

interface CapabilityTargetListProps {
  /** Exercise ID */
  exerciseId: string
  /** Whether the user can edit (Director+) */
  canEdit?: boolean
}

/**
 * List of Capability Targets for EEG Setup
 */
export const CapabilityTargetList: FC<CapabilityTargetListProps> = ({
  exerciseId,
  canEdit = true,
}) => {
  const { currentOrg } = useOrganization()

  const {
    capabilityTargets,
    loading,
    error,
    createCapabilityTarget,
    updateCapabilityTarget,
    deleteCapabilityTarget,
    isDeleting,
  } = useCapabilityTargets(exerciseId)

  // Only fetch capabilities from the current organization
  const { capabilities, loading: capabilitiesLoading } = useCapabilities({
    organizationId: currentOrg?.id,
  })

  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingTarget, setEditingTarget] = useState<CapabilityTargetDto | null>(null)
  const [deletingTarget, setDeletingTarget] = useState<CapabilityTargetDto | null>(null)

  const handleOpenCreate = () => {
    setEditingTarget(null)
    setIsFormOpen(true)
  }

  const handleOpenEdit = (target: CapabilityTargetDto) => {
    setEditingTarget(target)
    setIsFormOpen(true)
  }

  const handleCloseForm = () => {
    setIsFormOpen(false)
    setEditingTarget(null)
  }

  const handleCreate = async (request: CreateCapabilityTargetRequest) => {
    await createCapabilityTarget(request)
  }

  const handleUpdate = async (id: string, request: UpdateCapabilityTargetRequest) => {
    await updateCapabilityTarget(id, request)
  }

  const handleConfirmDelete = async () => {
    if (deletingTarget) {
      await deleteCapabilityTarget(deletingTarget.id)
      setDeletingTarget(null)
    }
  }

  // Build delete warning message
  const getDeleteWarning = (target: CapabilityTargetDto) => {
    if (target.criticalTaskCount > 0) {
      return `This will also delete ${target.criticalTaskCount} Critical Task${target.criticalTaskCount !== 1 ? 's' : ''} and all associated data.`
    }
    return null
  }

  if (loading) {
    return (
      <Stack spacing={2}>
        <Stack direction="row" justifyContent="space-between" alignItems="center">
          <Skeleton variant="text" width={200} height={32} />
          <Skeleton variant="rectangular" width={120} height={36} />
        </Stack>
        <Skeleton variant="rectangular" height={120} />
        <Skeleton variant="rectangular" height={120} />
      </Stack>
    )
  }

  if (error) {
    return <Alert severity="error">{error}</Alert>
  }

  return (
    <Box>
      {/* Header */}
      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
        <Box>
          <Typography variant="h6">Capability Targets</Typography>
          <Typography variant="body2" color="text.secondary">
            Define measurable performance thresholds for this exercise.
          </Typography>
        </Box>
        {canEdit && (
          <CobraPrimaryButton
            startIcon={<FontAwesomeIcon icon={faPlus} />}
            onClick={handleOpenCreate}
          >
            Add Target
          </CobraPrimaryButton>
        )}
      </Stack>

      {/* Target List */}
      {capabilityTargets.length === 0 ? (
        <Paper
          sx={{
            p: 4,
            textAlign: 'center',
            bgcolor: 'background.default',
          }}
        >
          <Typography color="text.secondary" gutterBottom>
            No Capability Targets defined yet.
          </Typography>
          <Typography variant="body2" color="text.secondary" mb={2}>
            Capability Targets establish what capabilities will be evaluated during this exercise
            and what success looks like for each.
          </Typography>
          {canEdit && (
            <CobraPrimaryButton
              startIcon={<FontAwesomeIcon icon={faPlus} />}
              onClick={handleOpenCreate}
            >
              Add First Target
            </CobraPrimaryButton>
          )}
        </Paper>
      ) : (
        <Stack spacing={1.5}>
          {capabilityTargets.map(target => (
            <CapabilityTargetCard
              key={target.id}
              exerciseId={exerciseId}
              target={target}
              canEdit={canEdit}
              onEdit={handleOpenEdit}
              onDelete={setDeletingTarget}
            />
          ))}
        </Stack>
      )}

      {/* Tip */}
      {capabilityTargets.length > 0 && (
        <Alert
          severity="info"
          icon={<FontAwesomeIcon icon={faLightbulb} />}
          sx={{ mt: 2 }}
        >
          After defining targets, expand each one to add Critical Tasks that specify
          the exact actions evaluators should observe. Then link injects to tasks
          in the MSEL view for full traceability.
        </Alert>
      )}

      {/* Form Dialog */}
      <CapabilityTargetFormDialog
        open={isFormOpen}
        target={editingTarget}
        capabilities={capabilities}
        capabilitiesLoading={capabilitiesLoading}
        onClose={handleCloseForm}
        onCreate={handleCreate}
        onUpdate={handleUpdate}
      />

      {/* Delete Confirmation Dialog */}
      <ConfirmDialog
        open={!!deletingTarget}
        title="Delete Capability Target"
        message={
          <>
            Are you sure you want to delete this Capability Target?
            <br />
            <br />
            <strong>{deletingTarget?.capability.name}</strong>
            <br />
            &quot;{deletingTarget?.targetDescription}&quot;
            {deletingTarget && getDeleteWarning(deletingTarget) && (
              <>
                <br />
                <br />
                <Typography component="span" color="warning.main">
                  {getDeleteWarning(deletingTarget)}
                </Typography>
              </>
            )}
          </>
        }
        confirmLabel="Delete"
        severity="danger"
        onConfirm={handleConfirmDelete}
        onCancel={() => setDeletingTarget(null)}
        isConfirming={isDeleting}
      />
    </Box>
  )
}

export default CapabilityTargetList
