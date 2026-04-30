import { render, screen, waitFor } from '@/test-utils/render';
import userEvent from '@testing-library/user-event';
import OrdersPage from '@/app/orders/page';

// Mock Next.js navigation hooks
jest.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams('page=1&pageSize=10'),
  useRouter: () => ({ push: jest.fn() }),
  usePathname: () => '/orders',
}));

describe('Orders Page', () => {
  it('should render the page header', async () => {
    render(<OrdersPage />);
    expect(screen.getByText('Orders')).toBeInTheDocument();
  });

  it('should load and display orders', async () => {
    render(<OrdersPage />);

    await waitFor(() => {
      expect(screen.getByText(/order-1/)).toBeInTheDocument();
    });
  });

  it('should show New Order button', () => {
    render(<OrdersPage />);
    expect(screen.getByText('New Order')).toBeInTheDocument();
  });
});
