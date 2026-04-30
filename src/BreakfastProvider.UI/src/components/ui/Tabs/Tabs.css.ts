import { style } from '@vanilla-extract/css';
import { vars } from '@/styles/theme.css';

export const tabList = style({
  display: 'flex',
  borderBottom: `1px solid ${vars.color.border}`,
  gap: vars.space[1],
});

const tabBase = style({
  padding: `${vars.space[2]} ${vars.space[4]}`,
  fontSize: vars.fontSize.sm,
  fontWeight: vars.fontWeight.medium,
  fontFamily: vars.font.body,
  color: vars.color.textMuted,
  backgroundColor: 'transparent',
  border: 'none',
  borderBottom: '2px solid transparent',
  cursor: 'pointer',
  transition: `all ${vars.transition.fast}`,
  marginBottom: '-1px',
  ':hover': {
    color: vars.color.text,
  },
  ':focus-visible': {
    outline: `2px solid ${vars.color.primary500}`,
    outlineOffset: '-2px',
  },
});

export const tab = tabBase;

export const tabActive = style([tabBase, {
  color: vars.color.primary500,
  borderBottomColor: vars.color.primary500,
}]);

export const tabPanel = style({
  padding: `${vars.space[4]} 0`,
});
