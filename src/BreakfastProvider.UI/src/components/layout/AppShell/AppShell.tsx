'use client';

import { useState, type ReactNode } from 'react';
import { assignInlineVars } from '@vanilla-extract/dynamic';
import { sidebarWidth } from '@/styles/vars.css';
import { Sidebar } from '../Sidebar/Sidebar';
import { Header } from '../Header/Header';
import * as styles from './AppShell.css';

interface AppShellProps {
  children: ReactNode;
}

/**
 * Main application layout: sidebar + header + content area.
 *
 * Learning point: The sidebar width is a CSS variable set via
 * assignInlineVars(). When collapsed, only the variable changes —
 * no React re-renders for child content, pure CSS transition.
 */
export function AppShell({ children }: AppShellProps) {
  const [collapsed, setCollapsed] = useState(false);

  return (
    <div
      className={styles.shell}
      style={assignInlineVars({
        [sidebarWidth]: collapsed ? '64px' : '260px',
      })}
    >
      <Sidebar collapsed={collapsed} onToggle={() => setCollapsed(!collapsed)} />
      <div className={styles.main}>
        <Header />
        <main className={styles.content}>{children}</main>
      </div>
    </div>
  );
}
