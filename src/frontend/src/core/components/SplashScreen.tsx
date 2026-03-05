import React, { useCallback, useEffect, useRef, useState } from 'react'
import { Box, Typography } from '@mui/material'
import { CobraIconButton } from '@/theme/styledComponents'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faXmark } from '@fortawesome/free-solid-svg-icons'
import { appVersion } from '@/config/version'

interface SplashScreenProps {
  onComplete: () => void;
}

const DISPLAY_MS = 4000
const FADE_MS = 500

/** Extract major.minor from semantic version string */
const getMajorMinor = (version: string) => {
  const parts = version.split('.')
  return parts.length >= 2 ? `${parts[0]}.${parts[1]}` : version
}

export const SplashScreen: React.FC<SplashScreenProps> = ({ onComplete }) => {
  const [fading, setFading] = useState(false)
  const [hovered, setHovered] = useState(false)
  const fadeTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const removeTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const startFadeOut = useCallback(() => {
    setFading(true)
    removeTimerRef.current = setTimeout(() => onComplete(), FADE_MS)
  }, [onComplete])

  // Auto-close timer — only runs when not hovered
  useEffect(() => {
    if (hovered) {
      if (fadeTimerRef.current) clearTimeout(fadeTimerRef.current)
      if (removeTimerRef.current) clearTimeout(removeTimerRef.current)
      fadeTimerRef.current = null
      removeTimerRef.current = null
      return
    }

    if (!fading) {
      fadeTimerRef.current = setTimeout(startFadeOut, DISPLAY_MS)
    }

    return () => {
      if (fadeTimerRef.current) clearTimeout(fadeTimerRef.current)
      if (removeTimerRef.current) clearTimeout(removeTimerRef.current)
    }
  }, [hovered, fading, startFadeOut])

  const handleClose = useCallback(() => {
    if (!fading) startFadeOut()
  }, [fading, startFadeOut])

  return (
    /* Backdrop — transparent on desktop so the page shows through, dimmed on mobile */
    <Box
      sx={{
        position: 'fixed',
        inset: 0,
        zIndex: 9999,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: { xs: '#1e3a5f', md: 'rgba(0, 0, 0, 0.5)' },
        opacity: fading ? 0 : 1,
        transition: `opacity ${FADE_MS}ms ease-out`,
        pointerEvents: fading ? 'none' : 'auto',
      }}
    >
      {/* Card — sized on desktop, full-viewport on mobile */}
      <Box
        role="status"
        aria-label="Loading Cadence"
        onMouseEnter={() => setHovered(true)}
        onMouseLeave={() => setHovered(false)}
        sx={{
          position: 'relative',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          backgroundColor: '#1e3a5f',
          // Mobile: full screen
          width: { xs: '100%', md: 480 },
          height: { xs: '100%', md: 'auto' },
          minHeight: { md: 400 },
          py: { md: 6 },
          px: { md: 4 },
          borderRadius: { xs: 0, md: 3 },
          boxShadow: { md: '0 24px 48px rgba(0, 0, 0, 0.3)' },
        }}
      >
        {/* Close button — visible when hovered */}
        <CobraIconButton
          onClick={handleClose}
          aria-label="Close splash screen"
          sx={{
            position: 'absolute',
            top: 12,
            right: 12,
            color: 'rgba(255, 255, 255, 0.6)',
            opacity: hovered ? 1 : 0,
            transition: 'opacity 200ms ease',
            '&:hover': { color: '#ffffff' },
          }}
        >
          <FontAwesomeIcon icon={faXmark} size="lg" />
        </CobraIconButton>

        {/* Logo with subtle pulse */}
        <Box
          component="img"
          src="/icon-source-light.svg"
          alt="Cadence Logo"
          sx={{
            width: 120,
            height: 120,
            mb: 3,
            borderRadius: 2,
            animation: 'splash-pulse 2s ease-in-out infinite',
            '@keyframes splash-pulse': {
              '0%, 100%': { transform: 'scale(1)', opacity: 0.9 },
              '50%': { transform: 'scale(1.05)', opacity: 1 },
            },
          }}
        />

        <Typography
          variant="h3"
          sx={{
            color: '#ffffff',
            fontWeight: 700,
            letterSpacing: 6,
            mb: 1,
          }}
        >
          CADENCE
        </Typography>

        <Typography
          variant="subtitle1"
          sx={{
            color: 'rgba(255, 255, 255, 0.7)',
            fontWeight: 300,
            letterSpacing: 1,
            mb: 4,
          }}
        >
          HSEEP MSEL Management Platform
        </Typography>

        <Typography
          variant="caption"
          sx={{
            color: 'rgba(255, 255, 255, 0.5)',
            textAlign: 'center',
            // On mobile: pinned to bottom like before. On desktop: inline at bottom of card.
            position: { xs: 'absolute', md: 'static' },
            bottom: { xs: 32 },
          }}
        >
          v{getMajorMinor(appVersion.version)}
          {' · '}
          &copy; {new Date().getFullYear()} Dynamis, Inc. All rights reserved.
        </Typography>
      </Box>
    </Box>
  )
}
