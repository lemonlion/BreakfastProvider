import { keyframes } from '@vanilla-extract/css';

export const spin = keyframes({
  from: { transform: 'rotate(0deg)' },
  to: { transform: 'rotate(360deg)' },
});

export const shimmer = keyframes({
  '0%': { backgroundPosition: '-200% 0' },
  '100%': { backgroundPosition: '200% 0' },
});

export const slideInRight = keyframes({
  from: { transform: 'translateX(100%)', opacity: '0' },
  to: { transform: 'translateX(0)', opacity: '1' },
});

export const slideOutRight = keyframes({
  from: { transform: 'translateX(0)', opacity: '1' },
  to: { transform: 'translateX(100%)', opacity: '0' },
});

export const fadeIn = keyframes({
  from: { opacity: '0' },
  to: { opacity: '1' },
});

export const fadeOut = keyframes({
  from: { opacity: '1' },
  to: { opacity: '0' },
});

export const scaleIn = keyframes({
  from: { transform: 'scale(0.95)', opacity: '0' },
  to: { transform: 'scale(1)', opacity: '1' },
});

export const fillWidth = keyframes({
  from: { width: '0%' },
  to: { width: 'var(--progress-width)' },
});

export const pulse = keyframes({
  '0%, 100%': { opacity: '1' },
  '50%': { opacity: '0.5' },
});
