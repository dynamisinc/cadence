import { useEffect, useState } from 'react';
import { appVersion } from '@/config/version';

const LAST_SEEN_VERSION_KEY = 'cadence_last_seen_version';

interface VersionCheckResult {
  /** Whether to show the What's New modal */
  showWhatsNew: boolean;
  /** Current app version */
  currentVersion: string;
  /** Previously seen version (null if first visit) */
  previousVersion: string | null;
  /** Mark current version as seen */
  dismissWhatsNew: () => void;
}

/**
 * Hook to detect version changes and manage What's New modal visibility.
 *
 * @returns Version check state and dismiss function
 *
 * @example
 * const { showWhatsNew, dismissWhatsNew } = useVersionCheck();
 * if (showWhatsNew) {
 *   return <WhatsNewModal onDismiss={dismissWhatsNew} />;
 * }
 */
export function useVersionCheck(): VersionCheckResult {
  const [showWhatsNew, setShowWhatsNew] = useState(false);
  const [previousVersion, setPreviousVersion] = useState<string | null>(null);

  useEffect(() => {
    const lastSeen = localStorage.getItem(LAST_SEEN_VERSION_KEY);
    setPreviousVersion(lastSeen);

    // Show modal only if:
    // 1. User has visited before (lastSeen exists)
    // 2. Version has changed
    if (lastSeen && lastSeen !== appVersion.version) {
      setShowWhatsNew(true);
    }

    // If first visit, store current version (no modal shown)
    if (!lastSeen) {
      localStorage.setItem(LAST_SEEN_VERSION_KEY, appVersion.version);
    }
  }, []);

  const dismissWhatsNew = () => {
    localStorage.setItem(LAST_SEEN_VERSION_KEY, appVersion.version);
    setShowWhatsNew(false);
  };

  return {
    showWhatsNew,
    currentVersion: appVersion.version,
    previousVersion,
    dismissWhatsNew,
  };
}
