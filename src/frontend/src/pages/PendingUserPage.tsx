/**
 * PendingUserPage - Shown when user has no organization assigned
 *
 * Displays a message explaining they're pending organization assignment,
 * with an option to enter an organization code to join.
 *
 * @module pages
 * @see docs/features/organization-management/OM-06-organization-switcher.md
 */
import { FC, useState } from 'react';
import { Box, Typography, Paper, Alert } from '@mui/material';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faHourglassHalf } from '@fortawesome/free-solid-svg-icons';
import { CobraPrimaryButton, CobraTextField } from '@/theme/styledComponents';
import CobraStyles from '@/theme/CobraStyles';

/**
 * PendingUserPage component
 */
export const PendingUserPage: FC = () => {
  const [orgCode, setOrgCode] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleJoinOrganization = async () => {
    if (!orgCode.trim()) return;

    setIsSubmitting(true);

    // TODO: Implement organization code redemption API call
    // This will be part of P1 stories (OM-08)
    console.log('Joining organization with code:', orgCode);

    // For now, just show a message
    alert('Organization code redemption will be implemented in a future release.');
    setIsSubmitting(false);
  };

  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: '100vh',
        bgcolor: 'background.default',
        p: CobraStyles.Padding.MainWindow,
      }}
    >
      <Paper
        elevation={2}
        sx={{
          maxWidth: 500,
          width: '100%',
          p: 4,
          textAlign: 'center',
        }}
      >
        {/* Icon */}
        <Box sx={{ mb: 2 }}>
          <FontAwesomeIcon icon={faHourglassHalf} size="3x" color="#757575" />
        </Box>

        {/* Title */}
        <Typography variant="h5" component="h1" gutterBottom>
          Waiting for Organization Assignment
        </Typography>

        {/* Message */}
        <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
          Your account has been created, but you haven't been assigned to an organization yet.
        </Typography>

        {/* Organization Code Section */}
        <Box sx={{ mb: 3 }}>
          <Alert severity="info" sx={{ mb: 2 }}>
            Have an organization code? Enter it below to join.
          </Alert>

          <CobraTextField
            label="Organization Code"
            value={orgCode}
            onChange={(e) => setOrgCode(e.target.value.toUpperCase())}
            placeholder="Enter 8-character code"
            fullWidth
            inputProps={{
              maxLength: 8,
              'aria-label': 'Organization Code',
            }}
            sx={{ mb: 2 }}
          />

          <CobraPrimaryButton
            onClick={handleJoinOrganization}
            disabled={!orgCode.trim() || isSubmitting}
            fullWidth
          >
            {isSubmitting ? 'Joining...' : 'Join Organization'}
          </CobraPrimaryButton>
        </Box>

        {/* Contact Info */}
        <Typography variant="body2" color="text.secondary">
          Or contact your administrator for access.
        </Typography>
      </Paper>
    </Box>
  );
};
