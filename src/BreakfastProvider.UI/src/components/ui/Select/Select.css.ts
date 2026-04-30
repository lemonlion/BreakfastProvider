import { style, styleVariants } from '@vanilla-extract/css';
import { vars } from '@/styles/theme.css';

export const label = style({
  display: 'block',
  fontSize: vars.fontSize.sm,
  fontWeight: vars.fontWeight.medium,
  color: vars.color.text,
  marginBottom: vars.space[1],
});

const selectBase = style({
  width: '100%',
  padding: `${vars.space[2]} ${vars.space[3]}`,
  fontSize: vars.fontSize.md,
  fontFamily: vars.font.body,
  borderRadius: vars.radius.md,
  border: `1px solid ${vars.color.border}`,
  backgroundColor: vars.color.neutral0,
  color: vars.color.text,
  transition: `all ${vars.transition.fast}`,
  outline: 'none',
  appearance: 'none',
  backgroundImage: `url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 12 12'%3E%3Cpath fill='%236b7280' d='M3 4.5L6 7.5L9 4.5'/%3E%3C/svg%3E")`,
  backgroundRepeat: 'no-repeat',
  backgroundPosition: `right ${vars.space[3]} center`,
  paddingRight: vars.space[8],
  ':focus': {
    borderColor: vars.color.primary500,
    boxShadow: `0 0 0 3px ${vars.color.primary50}`,
  },
});

export const selectVariants = styleVariants({
  default: [selectBase],
  error: [selectBase, { borderColor: vars.color.error, ':focus': { boxShadow: `0 0 0 3px ${vars.color.errorLight}` } }],
});

export const errorText = style({
  fontSize: vars.fontSize.sm,
  color: vars.color.error,
  marginTop: vars.space[1],
});

export const helperText = style({
  fontSize: vars.fontSize.sm,
  color: vars.color.textMuted,
  marginTop: vars.space[1],
});
