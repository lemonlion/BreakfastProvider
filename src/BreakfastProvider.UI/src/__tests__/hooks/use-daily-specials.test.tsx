import { renderHook, waitFor, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useDailySpecials, useOrderDailySpecial } from '@/hooks/use-daily-specials';

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe('useDailySpecials', () => {
  it('should fetch daily specials', async () => {
    const { result } = renderHook(() => useDailySpecials(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data).toHaveLength(1);
  });
});

describe('useOrderDailySpecial', () => {
  it('should create a daily special order', async () => {
    const { result } = renderHook(() => useOrderDailySpecial(), {
      wrapper: createWrapper(),
    });

    await act(async () => {
      await result.current.mutateAsync({
        request: { specialId: 'ds-1', quantity: 1 },
        idempotencyKey: 'test-key',
      });
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
  });
});
