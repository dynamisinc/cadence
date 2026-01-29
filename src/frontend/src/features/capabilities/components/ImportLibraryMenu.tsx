/**
 * ImportLibraryMenu Component
 *
 * Dropdown menu for importing predefined capability libraries.
 * Shows available libraries with confirmation dialog before import.
 */

import { useState } from 'react'
import type { FC } from 'react'
import {
  Menu,
  MenuItem,
  ListItemText,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Typography,
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faFileImport, faDownload } from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '@/theme/styledComponents'
import { useAvailableLibraries, useImportLibrary } from '../hooks/useImportLibrary'
import type { PredefinedLibraryInfo } from '../types'

interface ImportLibraryMenuProps {
  /** Organization ID (optional) */
  organizationId?: string
}

/**
 * Menu and dialog for importing predefined capability libraries
 */
export const ImportLibraryMenu: FC<ImportLibraryMenuProps> = ({
  organizationId,
}) => {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)
  const [selectedLibrary, setSelectedLibrary] = useState<PredefinedLibraryInfo | null>(
    null,
  )
  const [showConfirm, setShowConfirm] = useState(false)

  const { data: libraries, isLoading } = useAvailableLibraries(organizationId)
  const importMutation = useImportLibrary(organizationId)

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget)
  }

  const handleMenuClose = () => {
    setAnchorEl(null)
  }

  const handleLibrarySelect = (library: PredefinedLibraryInfo) => {
    setSelectedLibrary(library)
    setShowConfirm(true)
    handleMenuClose()
  }

  const handleConfirmClose = () => {
    setShowConfirm(false)
  }

  const handleImport = async () => {
    if (!selectedLibrary) return

    try {
      await importMutation.mutateAsync(selectedLibrary.id)
      setShowConfirm(false)
    } catch {
      // Error handled by mutation hook
    }
  }

  return (
    <>
      <CobraSecondaryButton
        startIcon={<FontAwesomeIcon icon={faFileImport} />}
        onClick={handleMenuOpen}
        disabled={isLoading}
      >
        Import Library
      </CobraSecondaryButton>

      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        transformOrigin={{ vertical: 'top', horizontal: 'left' }}
      >
        {libraries && libraries.length > 0 ? (
          libraries.map(library => (
            <MenuItem key={library.id} onClick={() => handleLibrarySelect(library)}>
              <ListItemText
                primary={library.name}
                secondary={`${library.capabilityCount} capabilities`}
              />
            </MenuItem>
          ))
        ) : (
          <MenuItem disabled>
            <ListItemText primary="No libraries available" />
          </MenuItem>
        )}
      </Menu>

      <Dialog
        open={showConfirm}
        onClose={handleConfirmClose}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Import {selectedLibrary?.name}?</DialogTitle>
        <DialogContent>
          <Typography variant="body1" paragraph>
            {selectedLibrary?.description}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
            This will add {selectedLibrary?.capabilityCount} capabilities to your
            library. Existing capabilities with the same name will be skipped.
          </Typography>
          {importMutation.isError && (
            <Alert severity="error" sx={{ mt: 2 }}>
              {importMutation.error instanceof Error
                ? importMutation.error.message
                : 'Failed to import library'}
            </Alert>
          )}
        </DialogContent>
        <DialogActions>
          <CobraSecondaryButton
            onClick={handleConfirmClose}
            disabled={importMutation.isPending}
          >
            Cancel
          </CobraSecondaryButton>
          <CobraPrimaryButton
            onClick={handleImport}
            disabled={importMutation.isPending}
            startIcon={<FontAwesomeIcon icon={faDownload} />}
          >
            {importMutation.isPending ? 'Importing...' : 'Import'}
          </CobraPrimaryButton>
        </DialogActions>
      </Dialog>
    </>
  )
}

export default ImportLibraryMenu
