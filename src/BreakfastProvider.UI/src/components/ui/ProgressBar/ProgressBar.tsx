import { assignInlineVars } from '@vanilla-extract/dynamic';
import { progressWidth } from '@/styles/vars.css';
import * as styles from './ProgressBar.css';

interface ProgressBarProps {
  value: number; // 0-100
  max?: number;
  variant?: 'primary' | 'success' | 'warning' | 'error';
  /** Show percentage text */
  showLabel?: boolean;
}

/**
 * Progress bar using @vanilla-extract/dynamic for runtime width.
 *
 * Learning point: assignInlineVars() sets a CSS custom property
 * (created via createVar()) as an inline style. The .css.ts file
 * references this variable for the bar width, allowing the animation
 * to run entirely in CSS while the value is set in JS.
 */
export function ProgressBar({ value, max = 100, variant = 'primary', showLabel }: ProgressBarProps) {
  const percentage = Math.min(100, Math.max(0, (value / max) * 100));

  return (
    <div
      className={styles.track}
      role="progressbar"
      aria-valuenow={value}
      aria-valuemin={0}
      aria-valuemax={max}
    >
      <div
        className={styles.barVariants[variant]}
        style={assignInlineVars({ [progressWidth]: `${percentage}%` })}
      />
      {showLabel && <span className={styles.label}>{Math.round(percentage)}%</span>}
    </div>
  );
}
