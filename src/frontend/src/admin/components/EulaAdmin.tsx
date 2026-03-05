import { useState, useEffect } from 'react'
import type { FC } from 'react'
import {
  Box,
  Typography,
  Stack,
  CircularProgress,
  Alert,
  Tabs,
  Tab,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faSave, faFileContract, faSpinner, faEye, faPen, faTrash } from '@fortawesome/free-solid-svg-icons'
import Markdown from 'react-markdown'
import {
  CobraPrimaryButton,
  CobraDeleteButton,
  CobraTextField,
} from '@/theme/styledComponents'
import { useSystemSettings, useUpdateSystemSettings } from '../hooks/useSystemSettings'
import { notify } from '@/shared/utils/notify'
import { formatDateTime } from '@/shared/utils/dateUtils'

export const EulaAdmin: FC = () => {
  const { data: settings, isLoading, error } = useSystemSettings()
  const updateSettings = useUpdateSystemSettings()

  const [content, setContent] = useState('')
  const [version, setVersion] = useState('')
  const [previewTab, setPreviewTab] = useState(0) // 0 = edit, 1 = preview
  const [hasChanges, setHasChanges] = useState(false)

  useEffect(() => {
    if (settings) {
      setContent(settings.eulaContent ?? '')
      setVersion(settings.eulaVersion ?? '')
    }
  }, [settings])

  useEffect(() => {
    if (!settings) return
    const contentChanged = content !== (settings.eulaContent ?? '')
    const versionChanged = version !== (settings.eulaVersion ?? '')
    setHasChanges(contentChanged || versionChanged)
  }, [content, version, settings])

  const handleSave = () => {
    if (!content.trim() && !version.trim()) {
      notify.warning('Please provide both EULA content and a version identifier.')
      return
    }
    if (content.trim() && !version.trim()) {
      notify.warning('Please provide a version identifier for the EULA.')
      return
    }

    updateSettings.mutate(
      {
        // Pass existing non-EULA fields as null to leave them unchanged
        supportAddress: null,
        defaultSenderAddress: null,
        defaultSenderName: null,
        gitHubToken: null,
        gitHubOwner: null,
        gitHubRepo: null,
        gitHubLabelsEnabled: null,
        eulaContent: content.trim() || null,
        eulaVersion: version.trim() || null,
      },
      {
        onSuccess: () => notify.success('EULA settings saved.'),
        onError: () => notify.error('Failed to save EULA settings.'),
      },
    )
  }

  const handleClear = () => {
    updateSettings.mutate(
      {
        supportAddress: null,
        defaultSenderAddress: null,
        defaultSenderName: null,
        gitHubToken: null,
        gitHubOwner: null,
        gitHubRepo: null,
        gitHubLabelsEnabled: null,
        eulaContent: '',
        eulaVersion: '',
      },
      {
        onSuccess: () => {
          setContent('')
          setVersion('')
          notify.success('EULA has been removed.')
        },
        onError: () => notify.error('Failed to remove EULA.'),
      },
    )
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
        <CircularProgress size={24} />
      </Box>
    )
  }

  if (error) {
    return <Alert severity="error">Failed to load EULA settings.</Alert>
  }

  return (
    <Stack spacing={3}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <FontAwesomeIcon icon={faFileContract} />
        <Typography variant="h6">EULA / Terms of Use</Typography>
        {settings?.eulaConfigured && (
          <Typography variant="caption" color="text.secondary" sx={{ ml: 'auto' }}>
            v{settings.eulaVersion} · Updated {settings.eulaUpdatedAt ? formatDateTime(settings.eulaUpdatedAt) : 'N/A'}
          </Typography>
        )}
      </Box>

      <Typography variant="body2" color="text.secondary">
        Upload a EULA in Markdown format. When configured, users must accept the EULA after
        logging in before they can access the application. Changing the version will require
        all users to re-accept.
      </Typography>

      <CobraTextField
        label="Version"
        value={version}
        onChange={e => setVersion(e.target.value)}
        placeholder="e.g., 1.0, 2024-03"
        size="small"
        sx={{ maxWidth: 200 }}
      />

      <Box>
        <Tabs
          value={previewTab}
          onChange={(_, v) => setPreviewTab(v)}
          sx={{ mb: 1, minHeight: 36 }}
        >
          <Tab
            icon={<FontAwesomeIcon icon={faPen} size="sm" />}
            iconPosition="start"
            label="Edit"
            sx={{ minHeight: 36, textTransform: 'none' }}
          />
          <Tab
            icon={<FontAwesomeIcon icon={faEye} size="sm" />}
            iconPosition="start"
            label="Preview"
            sx={{ minHeight: 36, textTransform: 'none' }}
          />
        </Tabs>

        {previewTab === 0 ? (
          <CobraTextField
            multiline
            minRows={12}
            maxRows={24}
            fullWidth
            value={content}
            onChange={e => setContent(e.target.value)}
            placeholder="Paste your EULA content here in Markdown format..."
          />
        ) : (
          <Box
            sx={{
              border: 1,
              borderColor: 'divider',
              borderRadius: 1,
              p: 2,
              minHeight: 300,
              maxHeight: 500,
              overflowY: 'auto',
              '& h1': { fontSize: '1.5rem', mt: 2, mb: 1 },
              '& h2': { fontSize: '1.25rem', mt: 2, mb: 1 },
              '& h3': { fontSize: '1.1rem', mt: 1.5, mb: 0.5 },
              '& p': { mb: 1.5, lineHeight: 1.7 },
              '& ul, & ol': { pl: 3, mb: 1.5 },
              '& li': { mb: 0.5 },
            }}
          >
            {content.trim() ? (
              <Markdown>{content}</Markdown>
            ) : (
              <Typography color="text.secondary" sx={{ fontStyle: 'italic' }}>
                No content to preview.
              </Typography>
            )}
          </Box>
        )}
      </Box>

      <Box sx={{ display: 'flex', gap: 1, justifyContent: 'flex-end' }}>
        {settings?.eulaConfigured && (
          <CobraDeleteButton
            onClick={handleClear}
            disabled={updateSettings.isPending}
            startIcon={<FontAwesomeIcon icon={faTrash} />}
          >
            Remove EULA
          </CobraDeleteButton>
        )}
        <CobraPrimaryButton
          onClick={handleSave}
          disabled={!hasChanges || updateSettings.isPending}
          startIcon={
            updateSettings.isPending
              ? <FontAwesomeIcon icon={faSpinner} spin />
              : <FontAwesomeIcon icon={faSave} />
          }
        >
          Save EULA
        </CobraPrimaryButton>
      </Box>
    </Stack>
  )
}
