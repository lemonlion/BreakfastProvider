import { style } from '@vanilla-extract/css';
import { vars } from '@/styles/theme.css';
import { fadeIn } from '@/styles/animations.css';

export const wrapper = style({
  position: 'relative',
  display: 'inline-block',
});

export const tooltip = style({
  position: 'absolute',
  bottom: 'calc(100% + 8px)',
  left: '50%',
  transform: 'translateX(-50%)',
  padding: `${vars.space[1]} ${vars.space[2]}`,
  fontSize: vars.fontSize.xs,
  fontWeight: vars.fontWeight.medium,
  color: vars.color.textInverse,
  backgroundColor: vars.color.neutral800,
  borderRadius: vars.radius.md,
  whiteSpace: 'nowrap',
  pointerEvents: 'none',
  animation: `${fadeIn} ${vars.transition.fast}`,
  zIndex: 50,
  '::after': {
    content: '""',
    position: 'absolute',
    top: '100%',
    left: '50%',
    transform: 'translateX(-50%)',
    borderWidth: '4px',
    borderStyle: 'solid',
    borderColor: `${vars.color.neutral800} transparent transparent transparent`,
  },
});
