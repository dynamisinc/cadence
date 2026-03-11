import { useState } from 'react'
import { Menu, MenuItem, ListItemIcon, ListItemText, CircularProgress, Alert } from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faFileExcel,
  faFileArchive,
  faChevronDown,
  faClipboardList,
} from '@fortawesome/free-solid-svg-icons'
import { CobraSecondaryButton } from '@/theme/styledComponents'
import { useExportMsel, useExportObservations, useExportFullPackage } from '../hooks/useExcelExport'
import type { ExportType } from '../types'

export interface ExportButtonProps {
  exerciseId: string
  disabled?: boolean
  size?: 'small' | 'medium'
}

/**
 * Export dropdown button component.
 * Provides options to export MSEL, Observations, or Full Package.
 */
export function ExportButton({
  exerciseId,
  disabled = false,
  size = 'medium',
}: ExportButtonProps): React.ReactElement {
  const theme = useTheme()
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)
  const [error, setError] = useState<string | null>(null)
  const open = Boolean(anchorEl)

  const exportMsel = useExportMsel()
  const exportObservations = useExportObservations()
  const exportFullPackage = useExportFullPackage()

  const isExporting =
    exportMsel.isPending || exportObservations.isPending || exportFullPackage.isPending

  const handleClick = (event: React.MouseEvent<HTMLButtonElement>) => {
    setError(null)
    setAnchorEl(event.currentTarget)
  }

  const handleClose = () => {
    setAnchorEl(null)
  }

  const handleExport = async (type: ExportType) => {
    handleClose()
    setError(null)

    try {
      switch (type) {
        case 'msel':
          await exportMsel.mutateAsync({
            exerciseId,
            format: 'xlsx',
            includeFormatting: true,
            includeConductData: true,
          })
          break
        case 'observations':
          await exportObservations.mutateAsync({
            exerciseId,
            includeFormatting: true,
          })
          break
        case 'full':
          await exportFullPackage.mutateAsync({
            exerciseId,
            includeFormatting: true,
          })
          break
      }
    } catch {
      setError('Export failed. Please try again.')
    }
  }

  return (
    <>
      <CobraSecondaryButton
        id="export-button"
        aria-controls={open ? 'export-menu' : undefined}
        aria-haspopup="true"
        aria-expanded={open ? 'true' : undefined}
        onClick={handleClick}
        disabled={disabled || isExporting}
        size={size}
        endIcon={
          isExporting ? (
            <CircularProgress size={16} color="inherit" />
          ) : (
            <FontAwesomeIcon icon={faChevronDown} />
          )
        }
      >
        {isExporting ? 'Exporting...' : 'Export'}
      </CobraSecondaryButton>

      <Menu
        id="export-menu"
        anchorEl={anchorEl}
        open={open}
        onClose={handleClose}
        MenuListProps={{
          'aria-labelledby': 'export-button',
        }}
        anchorOrigin={{
          vertical: 'bottom',
          horizontal: 'right',
        }}
        transformOrigin={{
          vertical: 'top',
          horizontal: 'right',
        }}
      >
        <MenuItem onClick={() => handleExport('msel')}>
          <ListItemIcon>
            <FontAwesomeIcon icon={faFileExcel} style={{ color: theme.palette.semantic.excel }} />
          </ListItemIcon>
          <ListItemText primary="Export MSEL" secondary="Excel file with injects" />
        </MenuItem>

        <MenuItem onClick={() => handleExport('observations')}>
          <ListItemIcon>
            <FontAwesomeIcon icon={faClipboardList} style={{ color: theme.palette.semantic.excel }} />
          </ListItemIcon>
          <ListItemText primary="Export Observations" secondary="Excel file with observations" />
        </MenuItem>

        <MenuItem onClick={() => handleExport('full')}>
          <ListItemIcon>
            <FontAwesomeIcon icon={faFileArchive} style={{ color: theme.palette.neutral[500] }} />
          </ListItemIcon>
          <ListItemText
            primary="Export Full Package"
            secondary="ZIP with MSEL, Observations & Summary"
          />
        </MenuItem>
      </Menu>

      {error && (
        <Alert severity="error" sx={{ mt: 1 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}
    </>
  )
}
