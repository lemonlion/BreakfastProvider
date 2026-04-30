import { addons } from '@storybook/manager-api';
import { create } from '@storybook/theming/create';

/**
 * Custom Storybook manager theme.
 *
 * Learning point: The manager theme customises the Storybook UI itself
 * (sidebar, toolbar), not the preview iframe where stories render.
 */
addons.setConfig({
  theme: create({
    base: 'light',
    brandTitle: 'BreakfastProvider UI',
    brandUrl: 'http://localhost:3000',
    colorPrimary: '#f59e0b',
    colorSecondary: '#3b82f6',
  }),
});
