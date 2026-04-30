import { style } from '@vanilla-extract/css';
import { vars } from '@/styles/theme.css';

export const container = style({
  width: '100%',
  borderRadius: vars.radius.md,
  border: `1px solid ${vars.color.border}`,
  backgroundColor: vars.color.neutral0,
});

export const item = style({
  width: '100%',
});
