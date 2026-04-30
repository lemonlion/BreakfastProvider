import { style } from '@vanilla-extract/css';
import { shimmer } from '@/styles/animations.css';
import { vars } from '@/styles/theme.css';

/**
 * Skeleton loading placeholder with shimmer animation.
 *
 * Learning point: The shimmer effect uses a gradient background that
 * slides left-to-right via keyframes. The background-size is 200%
 * so the gradient extends beyond the element, creating the sweep.
 */
export const skeleton = style({
  display: 'block',
  borderRadius: vars.radius.md,
  backgroundColor: vars.color.neutral200,
  backgroundImage: `linear-gradient(90deg, ${vars.color.neutral200} 0%, ${vars.color.neutral100} 50%, ${vars.color.neutral200} 100%)`,
  backgroundSize: '200% 100%',
  animation: `${shimmer} 1.5s ease-in-out infinite`,
});
