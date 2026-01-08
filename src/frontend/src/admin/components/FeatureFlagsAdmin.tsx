/**
 * Feature Flags Admin Component
 *
 * Admin UI for managing feature flag states:
 * - Displays all configurable features in a grid
 * - Toggle button group for each flag (Hidden/Coming Soon/Active)
 * - Reset to defaults button
 * - Groups features by category
 */

import React from 'react'
import {
  Box,
  Card,
  CardContent,
  Typography,
  ToggleButton,
  ToggleButtonGroup,
  Stack,
  Chip,
  Button,
  Divider,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faEye,
  faEyeSlash,
  faClock,
  faCheck,
  faRotateLeft,
} from '@fortawesome/free-solid-svg-icons'
import { useFeatureFlags } from '../contexts/FeatureFlagsContext'
import type {
  FeatureFlags,
  FeatureFlagState,
} from '../types/featureFlags'
import {
  featureFlagInfo,
  getFeatureStateColor,
  getFeatureStateLabel,
} from '../types/featureFlags'

/**
 * Get icon for feature state
 */
const getStateIcon = (state: FeatureFlagState) => {
  switch (state) {
    case 'Active':
      return faCheck
    case 'ComingSoon':
      return faClock
    case 'Hidden':
      return faEyeSlash
  }
}

/**
 * Feature Flag Card Component
 */
interface FeatureFlagCardProps {
  flagKey: keyof FeatureFlags;
  label: string;
  description: string;
  state: FeatureFlagState;
  onStateChange: (newState: FeatureFlagState) => void;
}

const FeatureFlagCard: React.FC<FeatureFlagCardProps> = ({
  flagKey,
  label,
  description,
  state,
  onStateChange,
}) => {
  const handleChange = (
    _event: React.MouseEvent<HTMLElement>,
    newState: FeatureFlagState | null,
  ) => {
    if (newState !== null) {
      onStateChange(newState)
    }
  }

  return (
    <Card
      data-testid={`feature-flag-card-${flagKey}`}
      sx={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
      }}
    >
      <CardContent sx={{ flex: 1 }}>
        <Stack spacing={2}>
          {/* Header */}
          <Box
            sx={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'flex-start',
            }}
          >
            <Typography variant="h6" component="h3">
              {label}
            </Typography>
            <Chip
              icon={
                <FontAwesomeIcon
                  icon={getStateIcon(state)}
                  style={{ marginLeft: 8 }}
                />
              }
              label={getFeatureStateLabel(state)}
              color={getFeatureStateColor(state)}
              size="small"
            />
          </Box>

          {/* Description */}
          <Typography variant="body2" color="text.secondary">
            {description}
          </Typography>

          {/* State Toggle */}
          <ToggleButtonGroup
            value={state}
            exclusive
            onChange={handleChange}
            aria-label={`${label} state`}
            size="small"
            fullWidth
            data-testid={`feature-flag-toggle-${flagKey}`}
          >
            <ToggleButton value="Hidden" aria-label="Hidden">
              <FontAwesomeIcon icon={faEyeSlash} style={{ marginRight: 8 }} />
              Hidden
            </ToggleButton>
            <ToggleButton value="ComingSoon" aria-label="Coming Soon">
              <FontAwesomeIcon icon={faClock} style={{ marginRight: 8 }} />
              Coming Soon
            </ToggleButton>
            <ToggleButton value="Active" aria-label="Active">
              <FontAwesomeIcon icon={faEye} style={{ marginRight: 8 }} />
              Active
            </ToggleButton>
          </ToggleButtonGroup>
        </Stack>
      </CardContent>
    </Card>
  )
}

/**
 * Feature Flags Admin Panel
 */
export const FeatureFlagsAdmin: React.FC = () => {
  const { flags, updateFlag, resetFlags } = useFeatureFlags()

  // Group features by category
  const toolsFlags = featureFlagInfo.filter(f => f.category === 'tools')
  const experimentalFlags = featureFlagInfo.filter(
    f => f.category === 'experimental',
  )

  const renderFlagGroup = (
    title: string,
    flagList: typeof featureFlagInfo,
    testId: string,
  ) => (
    <Box data-testid={testId}>
      <Typography variant="subtitle1" fontWeight="bold" gutterBottom>
        {title}
      </Typography>
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: {
            xs: '1fr',
            sm: 'repeat(2, 1fr)',
            md: 'repeat(3, 1fr)',
          },
          gap: 2,
        }}
      >
        {flagList.map(flag => (
          <FeatureFlagCard
            key={flag.key}
            flagKey={flag.key}
            label={flag.label}
            description={flag.description}
            state={flags[flag.key]}
            onStateChange={newState => updateFlag(flag.key, newState)}
          />
        ))}
      </Box>
    </Box>
  )

  return (
    <Box data-testid="feature-flags-admin">
      <Stack spacing={3}>
        {/* Header */}
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <Box>
            <Typography variant="h5" component="h2">
              Feature Flags
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Control feature visibility and availability across the application
            </Typography>
          </Box>
          <Button
            variant="outlined"
            startIcon={<FontAwesomeIcon icon={faRotateLeft} />}
            onClick={resetFlags}
            data-testid="reset-flags-button"
          >
            Reset to Defaults
          </Button>
        </Box>

        <Divider />

        {/* Tools Section */}
        {toolsFlags.length > 0 &&
          renderFlagGroup('Tools', toolsFlags, 'tools-flags-section')}

        {/* Experimental Section */}
        {experimentalFlags.length > 0 && (
          <>
            <Divider />
            {renderFlagGroup(
              'Experimental',
              experimentalFlags,
              'experimental-flags-section',
            )}
          </>
        )}
      </Stack>
    </Box>
  )
}

export default FeatureFlagsAdmin
