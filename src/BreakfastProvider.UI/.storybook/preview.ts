import type { Preview } from '@storybook/react';
import { ThemeProvider } from '../src/providers/theme-provider';
import '../src/styles/global.css';

/**
 * Global decorators and parameters applied to ALL stories.
 *
 * Learning points:
 * - Decorators wrap every story — here with ThemeProvider
 * - Parameters set global defaults (backgrounds, viewports, layout)
 * - argTypes define controls shared by all stories
 */
const preview: Preview = {
  decorators: [
    // Wrap every story in ThemeProvider for light/dark support
    (Story) => (
      <ThemeProvider>
        <div style={{ padding: 16 }}>
          <Story />
        </div>
      </ThemeProvider>
    ),
  ],
  parameters: {
    actions: { argTypesRegex: '^on[A-Z].*' }, // Auto-detect event handlers
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
    layout: 'centered',
    backgrounds: {
      default: 'light',
      values: [
        { name: 'light', value: '#ffffff' },
        { name: 'dark', value: '#1a1a2e' },
        { name: 'neutral', value: '#f5f5f5' },
      ],
    },
    viewport: {
      viewports: {
        mobile: { name: 'Mobile', styles: { width: '375px', height: '812px' } },
        tablet: { name: 'Tablet', styles: { width: '768px', height: '1024px' } },
        desktop: { name: 'Desktop', styles: { width: '1440px', height: '900px' } },
      },
    },
  },
  tags: ['autodocs'], // Enable autodocs for all stories by default
};

export default preview;
