import type { Meta, StoryObj } from '@storybook/react';
import { ProgressBar } from './ProgressBar';

const meta: Meta<typeof ProgressBar> = {
  title: 'UI/ProgressBar',
  component: ProgressBar,
  tags: ['autodocs'],
  argTypes: {
    value: {
      control: { type: 'range', min: 0, max: 100, step: 1 },
      description: 'Current progress value (0-100)',
    },
    variant: {
      control: 'select',
      options: ['primary', 'success', 'warning', 'danger'],
      description: 'Visual variant of the progress bar',
    },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

/** Default progress bar */
export const Default: Story = {
  args: { value: 60 },
};

/** Empty */
export const Empty: Story = {
  args: { value: 0 },
};

/** Complete */
export const Complete: Story = {
  args: { value: 100, variant: 'success' },
};

/** Warning state */
export const Warning: Story = {
  args: { value: 75, variant: 'warning' },
};

/** Danger state */
export const Danger: Story = {
  args: { value: 90, variant: 'error' },
};

/** All variants */
export const AllVariants: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16, maxWidth: 400 }}>
      <ProgressBar value={40} variant="primary" />
      <ProgressBar value={60} variant="success" />
      <ProgressBar value={75} variant="warning" />
      <ProgressBar value={90} variant="error" />
    </div>
  ),
};
