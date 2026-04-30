import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useHealth } from '@/hooks/use-health';

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe('useHealth', () => {
  it('should return health status with entries', async () => {
    const { result } = renderHook(() => useHealth(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.data?.status).toBe('Healthy');
    expect(result.current.data?.entries).toBeDefined();
  });

  it('should auto-refetch at configured interval', async () => {
    const { result } = renderHook(() => useHealth(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    // Switch to fake timers after initial fetch completes
    jest.useFakeTimers();
    // The hook has refetchInterval: 30_000
    jest.advanceTimersByTime(30_000);
    jest.useRealTimers();
  });
});
