import { style, styleVariants } from '@vanilla-extract/css';
import { vars } from '@/styles/theme.css';
import { fadeIn, scaleIn } from '@/styles/animations.css';

export const backdrop = style({
  position: 'fixed',
  inset: 0,
  backgroundColor: 'rgba(0, 0, 0, 0.5)',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  zIndex: vars.zIndex.modal,
  animation: `${fadeIn} 0.2s ease-out`,
});

const modalBase = style({
  backgroundColor: vars.color.neutral0,
  borderRadius: vars.radius.lg,
  boxShadow: vars.shadow.xl,
  maxHeight: '85vh',
  overflow: 'auto',
  animation: `${scaleIn} 0.2s ease-out`,
});

export const modalVariants = styleVariants({
  sm: [modalBase, { width: '400px' }],
  md: [modalBase, { width: '560px' }],
  lg: [modalBase, { width: '720px' }],
});

export const header = style({
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'space-between',
  padding: `${vars.space[4]} ${vars.space[6]}`,
  borderBottom: `1px solid ${vars.color.border}`,
});

export const title = style({
  fontSize: vars.fontSize.lg,
  fontWeight: vars.fontWeight.semibold,
  color: vars.color.text,
  margin: 0,
});

export const closeButton = style({
  background: 'none',
  border: 'none',
  fontSize: vars.fontSize.lg,
  color: vars.color.textMuted,
  cursor: 'pointer',
  padding: vars.space[1],
  borderRadius: vars.radius.sm,
  ':hover': {
    color: vars.color.text,
    backgroundColor: vars.color.surface,
  },
});

export const body = style({
  padding: vars.space[6],
});
