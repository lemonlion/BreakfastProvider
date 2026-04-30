import { render, screen, act } from '@/test-utils/render';
import userEvent from '@testing-library/user-event';
import { ToastProvider, useToast } from '@/components/ui/Toast/Toast';

function TestComponent() {
  const { addToast } = useToast();
  return (
    <button onClick={() => addToast('Test message', 'success', 5000)}>
      Show Toast
    </button>
  );
}

describe('Toast', () => {
  it('should auto-dismiss after duration', async () => {
    jest.useFakeTimers();
    const user = userEvent.setup({ advanceTimers: jest.advanceTimersByTime });

    render(
      <ToastProvider>
        <TestComponent />
      </ToastProvider>,
    );

    await user.click(screen.getByRole('button'));
    expect(screen.getByText('Test message')).toBeInTheDocument();

    act(() => {
      jest.advanceTimersByTime(5000);
    });

    expect(screen.queryByText('Test message')).not.toBeInTheDocument();

    jest.useRealTimers();
  });

  it('should dismiss when close button is clicked', async () => {
    const user = userEvent.setup();

    render(
      <ToastProvider>
        <TestComponent />
      </ToastProvider>,
    );

    await user.click(screen.getByText('Show Toast'));
    expect(screen.getByText('Test message')).toBeInTheDocument();

    await user.click(screen.getByLabelText('Dismiss notification'));
    expect(screen.queryByText('Test message')).not.toBeInTheDocument();
  });
});
