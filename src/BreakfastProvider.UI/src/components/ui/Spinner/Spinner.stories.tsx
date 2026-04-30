import type { Meta, StoryObj } from '@storybook/react';
import { Spinner } from './Spinner';

const meta: Meta<typeof Spinner> = {
  title: 'UI/Spinner',
  component: Spinner,
  tags: ['autodocs'],
  argTypes: {
    size: {
      control: { type: 'range', min: 16, max: 64, step: 4 },
      description: 'Size of the spinner in pixels',
    },
    color: {
      control: 'color',
      description: 'Override the spinner color',
    },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

/** Default spinner */
export const Default: Story = {};

/** Small spinner */
export const Small: Story = {
  args: { size: 16 },
};

/** Large spinner */
export const Large: Story = {
  args: { size: 48 },
};

/** Custom color */
export const CustomColor: Story = {
  args: { size: 32, color: '#f59e0b' },
};

/** All sizes */
export const Sizes: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: 16, alignItems: 'center' }}>
      <Spinner size={16} />
      <Spinner size={24} />
      <Spinner size={32} />
      <Spinner size={48} />
      <Spinner size={64} />
    </div>
  ),
};

/** On dark background */
export const OnDarkBackground: Story = {
  args: { size: 32 },
  parameters: {
    backgrounds: { default: 'dark' },
  },
};
