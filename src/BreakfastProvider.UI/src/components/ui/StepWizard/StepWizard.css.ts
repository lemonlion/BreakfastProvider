import { style } from '@vanilla-extract/css';
import { vars } from '@/styles/theme.css';

export const wrapper = style({
  display: 'flex',
  flexDirection: 'column',
  gap: vars.space[6],
});

export const indicators = style({
  display: 'flex',
  alignItems: 'center',
  gap: vars.space[2],
});

const stepBase = style({
  display: 'flex',
  alignItems: 'center',
  gap: vars.space[2],
  padding: `${vars.space[2]} ${vars.space[3]}`,
  borderRadius: vars.radius.md,
  fontSize: vars.fontSize.sm,
  fontWeight: vars.fontWeight.medium,
  transition: `all ${vars.transition.fast}`,
});

export const stepActive = style([stepBase, {
  backgroundColor: vars.color.primary50,
  color: vars.color.primary500,
}]);

export const stepCompleted = style([stepBase, {
  backgroundColor: vars.color.successLight,
  color: vars.color.success,
}]);

export const stepPending = style([stepBase, {
  backgroundColor: vars.color.neutral100,
  color: vars.color.textMuted,
}]);

export const stepNumber = style({
  display: 'inline-flex',
  alignItems: 'center',
  justifyContent: 'center',
  width: '24px',
  height: '24px',
  borderRadius: vars.radius.full,
  backgroundColor: 'currentColor',
  color: vars.color.neutral0,
  fontSize: vars.fontSize.xs,
  fontWeight: vars.fontWeight.bold,
});

export const stepLabel = style({
  whiteSpace: 'nowrap',
});

export const content = style({
  minHeight: '200px',
});

export const actions = style({
  display: 'flex',
  justifyContent: 'space-between',
  gap: vars.space[3],
});
