import type { Meta, StoryObj } from '@storybook/react';
import { fn } from '@storybook/test';
import { Select } from './Select';

const meta: Meta<typeof Select> = {
  title: 'UI/Select',
  component: Select,
  tags: ['autodocs'],
  argTypes: {
    label: { control: 'text' },
    error: { control: 'text' },
    disabled: { control: 'boolean' },
  },
  args: {
    onChange: fn(),
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

const Options = () => (
  <>
    <option value="">Choose...</option>
    <option value="pancakes">Pancakes</option>
    <option value="waffles">Waffles</option>
    <option value="french-toast">French Toast</option>
  </>
);

export const Default: Story = {
  args: { label: 'Breakfast Item', children: <Options /> },
};

export const WithError: Story = {
  args: { label: 'Breakfast Item', children: <Options />, error: 'Please select an item.' },
};

export const Disabled: Story = {
  args: { label: 'Breakfast Item', children: <Options />, disabled: true },
};