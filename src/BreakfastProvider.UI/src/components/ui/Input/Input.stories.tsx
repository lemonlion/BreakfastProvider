import type { Meta, StoryObj } from '@storybook/react';
import { fn, userEvent, within, expect } from '@storybook/test';
import { Input } from './Input';

const meta: Meta<typeof Input> = {
  title: 'UI/Input',
  component: Input,
  tags: ['autodocs'],
  argTypes: {
    label: { control: 'text' },
    error: { control: 'text' },
    helperText: { control: 'text' },
    disabled: { control: 'boolean' },
    placeholder: { control: 'text' },
    type: {
      control: 'select',
      options: ['text', 'email', 'password', 'number', 'search'],
    },
  },
  args: {
    label: 'Label',
    placeholder: 'Enter text...',
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

/** Default input with label */
export const Default: Story = {
  args: {
    label: 'Username',
    placeholder: 'Enter your username',
  },
};

/** Input with helper text */
export const WithHelperText: Story = {
  args: {
    label: 'Email',
    helperText: 'We will never share your email.',
    placeholder: 'you@example.com',
    type: 'email',
  },
};

/** Input with error state */
export const WithError: Story = {
  args: {
    label: 'Email',
    error: 'Please enter a valid email address.',
    placeholder: 'you@example.com',
    type: 'email',
  },
};

/** Disabled input */
export const Disabled: Story = {
  args: {
    label: 'Disabled Input',
    disabled: true,
    placeholder: 'Cannot type here',
  },
};

/** Focus interaction test */
export const FocusInteraction: Story = {
  args: {
    label: 'Focus Me',
    placeholder: 'Click or tab here',
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const input = canvas.getByLabelText('Focus Me');

    await userEvent.click(input);
    await expect(input).toHaveFocus();

    await userEvent.type(input, 'Hello World');
    await expect(input).toHaveValue('Hello World');
  },
};
