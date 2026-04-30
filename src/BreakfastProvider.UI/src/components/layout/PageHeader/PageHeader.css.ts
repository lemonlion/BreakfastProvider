import { style } from '@vanilla-extract/css';
import { vars } from '@/styles/theme.css';

export const wrapper = style({
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'space-between',
  marginBottom: vars.space[6],
});

export const title = style({
  fontSize: vars.fontSize['2xl'],
  fontWeight: vars.fontWeight.bold,
  color: vars.color.text,
  margin: 0,
});

export const description = style({
  fontSize: vars.fontSize.md,
  color: vars.color.textMuted,
  marginTop: vars.space[1],
});

export const actions = style({
  display: 'flex',
  alignItems: 'center',
  gap: vars.space[3],
});
