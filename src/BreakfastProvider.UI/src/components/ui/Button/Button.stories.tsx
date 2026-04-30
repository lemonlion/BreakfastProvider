import type { Meta, StoryObj } from '@storybook/react';
import { fn, userEvent, within, expect } from '@storybook/test';
import { Button } from './Button';

/**
 * Button component stories.
 *
 * Learning points demonstrated:
 * - Meta: defines the story group, component, default args
 * - StoryObj: typed story objects with args, argTypes
 * - args: passed as props to the component (editable in Controls panel)
 * - argTypes: fine-tune the Controls UI (select, radio, range, etc.)
 * - play: interaction tests that run in the browser
 * - fn(): creates a Storybook action spy (like jest.fn())
 * - decorators: story-level wrappers
 * - parameters: story-level overrides (layout, backgrounds)
 */
const meta: Meta<typeof Button> = {
  title: 'UI/Button',
  component: Button,
  tags: ['autodocs'],
  argTypes: {
    variant: {
      control: 'select',
      options: ['primary', 'secondary', 'danger', 'ghost'],
      description: 'Visual variant of the button',
      table: {
        defaultValue: { summary: 'primary' },
      },
    },
    size: {
      control: 'radio',
      options: ['sm', 'md', 'lg'],
      description: 'Size of the button',
    },
    loading: {
      control: 'boolean',
      description: 'Shows a loading spinner and disables the button',
    },
    disabled: {
      control: 'boolean',
    },
    onClick: { action: 'clicked' },
  },
  args: {
    children: 'Button',
    onClick: fn(), // Storybook action spy
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

/** Default primary button */
export const Primary: Story = {
  args: {
    variant: 'primary',
    children: 'Primary Button',
  },
};

/** Secondary / outlined button */
export const Secondary: Story = {
  args: {
    variant: 'secondary',
    children: 'Secondary Button',
  },
};

/** All variants side by side */
export const AllVariants: Story = {
  render: (args) => (
    <div style={{ display: 'flex', gap: 8 }}>
      <Button {...args} variant="primary">Primary</Button>
      <Button {...args} variant="secondary">Secondary</Button>
      <Button {...args} variant="danger">Danger</Button>
      <Button {...args} variant="ghost">Ghost</Button>
    </div>
  ),
};

/** All sizes */
export const Sizes: Story = {
  render: (args) => (
    <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
      <Button {...args} size="sm">Small</Button>
      <Button {...args} size="md">Medium</Button>
      <Button {...args} size="lg">Large</Button>
    </div>
  ),
};

/** Loading state */
export const Loading: Story = {
  args: {
    loading: true,
    children: 'Saving...',
  },
};

/** Disabled state */
export const Disabled: Story = {
  args: {
    disabled: true,
    children: 'Disabled',
  },
};

/**
 * Interaction test — verifies click handler fires.
 *
 * Learning point: play() functions run after the story renders.
 * They use Testing Library queries (within, userEvent) and
 * Storybook's expect() for assertions. These tests appear in
 * the Interactions panel and run in CI via test-storybook.
 */
export const ClickInteraction: Story = {
  args: {
    children: 'Click Me',
    onClick: fn(),
  },
  play: async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);
    const button = canvas.getByRole('button', { name: 'Click Me' });

    await userEvent.click(button);

    await expect(args.onClick).toHaveBeenCalledTimes(1);
  },
};

/** Test that loading state prevents clicks */
export const LoadingPreventsClick: Story = {
  args: {
    loading: true,
    children: 'Loading',
    onClick: fn(),
  },
  play: async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);
    const button = canvas.getByRole('button');

    await userEvent.click(button);

    await expect(args.onClick).not.toHaveBeenCalled();
    await expect(button).toHaveAttribute('aria-busy', 'true');
  },
};
