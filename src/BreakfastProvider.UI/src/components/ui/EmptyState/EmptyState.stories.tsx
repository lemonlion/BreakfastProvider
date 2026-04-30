import type { Meta, StoryObj } from '@storybook/react';
import { EmptyState } from './EmptyState';
import { Button } from '../Button/Button';

const meta: Meta<typeof EmptyState> = {
  title: 'UI/EmptyState',
  component: EmptyState,
  tags: ['autodocs'],
  argTypes: {
    icon: { control: 'text', description: 'Emoji or icon string' },
    title: { control: 'text' },
    description: { control: 'text' },
  },
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

/** Default empty state */
export const Default: Story = {
  args: {
    title: 'No items found',
    description: 'Try adjusting your search or filters.',
  },
};

/** With custom icon */
export const CustomIcon: Story = {
  args: {
    icon: '🥞',
    title: 'No pancakes yet',
    description: 'Create your first pancake order to get started.',
  },
};

/** With action button */
export const WithAction: Story = {
  args: {
    icon: '📋',
    title: 'No orders',
    description: 'Place your first breakfast order.',
    action: <Button variant="primary">Create Order</Button>,
  },
};

/** Error empty state */
export const ErrorState: Story = {
  args: {
    icon: '⚠️',
    title: 'Something went wrong',
    description: 'Failed to load data. Please try again.',
    action: <Button variant="secondary">Retry</Button>,
  },
};
