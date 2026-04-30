import { renderHook, waitFor, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useOrders, useCreateOrder, useUpdateOrderStatus } from '@/hooks/use-orders';
import { server } from '@/test-utils/msw/server';
import { http, HttpResponse } from 'msw';

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return function Wrapper({ children }: { children: React.ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  };
}

describe('useOrders', () => {
  it('should fetch paginated orders', async () => {
    const { result } = renderHook(() => useOrders(1, 10), {
      wrapper: createWrapper(),
    });

    expect(result.current.isLoading).toBe(true);

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.data?.items).toHaveLength(2);
    expect(result.current.data?.totalCount).toBe(2);
  });

  it('should handle network errors gracefully', async () => {
    server.use(
      http.get('http://localhost:3000/api/orders', () => {
        return HttpResponse.error();
      }),
    );

    const { result } = renderHook(() => useOrders(1, 10), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isError).toBe(true));
    expect(result.current.error).toBeDefined();
  });
});

describe('useCreateOrder', () => {
  it('should create order and return new order data', async () => {
    const { result } = renderHook(() => useCreateOrder(), {
      wrapper: createWrapper(),
    });

    await act(async () => {
      await result.current.mutateAsync({
        customerName: 'Test Customer',
        items: [{ quantity: 2 }],
      });
    });

    await waitFor(() => {
      expect(result.current.data).toEqual(
        expect.objectContaining({ orderId: 'new-order-1', status: 'Created' }),
      );
    });
  });
});

describe('useUpdateOrderStatus', () => {
  it('should update order status', async () => {
    const { result } = renderHook(() => useUpdateOrderStatus(), {
      wrapper: createWrapper(),
    });

    await act(async () => {
      await result.current.mutateAsync({
        orderId: 'order-1',
        request: { status: 'Preparing' },
      });
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
  });
});
