import { style } from '@vanilla-extract/css';
import { vars } from '@/styles/theme.css';

export const header = style({
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'space-between',
  padding: `${vars.space[3]} ${vars.space[6]}`,
  backgroundColor: vars.color.neutral0,
  borderBottom: `1px solid ${vars.color.border}`,
  position: 'sticky',
  top: 0,
  zIndex: vars.zIndex.sticky,
});

export const left = style({
  display: 'flex',
  alignItems: 'center',
  gap: vars.space[3],
});

export const title = style({
  fontSize: vars.fontSize.lg,
  fontWeight: vars.fontWeight.semibold,
  color: vars.color.text,
  margin: 0,
});

export const right = style({
  display: 'flex',
  alignItems: 'center',
  gap: vars.space[3],
});

export const themeToggle = style({
  background: 'none',
  border: 'none',
  fontSize: '1.25rem',
  cursor: 'pointer',
  padding: vars.space[2],
  borderRadius: vars.radius.md,
  ':hover': {
    backgroundColor: vars.color.surface,
  },
});
