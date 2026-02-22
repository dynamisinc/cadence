import { useMemo, useState } from 'react'
import {
  Box,
  Typography,
  Paper,
  Tabs,
  Tab,
  Stack,
  Alert,
  Divider,
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
import { HistoricalValuesSection } from '../components/HistoricalValuesSection'
import { PageHeader } from '@/shared/components'

export const SuggestionManagementPage = () => {
  const { currentOrg } = useOrganization()
  const [activeTab, setActiveTab] = useState(0)
  const [showAddDialog, setShowAddDialog] = useState(false)
  const [showBulkDialog, setShowBulkDialog] = useState(false)

  const activeField = SUGGESTION_FIELDS[activeTab]
  const fieldName: SuggestionFieldName = activeField.name

  const { data: allSuggestions = [], isLoading, error } = useFieldSuggestions(fieldName)

  // Split curated suggestions from blocked entries
  const suggestions = useMemo(
    () => allSuggestions.filter(s => !s.isBlocked),
    [allSuggestions],
  )
  const blockedSuggestions = useMemo(
    () => allSuggestions.filter(s => s.isBlocked),
    [allSuggestions],
  )

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
      <PageHeader
        title="Autocomplete Suggestions"
        icon={faLightbulb}
        subtitle="Manage autocomplete values for inject form fields"
        mb={2}
      />

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

          {/* Historical values section */}
          <Divider sx={{ my: 3 }} />
          <Typography variant="subtitle1" fontWeight={600} sx={{ mb: 1 }}>
            Historical Values
          </Typography>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 2 }}>
            Values auto-learned from past injects. Block unwanted values to hide them from
            autocomplete suggestions.
          </Typography>
          <HistoricalValuesSection
            fieldName={fieldName}
            blockedSuggestions={blockedSuggestions}
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
