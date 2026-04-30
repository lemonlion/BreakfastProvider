'use client';

import { useTheme } from '@/providers/theme-provider';
import { useHealth } from '@/hooks/use-health';
import { Badge } from '@/components/ui/Badge/Badge';
import { getStatusColor } from '@/lib/utils';
import * as styles from './Header.css';

/**
 * App header with theme toggle and live health status.
 *
 * Learning point: The health badge auto-updates every 30 seconds
 * (via useHealth's refetchInterval) — no manual refresh needed.
 */
export function Header() {
  const { theme, toggleTheme } = useTheme();
  const { data: health } = useHealth();

  return (
    <header className={styles.header}>
      <div className={styles.left}>
        <h1 className={styles.title}>
          {/* Uses next/font — see app/layout.tsx */}
          BreakfastProvider Dashboard
        </h1>
      </div>

      <div className={styles.right}>
        {/* Health status indicator */}
        {health && (
          <Badge variant={getStatusColor(health.status)} dot>
            {health.status}
          </Badge>
        )}

        {/* Theme toggle */}
        <button
          className={styles.themeToggle}
          onClick={toggleTheme}
          aria-label={`Switch to ${theme === 'light' ? 'dark' : 'light'} theme`}
        >
          {theme === 'light' ? '🌙' : '☀️'}
        </button>
      </div>
    </header>
  );
}
