import { style } from '@vanilla-extract/css';
import { vars } from '@/styles/theme.css';

export const wrapper = style({
  display: 'flex',
  flexDirection: 'column',
  gap: vars.space[4],
});

export const toolbar = style({
  display: 'flex',
  alignItems: 'center',
  gap: vars.space[3],
  padding: `${vars.space[2]} 0`,
});

export const globalFilter = style({
  padding: `${vars.space[2]} ${vars.space[3]}`,
  fontSize: vars.fontSize.sm,
  border: `1px solid ${vars.color.border}`,
  borderRadius: vars.radius.md,
  backgroundColor: vars.color.neutral0,
  color: vars.color.text,
  outline: 'none',
  minWidth: '250px',
  ':focus': {
    borderColor: vars.color.primary500,
    boxShadow: `0 0 0 3px ${vars.color.primary50}`,
  },
});

export const columnToggle = style({
  position: 'relative',
  marginLeft: 'auto',
});

export const columnToggleButton = style({
  padding: `${vars.space[2]} ${vars.space[3]}`,
  fontSize: vars.fontSize.sm,
  cursor: 'pointer',
  color: vars.color.textMuted,
  ':hover': {
    color: vars.color.text,
  },
});

export const columnToggleList = style({
  position: 'absolute',
  right: 0,
  top: '100%',
  backgroundColor: vars.color.neutral0,
  border: `1px solid ${vars.color.border}`,
  borderRadius: vars.radius.md,
  boxShadow: vars.shadow.md,
  padding: vars.space[2],
  zIndex: vars.zIndex.dropdown,
  minWidth: '180px',
});

export const columnToggleItem = style({
  display: 'flex',
  alignItems: 'center',
  gap: vars.space[2],
  padding: `${vars.space[1]} ${vars.space[2]}`,
  fontSize: vars.fontSize.sm,
  cursor: 'pointer',
  ':hover': {
    backgroundColor: vars.color.surface,
  },
});

export const tableContainer = style({
  overflowX: 'auto',
  border: `1px solid ${vars.color.border}`,
  borderRadius: vars.radius.lg,
});

export const table = style({
  width: '100%',
  borderCollapse: 'collapse',
  fontSize: vars.fontSize.sm,
});

export const th = style({
  padding: `${vars.space[3]} ${vars.space[4]}`,
  textAlign: 'left',
  fontWeight: vars.fontWeight.semibold,
  color: vars.color.textMuted,
  backgroundColor: vars.color.surface,
  borderBottom: `1px solid ${vars.color.border}`,
  whiteSpace: 'nowrap',
  userSelect: 'none',
});

export const tr = style({
  ':hover': {
    backgroundColor: vars.color.surface,
  },
});

export const td = style({
  padding: `${vars.space[3]} ${vars.space[4]}`,
  borderBottom: `1px solid ${vars.color.border}`,
  color: vars.color.text,
});

export const expandedRow = style({
  padding: vars.space[4],
  backgroundColor: vars.color.surface,
});

export const pagination = style({
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'space-between',
  padding: `${vars.space[3]} 0`,
});

export const pageInfo = style({
  fontSize: vars.fontSize.sm,
  color: vars.color.textMuted,
});

export const pageButtons = style({
  display: 'flex',
  gap: vars.space[2],
});
