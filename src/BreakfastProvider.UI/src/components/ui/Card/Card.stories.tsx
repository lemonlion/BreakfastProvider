import type { Meta, StoryObj } from '@storybook/react';
import { Card } from './Card';

const meta: Meta<typeof Card> = {
  title: 'UI/Card',
  component: Card,
  tags: ['autodocs'],
  argTypes: {
    variant: {
      control: 'select',
      options: ['elevated', 'outlined', 'flat'],
    },
  },
  /**
   * Story-level decorator — adds padding around the card.
   *
   * Learning point: Decorators at the meta level apply to ALL stories
   * in this file. Story-level decorators apply to one story only.
   */
  decorators: [
    (Story) => (
      <div style={{ maxWidth: 400 }}>
        <Story />
      </div>
    ),
  ],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Elevated: Story = {
  args: {
    variant: 'elevated',
    children: <div><h3>Elevated Card</h3><p>With shadow that grows on hover.</p></div>,
  },
};

export const Outlined: Story = {
  args: {
    variant: 'outlined',
    children: <div><h3>Outlined Card</h3><p>With border that darkens on hover.</p></div>,
  },
};

export const Flat: Story = {
  args: {
    variant: 'flat',
    children: <div><h3>Flat Card</h3><p>Subtle background, no shadow.</p></div>,
  },
};
