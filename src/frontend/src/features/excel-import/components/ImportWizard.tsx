/**
 * ImportWizard Component
 *
 * Main wizard component that orchestrates the Excel import flow.
 */

import { useState, useCallback } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  Box,
  Stepper,
  Step,
  StepLabel,
  IconButton,
  Typography,
  Stack,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faXmark } from '@fortawesome/free-solid-svg-icons'

import { FileUploadStep } from './FileUploadStep'
import { WorksheetSelectionStep } from './WorksheetSelectionStep'
import { ColumnMappingStep } from './ColumnMappingStep'
import { ValidationStep } from './ValidationStep'
import { ImportExecutionStep, type ImportOptions } from './ImportExecutionStep'

import {
  useUploadFile,
  useSelectWorksheet,
  useSuggestedMappings,
  useValidateImport,
  useExecuteImport,
  useCancelImport,
} from '../hooks/useExcelImport'
import { useDownloadTemplate } from '../../excel-export/hooks/useExcelExport'
import type {
  FileAnalysisResult,
  WorksheetSelectionResult,
  ColumnMapping,
  ValidationResult,
  ImportResult,
  ImportWizardStepType,
} from '../types'
import { ImportWizardStep } from '../types'

const STEPS = [
  { key: ImportWizardStep.Upload, label: 'Upload' },
  { key: ImportWizardStep.SheetSelection, label: 'Select Sheet' },
  { key: ImportWizardStep.Mapping, label: 'Map Columns' },
  { key: ImportWizardStep.Validation, label: 'Validate' },
  { key: ImportWizardStep.Import, label: 'Import' },
]

interface ImportWizardProps {
  /** Whether the wizard dialog is open */
  open: boolean
  /** Called when the wizard should close */
  onClose: () => void
  /** Exercise ID to import into */
  exerciseId: string
  /** Called when import is complete */
  onImportComplete?: (result: ImportResult) => void
}

export const ImportWizard = ({
  open,
  onClose,
  exerciseId,
  onImportComplete,
}: ImportWizardProps) => {
  // Wizard state
  const [currentStep, setCurrentStep] = useState<ImportWizardStepType>(ImportWizardStep.Upload)
  const [sessionId, setSessionId] = useState<string | null>(null)
  const [analysisResult, setAnalysisResult] = useState<FileAnalysisResult | null>(null)
  const [selectionResult, setSelectionResult] = useState<WorksheetSelectionResult | null>(null)
  const [mappings, setMappings] = useState<ColumnMapping[]>([])
  const [validationResult, setValidationResult] = useState<ValidationResult | null>(null)
  const [importResult, setImportResult] = useState<ImportResult | null>(null)
  const [skipErrors, setSkipErrors] = useState(false)

  // API hooks
  const uploadMutation = useUploadFile()
  const selectWorksheetMutation = useSelectWorksheet()
  const { data: suggestedMappings, isLoading: isLoadingMappings } = useSuggestedMappings(
    currentStep === ImportWizardStep.Mapping ? sessionId ?? undefined : undefined,
  )
  const validateMutation = useValidateImport()
  const executeMutation = useExecuteImport()
  const cancelMutation = useCancelImport()
  const downloadTemplateMutation = useDownloadTemplate()

  // Get current step index
  const currentStepIndex = STEPS.findIndex(s => s.key === currentStep)

  // Reset wizard state
  const resetWizard = useCallback(() => {
    if (sessionId) {
      cancelMutation.mutate(sessionId)
    }
    setCurrentStep(ImportWizardStep.Upload)
    setSessionId(null)
    setAnalysisResult(null)
    setSelectionResult(null)
    setMappings([])
    setValidationResult(null)
    setImportResult(null)
    setSkipErrors(false)
  }, [sessionId, cancelMutation])

  // Handle close
  const handleClose = () => {
    resetWizard()
    onClose()
  }

  // Step 1: File Upload
  const handleFileAnalyzed = (result: FileAnalysisResult) => {
    setAnalysisResult(result)
    setSessionId(result.sessionId)
    setCurrentStep(ImportWizardStep.SheetSelection)
  }

  // Step 2: Worksheet Selection
  const handleSelectWorksheet = async (index: number) => {
    if (!sessionId) return
    const result = await selectWorksheetMutation.mutateAsync({
      sessionId,
      worksheetIndex: index,
      previewRowCount: 5,
    })
    setSelectionResult(result)
  }

  const handleWorksheetConfirm = () => {
    setCurrentStep(ImportWizardStep.Mapping)
  }

  // Step 3: Column Mapping
  const handleMappingConfirm = async (newMappings: ColumnMapping[]) => {
    if (!sessionId) return
    setMappings(newMappings)

    // Validate with the new mappings
    const result = await validateMutation.mutateAsync({
      sessionId,
      mappings: newMappings,
    })
    setValidationResult(result)
    setCurrentStep(ImportWizardStep.Validation)
  }

  // Step 4: Validation
  const handleValidationProceed = (shouldSkipErrors: boolean) => {
    setSkipErrors(shouldSkipErrors)
    setCurrentStep(ImportWizardStep.Import)
  }

  // Step 5: Import Execution
  const handleImport = async (options: ImportOptions) => {
    if (!sessionId) return

    const result = await executeMutation.mutateAsync({
      sessionId,
      exerciseId,
      strategy: options.strategy,
      skipErrorRows: options.skipErrorRows,
      createMissingPhases: options.createMissingPhases,
      createMissingObjectives: options.createMissingObjectives,
    })

    setImportResult(result)

    if (onImportComplete) {
      onImportComplete(result)
    }
  }

  // Navigation handlers
  const handleBack = () => {
    switch (currentStep) {
      case ImportWizardStep.SheetSelection:
        setCurrentStep(ImportWizardStep.Upload)
        break
      case ImportWizardStep.Mapping:
        setCurrentStep(ImportWizardStep.SheetSelection)
        break
      case ImportWizardStep.Validation:
        setCurrentStep(ImportWizardStep.Mapping)
        break
      case ImportWizardStep.Import:
        setCurrentStep(ImportWizardStep.Validation)
        break
    }
  }

  const handleImportAnother = () => {
    resetWizard()
  }

  // Render current step content
  const renderStepContent = () => {
    switch (currentStep) {
      case ImportWizardStep.Upload:
        return (
          <FileUploadStep
            onFileAnalyzed={handleFileAnalyzed}
            uploadFile={file => uploadMutation.mutateAsync(file)}
            isUploading={uploadMutation.isPending}
            uploadError={uploadMutation.error?.message ?? null}
            onDownloadTemplate={() => downloadTemplateMutation.mutate()}
            isDownloadingTemplate={downloadTemplateMutation.isPending}
          />
        )

      case ImportWizardStep.SheetSelection:
        if (!analysisResult) return null
        return (
          <WorksheetSelectionStep
            analysisResult={analysisResult}
            selectionResult={selectionResult}
            isLoading={selectWorksheetMutation.isPending}
            error={selectWorksheetMutation.error?.message ?? null}
            onSelectWorksheet={handleSelectWorksheet}
            onBack={handleBack}
            onConfirm={handleWorksheetConfirm}
          />
        )

      case ImportWizardStep.Mapping:
        if (!selectionResult) return null
        return (
          <ColumnMappingStep
            columns={selectionResult.columns}
            initialMappings={suggestedMappings ?? mappings}
            previewRows={selectionResult.previewRows}
            isLoading={isLoadingMappings}
            error={null}
            onBack={handleBack}
            onConfirm={handleMappingConfirm}
          />
        )

      case ImportWizardStep.Validation:
        if (!validationResult) return null
        return (
          <ValidationStep
            validationResult={validationResult}
            mappings={mappings}
            isLoading={validateMutation.isPending}
            error={validateMutation.error?.message ?? null}
            onBack={handleBack}
            onProceed={handleValidationProceed}
          />
        )

      case ImportWizardStep.Import:
        if (!validationResult) return null
        return (
          <ImportExecutionStep
            rowCount={skipErrors ? validationResult.validRows : validationResult.totalRows}
            hasErrors={validationResult.errorRows > 0}
            isImporting={executeMutation.isPending}
            importResult={importResult}
            error={executeMutation.error?.message ?? null}
            onBack={handleBack}
            onImport={handleImport}
            onViewMsel={handleClose}
            onImportAnother={handleImportAnother}
          />
        )

      default:
        return null
    }
  }

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      maxWidth="lg"
      fullWidth
      PaperProps={{
        sx: {
          minHeight: '60vh',
          // Responsive width optimization
          width: {
            xs: '95%', // Mobile: nearly full width
            sm: '90%', // Tablet: 90% width
            md: '85%', // Small laptop: 85% width
            lg: '80%', // Large laptop: 80% width (optimal for most imports)
            xl: '75%', // Desktop: 75% width
          },
          maxWidth: {
            xs: '100%',
            sm: '100%',
            md: '900px',
            lg: '1200px', // Optimal width for Excel import workflows
            xl: '1400px',
          },
        },
      }}
    >
      <DialogTitle>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Typography variant="h6">Import MSEL from Excel</Typography>
          <IconButton onClick={handleClose} size="small">
            <FontAwesomeIcon icon={faXmark} />
          </IconButton>
        </Stack>
      </DialogTitle>

      <DialogContent dividers>
        {/* Stepper */}
        <Stepper activeStep={currentStepIndex} sx={{ mb: 4 }}>
          {STEPS.map(step => (
            <Step key={step.key}>
              <StepLabel>{step.label}</StepLabel>
            </Step>
          ))}
        </Stepper>

        {/* Step Content */}
        <Box>{renderStepContent()}</Box>
      </DialogContent>
    </Dialog>
  )
}

export default ImportWizard
