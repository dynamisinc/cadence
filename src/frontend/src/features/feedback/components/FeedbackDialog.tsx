/**
 * FeedbackDialog Component
 *
 * Tabbed dialog for submitting bug reports, feature requests, and general feedback.
 * Uses COBRA styled components and FontAwesome icons.
 */

import { useState } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Box,
  Typography,
  Tabs,
  Tab,
  Stack,
  Alert,
  MenuItem,
  Select,
  FormControl,
  InputLabel,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faCommentDots,
  faBug,
  faLightbulb,
  faMessage,
  faSpinner,
} from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '@/theme/styledComponents'
import { feedbackService } from '../services/feedbackService'
import type { FeedbackClientContext, FeedbackTab } from '../types'
import { useExerciseNavigation } from '@/shared/contexts'
import { notify } from '@/shared/utils/notify'

interface FeedbackDialogProps {
  open: boolean
  onClose: () => void
}

const SEVERITY_OPTIONS = ['Low', 'Medium', 'High', 'Critical']
const FEEDBACK_CATEGORIES = [
  'General',
  'User Interface',
  'Performance',
  'Documentation',
  'Other',
]

export const FeedbackDialog = ({ open, onClose }: FeedbackDialogProps) => {
  const { currentExercise } = useExerciseNavigation()

  const buildContext = (): FeedbackClientContext => ({
    currentUrl: window.location.href,
    screenSize: `${window.screen.width}x${window.screen.height}`,
    appVersion: __APP_VERSION__,
    commitSha: __COMMIT_SHA__,
    exerciseId: currentExercise?.id,
    exerciseName: currentExercise?.name,
    exerciseRole: currentExercise?.userRole,
  })

  const [activeTab, setActiveTab] = useState<FeedbackTab>('bug')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Bug report fields
  const [bugTitle, setBugTitle] = useState('')
  const [bugDescription, setBugDescription] = useState('')
  const [bugSteps, setBugSteps] = useState('')
  const [bugSeverity, setBugSeverity] = useState('Medium')

  // Feature request fields
  const [featureTitle, setFeatureTitle] = useState('')
  const [featureDescription, setFeatureDescription] = useState('')
  const [featureUseCase, setFeatureUseCase] = useState('')

  // General feedback fields
  const [feedbackCategory, setFeedbackCategory] = useState('General')
  const [feedbackSubject, setFeedbackSubject] = useState('')
  const [feedbackMessage, setFeedbackMessage] = useState('')

  const resetForm = () => {
    setBugTitle('')
    setBugDescription('')
    setBugSteps('')
    setBugSeverity('Medium')
    setFeatureTitle('')
    setFeatureDescription('')
    setFeatureUseCase('')
    setFeedbackCategory('General')
    setFeedbackSubject('')
    setFeedbackMessage('')
    setError(null)
  }

  const handleClose = () => {
    resetForm()
    onClose()
  }

  const isFormValid = (): boolean => {
    switch (activeTab) {
      case 'bug':
        return bugTitle.trim() !== '' && bugDescription.trim() !== ''
      case 'feature':
        return featureTitle.trim() !== '' && featureDescription.trim() !== ''
      case 'feedback':
        return feedbackSubject.trim() !== '' && feedbackMessage.trim() !== ''
    }
  }

  const handleSubmit = async () => {
    setIsSubmitting(true)
    setError(null)

    try {
      let result

      const context = buildContext()

      switch (activeTab) {
        case 'bug':
          result = await feedbackService.submitBugReport({
            title: bugTitle,
            description: bugDescription,
            stepsToReproduce: bugSteps || undefined,
            severity: bugSeverity,
            context,
          })
          break
        case 'feature':
          result = await feedbackService.submitFeatureRequest({
            title: featureTitle,
            description: featureDescription,
            useCase: featureUseCase || undefined,
            context,
          })
          break
        case 'feedback':
          result = await feedbackService.submitGeneralFeedback({
            category: feedbackCategory,
            subject: feedbackSubject,
            message: feedbackMessage,
            context,
          })
          break
      }

      notify.success(
        `${result.message} Reference: ${result.referenceNumber}`,
      )
      resetForm()
      onClose()
    } catch {
      setError('Failed to submit. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      maxWidth="sm"
      fullWidth
      aria-labelledby="feedback-dialog-title"
    >
      <DialogTitle
        id="feedback-dialog-title"
        sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}
      >
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'primary.main',
            fontSize: 24,
          }}
        >
          <FontAwesomeIcon icon={faCommentDots} />
        </Box>
        Send Feedback
      </DialogTitle>

      <DialogContent dividers>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        <Tabs
          value={activeTab}
          onChange={(_, val) => {
            setActiveTab(val)
            setError(null)
          }}
          sx={{ mb: 2, borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab
            value="bug"
            label={
              <Stack direction="row" spacing={1} alignItems="center">
                <FontAwesomeIcon icon={faBug} />
                <span>Bug Report</span>
              </Stack>
            }
          />
          <Tab
            value="feature"
            label={
              <Stack direction="row" spacing={1} alignItems="center">
                <FontAwesomeIcon icon={faLightbulb} />
                <span>Feature Request</span>
              </Stack>
            }
          />
          <Tab
            value="feedback"
            label={
              <Stack direction="row" spacing={1} alignItems="center">
                <FontAwesomeIcon icon={faMessage} />
                <span>Feedback</span>
              </Stack>
            }
          />
        </Tabs>

        {/* Bug Report Tab */}
        {activeTab === 'bug' && (
          <Stack spacing={2}>
            <CobraTextField
              label="Title"
              value={bugTitle}
              onChange={e => setBugTitle(e.target.value)}
              placeholder="Brief summary of the issue"
              required
              fullWidth
            />
            <CobraTextField
              label="Description"
              value={bugDescription}
              onChange={e => setBugDescription(e.target.value)}
              placeholder="What happened? What did you expect?"
              multiline
              rows={3}
              required
              fullWidth
            />
            <CobraTextField
              label="Steps to Reproduce"
              value={bugSteps}
              onChange={e => setBugSteps(e.target.value)}
              placeholder="1. Go to...\n2. Click on...\n3. See error"
              multiline
              rows={3}
              fullWidth
            />
            <FormControl fullWidth size="small">
              <InputLabel>Severity</InputLabel>
              <Select
                value={bugSeverity}
                label="Severity"
                onChange={e => setBugSeverity(e.target.value)}
              >
                {SEVERITY_OPTIONS.map(opt => (
                  <MenuItem key={opt} value={opt}>
                    {opt}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Stack>
        )}

        {/* Feature Request Tab */}
        {activeTab === 'feature' && (
          <Stack spacing={2}>
            <CobraTextField
              label="Title"
              value={featureTitle}
              onChange={e => setFeatureTitle(e.target.value)}
              placeholder="Brief summary of the feature"
              required
              fullWidth
            />
            <CobraTextField
              label="Description"
              value={featureDescription}
              onChange={e => setFeatureDescription(e.target.value)}
              placeholder="Describe the feature you'd like"
              multiline
              rows={3}
              required
              fullWidth
            />
            <CobraTextField
              label="Use Case"
              value={featureUseCase}
              onChange={e => setFeatureUseCase(e.target.value)}
              placeholder="How would this help your workflow?"
              multiline
              rows={2}
              fullWidth
            />
          </Stack>
        )}

        {/* General Feedback Tab */}
        {activeTab === 'feedback' && (
          <Stack spacing={2}>
            <FormControl fullWidth size="small">
              <InputLabel>Category</InputLabel>
              <Select
                value={feedbackCategory}
                label="Category"
                onChange={e => setFeedbackCategory(e.target.value)}
              >
                {FEEDBACK_CATEGORIES.map(cat => (
                  <MenuItem key={cat} value={cat}>
                    {cat}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <CobraTextField
              label="Subject"
              value={feedbackSubject}
              onChange={e => setFeedbackSubject(e.target.value)}
              placeholder="What's your feedback about?"
              required
              fullWidth
            />
            <CobraTextField
              label="Message"
              value={feedbackMessage}
              onChange={e => setFeedbackMessage(e.target.value)}
              placeholder="Share your thoughts..."
              multiline
              rows={4}
              required
              fullWidth
            />
          </Stack>
        )}

        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ display: 'block', mt: 2 }}
        >
          Your feedback is sent to our team and helps us improve Cadence.
        </Typography>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        <CobraSecondaryButton onClick={handleClose}>
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleSubmit}
          disabled={!isFormValid() || isSubmitting}
          startIcon={
            isSubmitting ? (
              <FontAwesomeIcon icon={faSpinner} spin />
            ) : undefined
          }
        >
          {isSubmitting ? 'Submitting...' : 'Submit'}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default FeedbackDialog
