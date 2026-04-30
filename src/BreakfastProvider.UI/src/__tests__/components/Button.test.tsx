import { render, screen } from '@/test-utils/render';
import userEvent from '@testing-library/user-event';
import { Button } from '@/components/ui/Button/Button';

// Override the vanilla-extract mock for Button.css specifically.
// The global mock returns Proxy functions which React rejects for style props.
// spinnerStyle is used as a React style prop and must be a plain object.
jest.mock('@/components/ui/Button/Button.css', () => ({
  __esModule: true,
  buttonRecipe: () => 'mock-button-class',
  spinnerStyle: { animation: 'none', width: '1em', height: '1em' },
}));

describe('Button', () => {
  it('should render children', () => {
    render(<Button>Click me</Button>);
    expect(screen.getByRole('button', { name: 'Click me' })).toBeInTheDocument();
  });

  it('should call onClick when clicked', async () => {
    const user = userEvent.setup();
    const onClick = jest.fn();
    render(<Button onClick={onClick}>Click</Button>);

    await user.click(screen.getByRole('button'));
    expect(onClick).toHaveBeenCalledTimes(1);
  });

  it('should not call onClick when disabled', async () => {
    const user = userEvent.setup();
    const onClick = jest.fn();
    render(<Button onClick={onClick} disabled>Disabled</Button>);

    await user.click(screen.getByRole('button'));
    expect(onClick).not.toHaveBeenCalled();
  });

  it('should not call onClick when loading', async () => {
    const user = userEvent.setup();
    const onClick = jest.fn();
    render(<Button onClick={onClick} loading>Loading</Button>);

    const button = screen.getByRole('button');
    await user.click(button);

    expect(onClick).not.toHaveBeenCalled();
    expect(button).toHaveAttribute('aria-busy', 'true');
  });

  it('should forward ref to button element', () => {
    const ref = { current: null as HTMLButtonElement | null };
    render(<Button ref={ref}>Ref Test</Button>);
    expect(ref.current).toBeInstanceOf(HTMLButtonElement);
  });
});
