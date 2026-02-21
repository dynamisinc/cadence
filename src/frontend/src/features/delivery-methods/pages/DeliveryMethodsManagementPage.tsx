import { useState } from 'react'
import {
  Box,
  Paper,
  Stack,
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPaperPlane, faPlus, faHome, faShieldHalved } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton } from '@/theme/styledComponents'
import { useBreadcrumbs } from '@/core/contexts'
import CobraStyles from '@/theme/CobraStyles'
import { useAllDeliveryMethods } from '../hooks/useDeliveryMethodManagement'
import { DeliveryMethodTable } from '../components/DeliveryMethodTable'
import { AddDeliveryMethodDialog } from '../components/AddDeliveryMethodDialog'
import { PageHeader } from '@/shared/components'

export const DeliveryMethodsManagementPage = () => {
  const [showAddDialog, setShowAddDialog] = useState(false)
  const { data: methods = [], isLoading, error } = useAllDeliveryMethods()

  useBreadcrumbs([
    { label: 'Home', path: '/', icon: faHome },
    { label: 'System Settings', path: '/admin', icon: faShieldHalved },
    { label: 'Delivery Methods' },
  ])

  if (error) {
    return (
      <Box sx={{ padding: CobraStyles.Padding.MainWindow }}>
        <Alert severity="error">
          {error instanceof Error ? error.message : 'Failed to load delivery methods'}
        </Alert>
      </Box>
    )
  }

  return (
    <Box sx={{ padding: CobraStyles.Padding.MainWindow }}>
      <PageHeader
        title="Delivery Methods"
        icon={faPaperPlane}
        subtitle="Manage system-wide delivery methods available in the inject form"
        mb={2}
      />

      <Paper sx={{ p: 2.5 }}>
        {/* Action buttons */}
        <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
          <CobraPrimaryButton
            size="small"
            startIcon={<FontAwesomeIcon icon={faPlus} />}
            onClick={() => setShowAddDialog(true)}
          >
            Add Delivery Method
          </CobraPrimaryButton>
        </Stack>

        {/* Table */}
        <DeliveryMethodTable methods={methods} isLoading={isLoading} />
      </Paper>

      {/* Dialogs */}
      <AddDeliveryMethodDialog
        open={showAddDialog}
        onClose={() => setShowAddDialog(false)}
      />
    </Box>
  )
}

export default DeliveryMethodsManagementPage
