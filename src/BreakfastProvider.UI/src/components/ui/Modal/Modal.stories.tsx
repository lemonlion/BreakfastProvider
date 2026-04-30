import type { Meta, StoryObj } from '@storybook/react';
import { fn, userEvent, expect } from '@storybook/test';
import { useState } from 'react';
import { Modal } from './Modal';
import { Button } from '../Button/Button';

const meta: Meta<typeof Modal> = {
  title: 'UI/Modal',
  component: Modal,
  tags: ['autodocs'],
  /**
   * Learning point: Parameters can override layout for specific stories.
   * Modals need 'fullscreen' layout since they use createPortal on body.
   */
  parameters: { layout: 'fullscreen' },
};

export default meta;
type Story = StoryObj<typeof meta>;

/** Render function story — manages open state internally */
export const Interactive: Story = {
  render: () => {
    const [isOpen, setIsOpen] = useState(false);
    return (
      <div style={{ padding: 32 }}>
        <Button onClick={() => setIsOpen(true)}>Open Modal</Button>
        <Modal isOpen={isOpen} onClose={() => setIsOpen(false)} title="Example Modal" size="md">
          <p>This is modal content. Press Escape or click the backdrop to close.</p>
          <Button onClick={() => setIsOpen(false)}>Close</Button>
        </Modal>
      </div>
    );
  },
};

/** All sizes */
export const Sizes: Story = {
  render: () => {
    const [size, setSize] = useState<'sm' | 'md' | 'lg' | null>(null);
    return (
      <div style={{ padding: 32, display: 'flex', gap: 8 }}>
        <Button onClick={() => setSize('sm')}>Small</Button>
        <Button onClick={() => setSize('md')}>Medium</Button>
        <Button onClick={() => setSize('lg')}>Large</Button>
        {size && (
          <Modal isOpen={true} onClose={() => setSize(null)} title={`${size} Modal`} size={size}>
            <p>Size: {size}</p>
          </Modal>
        )}
      </div>
    );
  },
};

/** Escape key closes modal */
export const EscapeToClose: Story = {
  args: {
    isOpen: true,
    title: 'Press Escape',
    onClose: fn(),
    children: <p>Press Escape to close</p>,
  },
  play: async ({ args }) => {
    await userEvent.keyboard('{Escape}');
    await expect(args.onClose).toHaveBeenCalled();
  },
};
