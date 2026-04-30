import { spin } from '@/styles/animations.css';
import { vars } from '@/styles/theme.css';

interface SpinnerProps {
  size?: number;
  color?: string;
}

/**
 * Loading spinner using the keyframes animation from the design system.
 */
export function Spinner({ size = 24, color }: SpinnerProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      style={{ animation: `${spin} 1s linear infinite` }}
      role="status"
      aria-label="Loading"
    >
      <circle cx="12" cy="12" r="10" stroke={color ?? vars.color.neutral300} strokeWidth="3" />
      <path
        d="M12 2a10 10 0 0 1 10 10"
        stroke={color ?? vars.color.primary500}
        strokeWidth="3"
        strokeLinecap="round"
      />
    </svg>
  );
}
