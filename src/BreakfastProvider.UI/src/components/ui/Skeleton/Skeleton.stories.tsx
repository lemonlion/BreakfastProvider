import type { Meta, StoryObj } from '@storybook/react';
import { Skeleton } from './Skeleton';

const meta: Meta<typeof Skeleton> = {
  title: 'UI/Skeleton',
  component: Skeleton,
  tags: ['autodocs'],
  argTypes: {
    width: {
      control: 'text',
      description: 'Width of the skeleton (string or number)',
    },
    height: {
      control: { type: 'range', min: 8, max: 200, step: 4 },
      description: 'Height of the skeleton in pixels',
    },
    borderRadius: {
      control: 'text',
      description: 'Border radius (e.g. "50%", "8px")',
    },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

/** Default full-width skeleton line */
export const Default: Story = {};

/** Text line skeleton */
export const TextLine: Story = {
  args: { width: '60%', height: 16 },
};

/** Avatar circle */
export const Avatar: Story = {
  args: { width: 48, height: 48, borderRadius: '50%' },
};

/** Card placeholder */
export const CardPlaceholder: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12, maxWidth: 300 }}>
      <Skeleton height={160} borderRadius="8px" />
      <Skeleton width="70%" height={20} />
      <Skeleton width="90%" height={14} />
      <Skeleton width="40%" height={14} />
    </div>
  ),
};

/** Multiple text lines */
export const TextBlock: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 8, maxWidth: 400 }}>
      <Skeleton height={16} />
      <Skeleton height={16} />
      <Skeleton height={16} width="80%" />
    </div>
  ),
};
