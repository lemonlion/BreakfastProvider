import { style, styleVariants } from '@vanilla-extract/css';
import { vars } from '@/styles/theme.css';
import { slideInRight } from '@/styles/animations.css';
import { toastOffset, toastTimerWidth } from '@/styles/vars.css';

export const container = style({
  position: 'fixed',
  top: vars.space[4],
  right: vars.space[4],
  zIndex: vars.zIndex.toast,
  display: 'flex',
  flexDirection: 'column',
  gap: vars.space[2],
});

const toastBase = style({
  display: 'flex',
  alignItems: 'center',
  gap: vars.space[3],
  padding: `${vars.space[3]} ${vars.space[4]}`,
  borderRadius: vars.radius.md,
  boxShadow: vars.shadow.lg,
  minWidth: '300px',
  maxWidth: '420px',
  position: 'relative',
  overflow: 'hidden',
  animation: `${slideInRight} 0.3s ease-out`,
  transform: `translateY(${toastOffset})`,
});

export const toastVariants = styleVariants({
  success: [toastBase, { backgroundColor: vars.color.successLight, color: vars.color.success, borderLeft: `4px solid ${vars.color.success}` }],
  error: [toastBase, { backgroundColor: vars.color.errorLight, color: vars.color.error, borderLeft: `4px solid ${vars.color.error}` }],
  warning: [toastBase, { backgroundColor: vars.color.warningLight, color: vars.color.warning, borderLeft: `4px solid ${vars.color.warning}` }],
  info: [toastBase, { backgroundColor: vars.color.infoLight, color: vars.color.info, borderLeft: `4px solid ${vars.color.info}` }],
});

export const dismissButton = style({
  background: 'none',
  border: 'none',
  color: 'currentColor',
  cursor: 'pointer',
  padding: vars.space[1],
  marginLeft: 'auto',
  fontSize: vars.fontSize.sm,
  opacity: 0.7,
  ':hover': {
    opacity: 1,
  },
});

export const timerBar = style({
  position: 'absolute',
  bottom: 0,
  left: 0,
  height: '3px',
  backgroundColor: 'currentColor',
  opacity: 0.3,
  width: toastTimerWidth,
  animationName: 'shrinkWidth',
  animationTimingFunction: 'linear',
  animationFillMode: 'forwards',
});
