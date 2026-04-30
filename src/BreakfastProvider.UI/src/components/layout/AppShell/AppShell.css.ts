import { style } from '@vanilla-extract/css';
import { vars } from '@/styles/theme.css';
import { sidebarWidth } from '@/styles/vars.css';

export const shell = style({
  display: 'grid',
  gridTemplateColumns: `${sidebarWidth} 1fr`,
  minHeight: '100vh',
  transition: `grid-template-columns ${vars.transition.normal}`,
});

export const main = style({
  display: 'flex',
  flexDirection: 'column',
  minHeight: '100vh',
  overflow: 'hidden',
});

export const content = style({
  flex: 1,
  padding: vars.space[6],
  overflow: 'auto',
  backgroundColor: vars.color.background,
});
