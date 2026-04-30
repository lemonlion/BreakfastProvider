import { globalStyle } from '@vanilla-extract/css';
import { vars } from './theme.css';
import { resetLayer, baseLayer } from './layers.css';

globalStyle('*, *::before, *::after', {
  '@layer': { [resetLayer]: { boxSizing: 'border-box', margin: 0, padding: 0 } },
});

globalStyle('html', {
  '@layer': {
    [baseLayer]: {
      fontFamily: vars.font.body,
      fontSize: vars.fontSize.md,
      lineHeight: vars.lineHeight.normal,
      color: vars.color.text,
      backgroundColor: vars.color.background,
      WebkitFontSmoothing: 'antialiased',
      MozOsxFontSmoothing: 'grayscale',
    },
  },
});

globalStyle('body', {
  '@layer': {
    [baseLayer]: {
      minHeight: '100vh',
      overflowX: 'hidden',
    },
  },
});

globalStyle(':focus-visible', {
  '@layer': {
    [baseLayer]: {
      outline: `2px solid ${vars.color.primary500}`,
      outlineOffset: '2px',
    },
  },
});

globalStyle('a', {
  '@layer': {
    [baseLayer]: {
      color: vars.color.primary500,
      textDecoration: 'none',
    },
  },
});

globalStyle('a:hover', {
  '@layer': {
    [baseLayer]: {
      textDecoration: 'underline',
    },
  },
});
