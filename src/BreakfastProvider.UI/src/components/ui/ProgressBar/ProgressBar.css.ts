import { style, styleVariants } from '@vanilla-extract/css';
import { vars } from '@/styles/theme.css';
import { progressWidth } from '@/styles/vars.css';

export const track = style({
  position: 'relative',
  width: '100%',
  height: '8px',
  borderRadius: vars.radius.full,
  backgroundColor: vars.color.neutral200,
  overflow: 'hidden',
});

const barBase = style({
  height: '100%',
  borderRadius: vars.radius.full,
  transition: `width ${vars.transition.normal}`,
  width: progressWidth,
});

export const barVariants = styleVariants({
  primary: [barBase, { backgroundColor: vars.color.primary500 }],
  success: [barBase, { backgroundColor: vars.color.success }],
  warning: [barBase, { backgroundColor: vars.color.warning }],
  error: [barBase, { backgroundColor: vars.color.error }],
});

export const label = style({
  position: 'absolute',
  right: 0,
  top: '-20px',
  fontSize: vars.fontSize.xs,
  fontWeight: vars.fontWeight.medium,
  color: vars.color.textMuted,
});
