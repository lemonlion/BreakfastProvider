'use client';

import { useRef, type ReactNode, type KeyboardEvent } from 'react';
import * as styles from './Tabs.css';

interface Tab {
  id: string;
  label: string;
  content: ReactNode;
}

interface TabsProps {
  tabs: Tab[];
  activeTab: string;
  onTabChange: (tabId: string) => void;
}

/**
 * Accessible tab panel component with keyboard navigation.
 *
 * Learning point: Arrow Left/Right moves focus between tabs (WAI-ARIA
 * Tabs pattern). The active tab is controlled via props for flexibility.
 */
export function Tabs({ tabs, activeTab, onTabChange }: TabsProps) {
  const tabRefs = useRef<(HTMLButtonElement | null)[]>([]);

  const handleKeyDown = (e: KeyboardEvent, index: number) => {
    let nextIndex: number | null = null;

    if (e.key === 'ArrowRight') {
      nextIndex = (index + 1) % tabs.length;
    } else if (e.key === 'ArrowLeft') {
      nextIndex = (index - 1 + tabs.length) % tabs.length;
    } else if (e.key === 'Home') {
      nextIndex = 0;
    } else if (e.key === 'End') {
      nextIndex = tabs.length - 1;
    }

    if (nextIndex !== null) {
      e.preventDefault();
      tabRefs.current[nextIndex]?.focus();
      onTabChange(tabs[nextIndex].id);
    }
  };

  const activePanel = tabs.find((t) => t.id === activeTab);

  return (
    <div>
      <div className={styles.tabList} role="tablist">
        {tabs.map((tab, index) => (
          <button
            key={tab.id}
            ref={(el) => { tabRefs.current[index] = el; }}
            role="tab"
            id={`tab-${tab.id}`}
            aria-selected={activeTab === tab.id}
            aria-controls={`panel-${tab.id}`}
            tabIndex={activeTab === tab.id ? 0 : -1}
            className={activeTab === tab.id ? styles.tabActive : styles.tab}
            onClick={() => onTabChange(tab.id)}
            onKeyDown={(e) => handleKeyDown(e, index)}
          >
            {tab.label}
          </button>
        ))}
      </div>
      {activePanel && (
        <div
          role="tabpanel"
          id={`panel-${activePanel.id}`}
          aria-labelledby={`tab-${activePanel.id}`}
          className={styles.tabPanel}
        >
          {activePanel.content}
        </div>
      )}
    </div>
  );
}
