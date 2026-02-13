import { useState } from 'react'
import {
  Box,
  Typography,
  Paper,
  Tabs,
  Tab,
  Stack,
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faLightbulb, faPlus, faFileImport, faHome, faBuilding } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraSecondaryButton } from '@/theme/styledComponents'
import { useOrganization } from '@/contexts/OrganizationContext'
import { useBreadcrumbs } from '@/core/contexts'
import CobraStyles from '@/theme/CobraStyles'
import { SUGGESTION_FIELDS } from '../types'
import type { SuggestionFieldName } from '../types'
import { useFieldSuggestions } from '../hooks/useSuggestionManagement'
import { SuggestionTable } from '../components/SuggestionTable'
import { AddSuggestionDialog } from '../components/AddSuggestionDialog'
import { BulkPasteDialog } from '../components/BulkPasteDialog'

export const SuggestionManagementPage = () => {
  const { currentOrg } = useOrganization()
  const [activeTab, setActiveTab] = useState(0)
  const [showAddDialog, setShowAddDialog] = useState(false)
  const [showBulkDialog, setShowBulkDialog] = useState(false)

  const activeField = SUGGESTION_FIELDS[activeTab]
  const fieldName: SuggestionFieldName = activeField.name

  const { data: suggestions = [], isLoading, error } = useFieldSuggestions(fieldName)

  useBreadcrumbs([
    { label: 'Home', path: '/', icon: faHome },
    { label: 'Organization', path: '/organization/details', icon: faBuilding },
    { label: 'Autocomplete' },
  ])

  if (error) {
    return (
      <Box sx={{ padding: CobraStyles.Padding.MainWindow }}>
        <Alert severity="error">
          {error instanceof Error ? error.message : 'Failed to load suggestions'}
        </Alert>
      </Box>
    )
  }

  return (
    <Box sx={{ padding: CobraStyles.Padding.MainWindow }}>
      {/* Header */}
      <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mb: 2 }}>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'primary.main',
            fontSize: 28,
          }}
        >
          <FontAwesomeIcon icon={faLightbulb} />
        </Box>
        <Box sx={{ flex: 1 }}>
          <Typography variant="h5" fontWeight={600}>
            Autocomplete Suggestions
          </Typography>
          <Typography variant="caption" color="text.secondary">
            Manage curated suggestions for {currentOrg?.name || 'your organization'}
          </Typography>
        </Box>
      </Stack>

      <Paper sx={{ p: 0 }}>
        {/* Field Tabs */}
        <Tabs
          value={activeTab}
          onChange={(_e, newValue: number) => setActiveTab(newValue)}
          variant="scrollable"
          scrollButtons="auto"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          {SUGGESTION_FIELDS.map(field => (
            <Tab key={field.name} label={field.label} />
          ))}
        </Tabs>

        {/* Content */}
        <Box sx={{ p: 2.5 }}>
          {/* Action buttons */}
          <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
            <CobraPrimaryButton
              size="small"
              startIcon={<FontAwesomeIcon icon={faPlus} />}
              onClick={() => setShowAddDialog(true)}
            >
              Add Suggestion
            </CobraPrimaryButton>
            <CobraSecondaryButton
              size="small"
              startIcon={<FontAwesomeIcon icon={faFileImport} />}
              onClick={() => setShowBulkDialog(true)}
            >
              Bulk Paste
            </CobraSecondaryButton>
          </Stack>

          {/* Suggestion table */}
          <SuggestionTable
            suggestions={suggestions}
            fieldName={fieldName}
            isLoading={isLoading}
          />
        </Box>
      </Paper>

      {/* Dialogs */}
      <AddSuggestionDialog
        open={showAddDialog}
        onClose={() => setShowAddDialog(false)}
        fieldName={fieldName}
      />
      <BulkPasteDialog
        open={showBulkDialog}
        onClose={() => setShowBulkDialog(false)}
        fieldName={fieldName}
      />
    </Box>
  )
}

export default SuggestionManagementPage
