import { render, screen } from '@/test-utils/render';
import userEvent from '@testing-library/user-event';
import { Modal } from '@/components/ui/Modal/Modal';

describe('Modal', () => {
  it('should render when isOpen is true', () => {
    render(
      <Modal isOpen onClose={jest.fn()} title="Test Modal">
        <p>Modal content</p>
      </Modal>,
    );

    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByText('Test Modal')).toBeInTheDocument();
    expect(screen.getByText('Modal content')).toBeInTheDocument();
  });

  it('should not render when isOpen is false', () => {
    render(
      <Modal isOpen={false} onClose={jest.fn()} title="Hidden Modal">
        <p>Hidden</p>
      </Modal>,
    );

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('should call onClose when Escape key is pressed', async () => {
    const user = userEvent.setup();
    const onClose = jest.fn();

    render(
      <Modal isOpen onClose={onClose} title="Escape Test">
        <p>Content</p>
      </Modal>,
    );

    await user.keyboard('{Escape}');
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('should call onClose when close button is clicked', async () => {
    const user = userEvent.setup();
    const onClose = jest.fn();

    render(
      <Modal isOpen onClose={onClose} title="Close Test">
        <p>Content</p>
      </Modal>,
    );

    await user.click(screen.getByLabelText('Close'));
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('should call onClose when backdrop is clicked', async () => {
    const user = userEvent.setup();
    const onClose = jest.fn();

    render(
      <Modal isOpen onClose={onClose} title="Backdrop Test">
        <p>Content</p>
      </Modal>,
    );

    // Click the backdrop (presentation role)
    await user.click(screen.getByRole('presentation'));
    expect(onClose).toHaveBeenCalled();
  });

  it('should not close when clicking inside modal body', async () => {
    const user = userEvent.setup();
    const onClose = jest.fn();

    render(
      <Modal isOpen onClose={onClose} title="Body Click Test">
        <p>Click me</p>
      </Modal>,
    );

    await user.click(screen.getByText('Click me'));
    expect(onClose).not.toHaveBeenCalled();
  });
});
