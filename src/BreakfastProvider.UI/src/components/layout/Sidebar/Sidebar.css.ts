import { style, styleVariants } from '@vanilla-extract/css';
import { vars } from '@/styles/theme.css';

export const sidebar = style({
  display: 'flex',
  flexDirection: 'column',
  backgroundColor: vars.color.neutral900,
  color: vars.color.neutral300,
  height: '100vh',
  position: 'sticky',
  top: 0,
  overflow: 'hidden',
  transition: `width ${vars.transition.normal}`,
});

export const logo = style({
  display: 'flex',
  alignItems: 'center',
  gap: vars.space[3],
  padding: `${vars.space[4]} ${vars.space[4]}`,
  borderBottom: `1px solid ${vars.color.neutral700}`,
});

export const logoIcon = style({
  fontSize: '1.5rem',
  flexShrink: 0,
});

export const logoText = style({
  fontSize: vars.fontSize.lg,
  fontWeight: vars.fontWeight.bold,
  color: vars.color.neutral0,
  whiteSpace: 'nowrap',
});

export const collapseButton = style({
  background: 'none',
  border: 'none',
  color: vars.color.neutral400,
  cursor: 'pointer',
  padding: `${vars.space[2]} ${vars.space[4]}`,
  textAlign: 'left',
  fontSize: vars.fontSize.md,
  ':hover': {
    color: vars.color.neutral0,
  },
});

export const navList = style({
  listStyle: 'none',
  padding: `${vars.space[2]} 0`,
  margin: 0,
  flex: 1,
  overflowY: 'auto',
});

const navItemBase = style({
  display: 'flex',
  alignItems: 'center',
  gap: vars.space[3],
  padding: `${vars.space[2]} ${vars.space[4]}`,
  color: vars.color.neutral400,
  textDecoration: 'none',
  fontSize: vars.fontSize.sm,
  transition: `all ${vars.transition.fast}`,
  borderLeft: '3px solid transparent',
  whiteSpace: 'nowrap',
  ':hover': {
    color: vars.color.neutral0,
    backgroundColor: vars.color.neutral800,
  },
});

export const navItem = navItemBase;

export const navItemActive = style([navItemBase, {
  color: vars.color.neutral0,
  backgroundColor: vars.color.neutral800,
  borderLeftColor: vars.color.primary500,
}]);

export const navIcon = style({
  flexShrink: 0,
  width: '20px',
  textAlign: 'center',
});

export const navLabel = style({
  whiteSpace: 'nowrap',
  overflow: 'hidden',
  textOverflow: 'ellipsis',
});
