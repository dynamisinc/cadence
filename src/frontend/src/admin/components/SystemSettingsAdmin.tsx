import { useState, useEffect } from 'react'
import type { FC } from 'react'
import {
  Box,
  Typography,
  Stack,
  CircularProgress,
  Alert,
  IconButton,
  Tooltip,
  Divider,
  FormControlLabel,
  Switch,
  Chip,
  Link,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faSave, faEnvelope, faXmark, faPlug, faSpinner, faCircleQuestion } from '@fortawesome/free-solid-svg-icons'
import { faGithub } from '@fortawesome/free-brands-svg-icons'
import type { IconProp } from '@fortawesome/fontawesome-svg-core'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '@/theme/styledComponents'
import { formatDateTime } from '@/shared/utils/dateUtils'
import {
  useSystemSettings,
  useUpdateSystemSettings,
  useTestGitHubConnection,
} from '../hooks/useSystemSettings'
import { notify } from '@/shared/utils/notify'
import type { GitHubConnectionTestResult } from '../types/systemSettings'

export const SystemSettingsAdmin: FC = () => {
  const { data: settings, isLoading, error } = useSystemSettings()
  const updateSettings = useUpdateSystemSettings()

  const testConnection = useTestGitHubConnection()

  const [supportAddress, setSupportAddress] = useState('')
  const [defaultSenderName, setDefaultSenderName] = useState('')
  const [gitHubToken, setGitHubToken] = useState('')
  const [gitHubOwner, setGitHubOwner] = useState('')
  const [gitHubRepo, setGitHubRepo] = useState('')
  const [gitHubLabelsEnabled, setGitHubLabelsEnabled] = useState(false)
  const [testResult, setTestResult] = useState<GitHubConnectionTestResult | null>(null)
  const [hasChanges, setHasChanges] = useState(false)

  // Populate form when settings load
  useEffect(() => {
    if (settings) {
      setSupportAddress(settings.supportAddress ?? '')
      setDefaultSenderName(settings.defaultSenderName ?? '')
      setGitHubOwner(settings.gitHubOwner ?? '')
      setGitHubRepo(settings.gitHubRepo ?? '')
      setGitHubLabelsEnabled(settings.gitHubLabelsEnabled)
      setGitHubToken('')
      setHasChanges(false)
    }
  }, [settings])

  // Track changes
  useEffect(() => {
    if (settings) {
      const changed =
        supportAddress !== (settings.supportAddress ?? '') ||
        defaultSenderName !== (settings.defaultSenderName ?? '') ||
        gitHubToken !== '' ||
        gitHubOwner !== (settings.gitHubOwner ?? '') ||
        gitHubRepo !== (settings.gitHubRepo ?? '') ||
        gitHubLabelsEnabled !== settings.gitHubLabelsEnabled
      setHasChanges(changed)
    }
  }, [supportAddress, defaultSenderName, gitHubToken, gitHubOwner, gitHubRepo, gitHubLabelsEnabled, settings])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    try {
      await updateSettings.mutateAsync({
        supportAddress: supportAddress.trim() || null,
        defaultSenderAddress: null,
        defaultSenderName: defaultSenderName.trim() || null,
        gitHubToken: gitHubToken || null,
        gitHubOwner: gitHubOwner.trim() || null,
        gitHubRepo: gitHubRepo.trim() || null,
        gitHubLabelsEnabled: gitHubLabelsEnabled,
        eulaContent: null,
        eulaVersion: null,
      })
      notify.success('System settings updated')
      setHasChanges(false)
    } catch (err: unknown) {
      console.error('[SystemSettingsAdmin] Failed to update:', err)
      const errorMessage = err instanceof Error ? err.message : 'Failed to update settings'
      notify.error(errorMessage)
    }
  }

  const handleReset = () => {
    if (settings) {
      setSupportAddress(settings.supportAddress ?? '')
      setDefaultSenderName(settings.defaultSenderName ?? '')
      setGitHubToken('')
      setGitHubOwner(settings.gitHubOwner ?? '')
      setGitHubRepo(settings.gitHubRepo ?? '')
      setGitHubLabelsEnabled(settings.gitHubLabelsEnabled)
      setTestResult(null)
      setHasChanges(false)
    }
  }

  const handleTestConnection = async () => {
    setTestResult(null)
    try {
      const result = await testConnection.mutateAsync()
      setTestResult(result)
    } catch {
      setTestResult({ success: false, message: 'Failed to test connection' })
    }
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress size={32} />
      </Box>
    )
  }

  if (error) {
    return (
      <Alert severity="error">
        {error instanceof Error ? error.message : 'Failed to load system settings'}
      </Alert>
    )
  }

  return (
    <Box>
      <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mb: 3 }}>
        <FontAwesomeIcon icon={faEnvelope} />
        <Box>
          <Typography variant="h6">Email Configuration</Typography>
          <Typography variant="caption" color="text.secondary">
            Override default email settings. Leave blank to use deployment defaults.
          </Typography>
        </Box>
      </Stack>

      <form onSubmit={handleSubmit}>
        <Stack spacing={2.5} sx={{ maxWidth: 480 }}>
          <CobraTextField
            label="Support Email Address"
            value={supportAddress}
            onChange={e => setSupportAddress(e.target.value)}
            fullWidth
            type="email"
            placeholder={settings?.effectiveSupportAddress}
            helperText={
              supportAddress
                ? 'Overriding deployment default'
                : `Default: ${settings?.effectiveSupportAddress ?? 'not configured'}`
            }
            slotProps={{
              input: {
                endAdornment: supportAddress ? (
                  <Tooltip title="Clear override">
                    <IconButton size="small" onClick={() => setSupportAddress('')}>
                      <FontAwesomeIcon icon={faXmark} size="sm" />
                    </IconButton>
                  </Tooltip>
                ) : undefined,
              },
            }}
          />

          <CobraTextField
            label="Default Sender Name"
            value={defaultSenderName}
            onChange={e => setDefaultSenderName(e.target.value)}
            fullWidth
            placeholder={settings?.effectiveDefaultSenderName}
            helperText={
              defaultSenderName
                ? 'Overriding deployment default'
                : `Default: ${settings?.effectiveDefaultSenderName ?? 'not configured'}`
            }
            slotProps={{
              input: {
                endAdornment: defaultSenderName ? (
                  <Tooltip title="Clear override">
                    <IconButton size="small" onClick={() => setDefaultSenderName('')}>
                      <FontAwesomeIcon icon={faXmark} size="sm" />
                    </IconButton>
                  </Tooltip>
                ) : undefined,
              },
            }}
          />
        </Stack>

        <Divider sx={{ my: 4 }} />

        <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mb: 1 }}>
          <FontAwesomeIcon icon={faGithub as IconProp} />
          <Box>
            <Typography variant="h6">GitHub Integration</Typography>
            <Typography variant="caption" color="text.secondary">
              Automatically create GitHub issues from feedback submissions.
            </Typography>
          </Box>
        </Stack>

        <Alert severity="info" variant="outlined" sx={{ mb: 3 }}>
          <Typography variant="body2">
            To set up GitHub integration, you need a{' '}
            <Link
              href="https://github.com/settings/tokens?type=beta"
              target="_blank"
              rel="noreferrer"
            >
              Fine-grained Personal Access Token
            </Link>{' '}
            with <strong>Issues: Read and write</strong> permission on the target repository.{' '}
            <Link
              href="https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens#creating-a-fine-grained-personal-access-token"
              target="_blank"
              rel="noreferrer"
            >
              Learn how to create one
            </Link>
          </Typography>
        </Alert>

        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' },
            gap: 2.5,
            mb: 2.5,
          }}
        >
          <CobraTextField
            label="Personal Access Token"
            value={gitHubToken}
            onChange={e => setGitHubToken(e.target.value)}
            fullWidth
            type="password"
            placeholder={settings?.gitHubTokenConfigured ? '••••••••' + (settings.gitHubTokenMasked ?? '') : 'ghp_...'}
            helperText={
              settings?.gitHubTokenConfigured
                ? gitHubToken
                  ? 'Will replace existing token'
                  : `Token configured (ends in ${settings.gitHubTokenMasked ?? '****'})`
                : 'Generate at GitHub > Settings > Developer settings > Personal access tokens'
            }
            slotProps={{
              input: {
                endAdornment: settings?.gitHubTokenConfigured && !gitHubToken ? (
                  <Tooltip title="Remove token">
                    <IconButton size="small" onClick={() => setGitHubToken('__clear__')}>
                      <FontAwesomeIcon icon={faXmark} size="sm" />
                    </IconButton>
                  </Tooltip>
                ) : undefined,
              },
            }}
          />

          <Box />

          <CobraTextField
            label="Repository Owner"
            value={gitHubOwner}
            onChange={e => setGitHubOwner(e.target.value)}
            fullWidth
            placeholder="organization-or-username"
            helperText={
              <span>
                The owner from your repo URL: github.com/
                <strong>{gitHubOwner || 'owner'}</strong>/{gitHubRepo || 'repo'}
              </span>
            }
          />

          <CobraTextField
            label="Repository Name"
            value={gitHubRepo}
            onChange={e => setGitHubRepo(e.target.value)}
            fullWidth
            placeholder="repository-name"
            helperText={
              <span>
                The repo name from your URL: github.com/{gitHubOwner || 'owner'}/
                <strong>{gitHubRepo || 'repo'}</strong>
              </span>
            }
          />
        </Box>

        <Stack spacing={2.5}>
          <FormControlLabel
            control={
              <Switch
                checked={gitHubLabelsEnabled}
                onChange={e => setGitHubLabelsEnabled(e.target.checked)}
              />
            }
            label={
              <Stack direction="row" spacing={1} alignItems="center">
                <Typography variant="body2">
                  Add labels to issues automatically
                </Typography>
                <Tooltip
                  title="Bug reports get 'bug', feature requests get 'enhancement', and general feedback gets 'feedback'. Labels are created if they don't exist."
                  arrow
                >
                  <Box component="span" sx={{ color: 'text.secondary', cursor: 'help', fontSize: 14 }}>
                    <FontAwesomeIcon icon={faCircleQuestion} />
                  </Box>
                </Tooltip>
              </Stack>
            }
          />

          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <CobraSecondaryButton
              type="button"
              onClick={handleTestConnection}
              disabled={testConnection.isPending || !settings?.gitHubTokenConfigured || !settings?.gitHubOwner || !settings?.gitHubRepo || hasChanges}
              startIcon={
                testConnection.isPending ? (
                  <FontAwesomeIcon icon={faSpinner} spin />
                ) : (
                  <FontAwesomeIcon icon={faPlug} />
                )
              }
            >
              {testConnection.isPending ? 'Testing...' : 'Test Connection'}
            </CobraSecondaryButton>

            {testResult ? (
              <Chip
                label={testResult.message}
                color={testResult.success ? 'success' : 'error'}
                size="small"
                variant="outlined"
              />
            ) : hasChanges ? (
              <Typography variant="caption" color="text.secondary">
                Save changes before testing
              </Typography>
            ) : (!settings?.gitHubTokenConfigured || !settings?.gitHubOwner || !settings?.gitHubRepo) ? (
              <Typography variant="caption" color="text.secondary">
                Configure and save token, owner, and repo first
              </Typography>
            ) : null}
          </Box>
        </Stack>

        <Divider sx={{ my: 4 }} />

        {settings?.updatedAt && (
          <Typography variant="caption" color="text.secondary" sx={{ mb: 2, display: 'block' }}>
            Last updated: {formatDateTime(settings.updatedAt)}
            {settings.updatedBy && ` by ${settings.updatedBy}`}
          </Typography>
        )}

        <Box sx={{ display: 'flex', gap: 2, justifyContent: 'flex-end' }}>
          <CobraSecondaryButton
            type="button"
            onClick={handleReset}
            disabled={!hasChanges || updateSettings.isPending}
          >
            Reset
          </CobraSecondaryButton>
          <CobraPrimaryButton
            type="submit"
            disabled={!hasChanges || updateSettings.isPending}
            startIcon={
              updateSettings.isPending ? (
                <CircularProgress size={16} color="inherit" />
              ) : (
                <FontAwesomeIcon icon={faSave} />
              )
            }
          >
            {updateSettings.isPending ? 'Saving...' : 'Save Changes'}
          </CobraPrimaryButton>
        </Box>
      </form>
    </Box>
  )
}

export default SystemSettingsAdmin
