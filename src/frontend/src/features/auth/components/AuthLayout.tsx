/**
 * AuthLayout - Shared layout for authentication pages
 *
 * Provides consistent layout with:
 * - Centered card (max-width 400px)
 * - Cadence branding at top
 * - Consistent padding and spacing
 * - Optional offline indicator slot
 *
 * @module features/auth
 */
import { FC, ReactNode } from 'react';
import { Box, Paper, Typography, Alert } from '@mui/material';
import CobraStyles from '../../../theme/CobraStyles';

interface AuthLayoutProps {
  /** Page title (e.g., "Sign In", "Create Account") */
  title: string;
  /** Form content */
  children: ReactNode;
  /** Optional offline indicator */
  showOfflineIndicator?: boolean;
}

/**
 * Centered authentication layout with consistent styling
 */
export const AuthLayout: FC<AuthLayoutProps> = ({
  title,
  children,
  showOfflineIndicator = false,
}) => {
  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'background.default',
        p: 2,
      }}
    >
      {/* Cadence Branding */}
      <Typography
        variant="h4"
        component="h1"
        sx={{ mb: 4, fontWeight: 500, color: 'text.primary' }}
      >
        CADENCE
      </Typography>

      {/* Auth Card */}
      <Paper
        sx={{
          width: '100%',
          maxWidth: 400,
          p: CobraStyles.Padding.DialogContent,
        }}
        elevation={3}
      >
        <Typography variant="h5" component="h2" sx={{ mb: 3, textAlign: 'center' }}>
          {title}
        </Typography>

        {children}
      </Paper>

      {/* Offline Indicator */}
      {showOfflineIndicator && (
        <Alert severity="warning" sx={{ mt: 2, maxWidth: 400, width: '100%' }}>
          Offline - Using cached session
        </Alert>
      )}
    </Box>
  );
};
