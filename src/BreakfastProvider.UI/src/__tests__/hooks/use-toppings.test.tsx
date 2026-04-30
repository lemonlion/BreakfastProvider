import { renderHook, waitFor, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useToppings, useCreateTopping, useDeleteTopping } from '@/hooks/use-toppings';
import { server } from '@/test-utils/msw/server';
import { http, HttpResponse } from 'msw';

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe('useToppings', () => {
  it('should fetch toppings list', async () => {
    const { result } = renderHook(() => useToppings(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data).toHaveLength(2);
  });
});

describe('useCreateTopping', () => {
  it('should create a topping', async () => {
    const { result } = renderHook(() => useCreateTopping(), {
      wrapper: createWrapper(),
    });

    await act(async () => {
      await result.current.mutateAsync({ name: 'Strawberries', category: 'Fruit' });
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
  });
});

describe('useDeleteTopping', () => {
  it('should optimistically remove topping from cache', async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });

    // Pre-populate the cache
    queryClient.setQueryData(['toppings'], [
      { toppingId: 'top-1', name: 'Chocolate Chips', category: 'Sweet' },
      { toppingId: 'top-2', name: 'Bacon Bits', category: 'Savoury' },
    ]);

    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    const { result } = renderHook(() => useDeleteTopping(), { wrapper });

    await act(async () => {
      await result.current.mutateAsync('top-1');
    });

    // After optimistic update, topping should be removed from cache
    const cached = queryClient.getQueryData<{ toppingId: string }[]>(['toppings']);
    expect(cached?.find((t) => t.toppingId === 'top-1')).toBeUndefined();
  });
});
