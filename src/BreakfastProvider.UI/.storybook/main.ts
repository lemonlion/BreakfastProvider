import type { StorybookConfig } from '@storybook/nextjs';
import { VanillaExtractPlugin } from '@vanilla-extract/webpack-plugin';

/**
 * Storybook configuration for Next.js + vanilla-extract.
 *
 * Learning points:
 * - @storybook/nextjs framework handles App Router, next/font, next/image
 * - webpackFinal adds the VanillaExtractPlugin so .css.ts files compile
 * - autodocs generates documentation from component props + JSDoc
 * - stories glob pattern finds all .stories.tsx co-located with components
 */
const config: StorybookConfig = {
  stories: ['../src/**/*.stories.@(ts|tsx)'],
  addons: [
    '@storybook/addon-essentials', // Controls, Actions, Viewport, Backgrounds, Docs
    '@storybook/addon-a11y',       // Accessibility audit panel
    '@storybook/addon-interactions', // Test + play function debugger
    '@storybook/addon-links',      // Cross-story navigation
  ],
  framework: {
    name: '@storybook/nextjs',
    options: {},
  },
  docs: {
    autodocs: 'tag', // Auto-generate docs for stories tagged 'autodocs'
  },
  webpackFinal: async (config) => {
    config.plugins = config.plugins ?? [];
    config.plugins.push(new VanillaExtractPlugin());
    return config;
  },
  staticDirs: ['../public'],
};

export default config;
