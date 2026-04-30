import { style } from '@vanilla-extract/css';
import { vars } from '@/styles/theme.css';

export const wrapper = style({
  display: 'flex',
  flexDirection: 'column',
  alignItems: 'center',
  justifyContent: 'center',
  padding: `${vars.space[12]} ${vars.space[6]}`,
  textAlign: 'center',
});

export const icon = style({
  fontSize: '3rem',
  marginBottom: vars.space[4],
});

export const title = style({
  fontSize: vars.fontSize.xl,
  fontWeight: vars.fontWeight.semibold,
  color: vars.color.text,
  marginBottom: vars.space[2],
});

export const description = style({
  fontSize: vars.fontSize.md,
  color: vars.color.textMuted,
  marginBottom: vars.space[6],
  maxWidth: '400px',
});
