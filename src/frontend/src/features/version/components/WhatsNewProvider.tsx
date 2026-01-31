import { useCallback } from 'react';
import { WhatsNewModal } from './WhatsNewModal';
import { useVersionCheck } from '../hooks/useVersionCheck';

interface WhatsNewProviderProps {
  children: React.ReactNode;
}

/**
 * Provider component that manages What's New modal display.
 * Wrap your app with this to automatically show What's New on version changes.
 *
 * @example
 * <WhatsNewProvider>
 *   <App />
 * </WhatsNewProvider>
 */
export function WhatsNewProvider({ children }: WhatsNewProviderProps) {
  const { showWhatsNew, dismissWhatsNew } = useVersionCheck();

  const handleViewAllNotes = useCallback(() => {
    // Use window.location since we're outside Router context
    window.location.href = '/about';
  }, []);

  return (
    <>
      {children}
      {showWhatsNew && (
        <WhatsNewModal
          open={showWhatsNew}
          onDismiss={dismissWhatsNew}
          onViewAllNotes={handleViewAllNotes}
        />
      )}
    </>
  );
}
