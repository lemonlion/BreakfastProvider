import type { Meta, StoryObj } from '@storybook/react';
import { Tooltip } from './Tooltip';
import { Button } from '../Button/Button';

const meta: Meta<typeof Tooltip> = {
  title: 'UI/Tooltip',
  component: Tooltip,
  tags: ['autodocs'],
  argTypes: {
    content: { control: 'text', description: 'Tooltip text content' },
  },
  decorators: [(Story) => <div style={{ padding: 80 }}><Story /></div>],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: { content: 'Extra maple syrup on the side', children: <Button>Hover me</Button> },
};

export const LongContent: Story = {
  args: {
    content: 'This is a really long tooltip that gives a detailed description of the breakfast item.',
    children: <Button>Long tooltip</Button>,
  },
};