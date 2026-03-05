import React, { useCallback, useRef, useState } from 'react'
import { Box, Dialog, DialogActions, DialogContent, DialogTitle, Typography } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faFileContract, faSpinner } from '@fortawesome/free-solid-svg-icons'
import Markdown from 'react-markdown'
import { CobraPrimaryButton } from '@/theme/styledComponents'
import { useAuth } from '@/contexts/AuthContext'
import { useEulaStatus, useAcceptEula } from '../hooks/useEula'
import { Loading } from '@/shared/components/Loading'

interface EulaGateProps {
  children: React.ReactNode;
}

export const EulaGate: React.FC<EulaGateProps> = ({ children }) => {
  const { isAuthenticated, isLoading: isAuthLoading } = useAuth()
  const { data: eulaStatus, isLoading: isEulaLoading } = useEulaStatus(isAuthenticated && !isAuthLoading)
  const acceptMutation = useAcceptEula()
  const [hasScrolledToBottom, setHasScrolledToBottom] = useState(false)
  const contentRef = useRef<HTMLDivElement>(null)

  const handleScroll = useCallback(() => {
    const el = contentRef.current
    if (!el) return
    // Consider "scrolled to bottom" when within 20px of the end
    const atBottom = el.scrollHeight - el.scrollTop - el.clientHeight < 20
    if (atBottom) setHasScrolledToBottom(true)
  }, [])

  const handleAccept = useCallback(() => {
    if (!eulaStatus?.version) return
    acceptMutation.mutate(eulaStatus.version)
  }, [acceptMutation, eulaStatus?.version])

  // Not authenticated yet or still loading — render children (auth guards handle the rest)
  if (!isAuthenticated || isAuthLoading) {
    return <>{children}</>
  }

  // EULA status still loading
  if (isEulaLoading) {
    return <Loading />
  }

  // No EULA required — pass through
  if (!eulaStatus?.required) {
    return <>{children}</>
  }

  // EULA acceptance required — show blocking dialog
  return (
    <>
      {children}
      <Dialog
        open
        maxWidth="md"
        fullWidth
        disableEscapeKeyDown
        slotProps={{ backdrop: { sx: { backgroundColor: 'rgba(0, 0, 0, 0.8)' } } }}
        PaperProps={{
          sx: { height: '80vh', display: 'flex', flexDirection: 'column' },
        }}
      >
        <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1.5, pb: 1 }}>
          <FontAwesomeIcon icon={faFileContract} />
          <Typography variant="h6" component="span">
            Terms of Use
          </Typography>
          {eulaStatus.version && (
            <Typography variant="caption" color="text.secondary" sx={{ ml: 'auto' }}>
              Version {eulaStatus.version}
            </Typography>
          )}
        </DialogTitle>

        <DialogContent
          ref={contentRef}
          onScroll={handleScroll}
          dividers
          sx={{
            flex: 1,
            overflowY: 'auto',
            '& h1': { fontSize: '1.5rem', mt: 2, mb: 1 },
            '& h2': { fontSize: '1.25rem', mt: 2, mb: 1 },
            '& h3': { fontSize: '1.1rem', mt: 1.5, mb: 0.5 },
            '& p': { mb: 1.5, lineHeight: 1.7 },
            '& ul, & ol': { pl: 3, mb: 1.5 },
            '& li': { mb: 0.5 },
          }}
        >
          <Markdown>{eulaStatus.content ?? ''}</Markdown>
        </DialogContent>

        <DialogActions sx={{ px: 3, py: 2, justifyContent: 'space-between' }}>
          <Box>
            {!hasScrolledToBottom && (
              <Typography variant="caption" color="text.secondary">
                Please scroll to the bottom to continue
              </Typography>
            )}
          </Box>
          <CobraPrimaryButton
            onClick={handleAccept}
            disabled={!hasScrolledToBottom || acceptMutation.isPending}
            startIcon={
              acceptMutation.isPending
                ? <FontAwesomeIcon icon={faSpinner} spin />
                : undefined
            }
          >
            I Accept
          </CobraPrimaryButton>
        </DialogActions>
      </Dialog>
    </>
  )
}
