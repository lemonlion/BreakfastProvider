import { defineProperties, createSprinkles } from '@vanilla-extract/sprinkles';
import { vars } from './theme.css';

const responsiveProperties = defineProperties({
  conditions: {
    mobile: {},
    tablet: { '@media': 'screen and (min-width: 768px)' },
    desktop: { '@media': 'screen and (min-width: 1024px)' },
    wide: { '@media': 'screen and (min-width: 1280px)' },
  },
  defaultCondition: 'mobile',
  properties: {
    display: ['none', 'flex', 'grid', 'block', 'inline-flex', 'inline-block', 'contents'],
    flexDirection: ['row', 'column', 'row-reverse', 'column-reverse'],
    flexWrap: ['wrap', 'nowrap'],
    alignItems: ['stretch', 'flex-start', 'center', 'flex-end', 'baseline'],
    justifyContent: ['flex-start', 'center', 'flex-end', 'space-between', 'space-around', 'space-evenly'],
    gap: vars.space,

    gridTemplateColumns: {
      '1': 'repeat(1, minmax(0, 1fr))',
      '2': 'repeat(2, minmax(0, 1fr))',
      '3': 'repeat(3, minmax(0, 1fr))',
      '4': 'repeat(4, minmax(0, 1fr))',
    },

    paddingTop: vars.space,
    paddingBottom: vars.space,
    paddingLeft: vars.space,
    paddingRight: vars.space,
    marginTop: vars.space,
    marginBottom: vars.space,
    marginLeft: vars.space,
    marginRight: vars.space,

    width: ['100%', 'auto', 'fit-content'],
    maxWidth: ['none', '100%', '640px', '768px', '1024px', '1280px'],

    fontSize: vars.fontSize,
    fontWeight: vars.fontWeight,
    lineHeight: vars.lineHeight,
    textAlign: ['left', 'center', 'right'],

    position: ['relative', 'absolute', 'fixed', 'sticky'],
    overflow: ['hidden', 'auto', 'visible', 'scroll'],
  },
  shorthands: {
    padding: ['paddingTop', 'paddingBottom', 'paddingLeft', 'paddingRight'],
    paddingX: ['paddingLeft', 'paddingRight'],
    paddingY: ['paddingTop', 'paddingBottom'],
    margin: ['marginTop', 'marginBottom', 'marginLeft', 'marginRight'],
    marginX: ['marginLeft', 'marginRight'],
    marginY: ['marginTop', 'marginBottom'],
  },
});

const colorProperties = defineProperties({
  properties: {
    color: vars.color,
    backgroundColor: vars.color,
    borderColor: vars.color,
    borderRadius: vars.radius,
  },
});

export const sprinkles = createSprinkles(responsiveProperties, colorProperties);

export type Sprinkles = Parameters<typeof sprinkles>[0];
