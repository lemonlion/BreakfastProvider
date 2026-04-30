import type { Meta, StoryObj } from '@storybook/react';
import { Badge } from './Badge';

const meta: Meta<typeof Badge> = {
  title: 'UI/Badge',
  component: Badge,
  tags: ['autodocs'],
  argTypes: {
    variant: {
      control: 'select',
      options: ['success', 'warning', 'error', 'info', 'neutral'],
    },
    dot: { control: 'boolean' },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Success: Story = { args: { variant: 'success', children: 'Healthy' } };
export const Warning: Story = { args: { variant: 'warning', children: 'Degraded' } };
export const Error: Story = { args: { variant: 'error', children: 'Unhealthy' } };
export const Info: Story = { args: { variant: 'info', children: 'Processing' } };

/** With dot indicator */
export const WithDot: Story = {
  args: { variant: 'success', children: 'Online', dot: true },
};

/** All variants gallery */
export const AllVariants: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: 8 }}>
      <Badge variant="success" dot>Healthy</Badge>
      <Badge variant="warning" dot>Degraded</Badge>
      <Badge variant="error" dot>Unhealthy</Badge>
      <Badge variant="info">Info</Badge>
      <Badge variant="neutral">Neutral</Badge>
    </div>
  ),
};
