import type { Meta, StoryObj } from '@storybook/react';
import { fn, userEvent, within, expect } from '@storybook/test';
import { TextArea } from './TextArea';

const meta: Meta<typeof TextArea> = {
  title: 'UI/TextArea',
  component: TextArea,
  tags: ['autodocs'],
  argTypes: {
    label: { control: 'text' },
    error: { control: 'text' },
    helperText: { control: 'text' },
    rows: {
      control: { type: 'range', min: 2, max: 12, step: 1 },
      description: 'Number of visible text rows',
    },
    maxLength: {
      control: { type: 'number' },
      description: 'Maximum character length',
    },
    disabled: { control: 'boolean' },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

/** Default text area */
export const Default: Story = {
  args: {
    label: 'Special Instructions',
    placeholder: 'Any dietary requirements or preferences...',
    rows: 4,
  },
};

/** With character limit */
export const WithMaxLength: Story = {
  args: {
    label: 'Notes',
    placeholder: 'Add your notes...',
    rows: 3,
    maxLength: 200,
    helperText: '200 characters max',
  },
};

/** With error */
export const WithError: Story = {
  args: {
    label: 'Description',
    error: 'Description is required.',
    rows: 3,
  },
};

/** Disabled */
export const Disabled: Story = {
  args: {
    label: 'Read Only Notes',
    disabled: true,
    rows: 3,
    placeholder: 'This field is disabled',
  },
};
