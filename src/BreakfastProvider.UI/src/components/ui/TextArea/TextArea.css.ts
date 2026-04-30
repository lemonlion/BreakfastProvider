import { style, styleVariants } from '@vanilla-extract/css';
import { vars } from '@/styles/theme.css';

export const label = style({
  display: 'block',
  fontSize: vars.fontSize.sm,
  fontWeight: vars.fontWeight.medium,
  color: vars.color.text,
  marginBottom: vars.space[1],
});

const textareaBase = style({
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
  resize: 'vertical',
  ':focus': {
    borderColor: vars.color.primary500,
    boxShadow: `0 0 0 3px ${vars.color.primary50}`,
  },
  '::placeholder': {
    color: vars.color.neutral400,
  },
});

export const textareaVariants = styleVariants({
  default: [textareaBase],
  error: [textareaBase, { borderColor: vars.color.error, ':focus': { boxShadow: `0 0 0 3px ${vars.color.errorLight}` } }],
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
