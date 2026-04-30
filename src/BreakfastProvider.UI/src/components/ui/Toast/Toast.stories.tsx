import type { Meta, StoryObj } from '@storybook/react';
import { userEvent, within } from '@storybook/test';
import { ToastProvider, useToast } from './Toast';
import { Button } from '../Button/Button';

function ToastDemo() {
  const { addToast } = useToast();
  return (
    <div style={{ display: 'flex', gap: 8 }}>
      <Button variant="primary" onClick={() => addToast('Success message', 'success')}>Success</Button>
      <Button variant="danger" onClick={() => addToast('Error occurred', 'error')}>Error</Button>
      <Button variant="secondary" onClick={() => addToast('Warning notice', 'warning')}>Warning</Button>
      <Button variant="ghost" onClick={() => addToast('Info message', 'info')}>Info</Button>
    </div>
  );
}

const meta: Meta<typeof ToastProvider> = {
  title: 'UI/Toast',
  component: ToastProvider,
  tags: ['autodocs'],
  decorators: [
    (Story) => (
      <ToastProvider>
        <Story />
      </ToastProvider>
    ),
  ],
  parameters: { layout: 'fullscreen' },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const AllVariants: Story = {
  render: () => <ToastDemo />,
};

/** Multiple toasts stacking */
export const Stacking: Story = {
  render: () => <ToastDemo />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const buttons = canvas.getAllByRole('button');
    // Click all 4 buttons to stack toasts
    for (const button of buttons) {
      await userEvent.click(button);
    }
  },
};
