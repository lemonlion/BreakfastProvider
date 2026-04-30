import { style } from '@vanilla-extract/css';
import { vars } from '@/styles/theme.css';

export const wrapper = style({
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  gap: vars.space[1],
});

const pageButtonBase = style({
  display: 'inline-flex',
  alignItems: 'center',
  justifyContent: 'center',
  minWidth: '36px',
  height: '36px',
  padding: `0 ${vars.space[2]}`,
  fontSize: vars.fontSize.sm,
  fontWeight: vars.fontWeight.medium,
  fontFamily: vars.font.body,
  borderRadius: vars.radius.md,
  border: `1px solid ${vars.color.border}`,
  backgroundColor: vars.color.neutral0,
  color: vars.color.text,
  cursor: 'pointer',
  transition: `all ${vars.transition.fast}`,
  ':hover': {
    backgroundColor: vars.color.surfaceHover,
    borderColor: vars.color.borderHover,
  },
  ':disabled': {
    opacity: 0.5,
    cursor: 'not-allowed',
  },
  ':focus-visible': {
    outline: `2px solid ${vars.color.primary500}`,
    outlineOffset: '2px',
  },
});

export const pageButton = pageButtonBase;

export const pageButtonActive = style([pageButtonBase, {
  backgroundColor: vars.color.primary500,
  color: vars.color.textInverse,
  borderColor: vars.color.primary500,
  ':hover': {
    backgroundColor: vars.color.primary600,
    borderColor: vars.color.primary600,
  },
}]);

export const ellipsis = style({
  display: 'inline-flex',
  alignItems: 'center',
  justifyContent: 'center',
  minWidth: '36px',
  height: '36px',
  fontSize: vars.fontSize.sm,
  color: vars.color.textMuted,
});
