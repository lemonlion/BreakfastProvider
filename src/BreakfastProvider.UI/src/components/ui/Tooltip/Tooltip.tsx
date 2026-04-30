'use client';

import { useState, useId, type ReactNode } from 'react';
import * as styles from './Tooltip.css';

interface TooltipProps {
  content: string;
  children: ReactNode;
}

/**
 * Tooltip on hover/focus with accessible markup.
 *
 * Learning point: role="tooltip" + aria-describedby connects the tooltip
 * content to the trigger element for screen readers.
 */
export function Tooltip({ content, children }: TooltipProps) {
  const [visible, setVisible] = useState(false);
  const tooltipId = useId();

  return (
    <div
      className={styles.wrapper}
      onMouseEnter={() => setVisible(true)}
      onMouseLeave={() => setVisible(false)}
      onFocus={() => setVisible(true)}
      onBlur={() => setVisible(false)}
    >
      <div aria-describedby={visible ? tooltipId : undefined}>{children}</div>
      {visible && (
        <div id={tooltipId} role="tooltip" className={styles.tooltip}>
          {content}
        </div>
      )}
    </div>
  );
}
