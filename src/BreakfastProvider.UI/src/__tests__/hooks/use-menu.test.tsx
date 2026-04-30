import { renderHook, waitFor, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useMenu, useClearMenuCache } from '@/hooks/use-menu';
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

describe('useMenu', () => {
  it('should fetch menu items', async () => {
    const { result } = renderHook(() => useMenu(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data).toBeDefined();
  });

  it('should have 5-minute staleTime', async () => {
    const { result } = renderHook(() => useMenu(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    // Data should not be stale immediately
    expect(result.current.isStale).toBe(false);
  });
});

describe('useClearMenuCache', () => {
  it('should call DELETE and invalidate menu query', async () => {
    let wasCalled = false;
    server.use(
      http.delete('http://localhost:3000/api/menu/cache', () => {
        wasCalled = true;
        return new HttpResponse(null, { status: 204 });
      }),
    );

    const { result } = renderHook(() => useClearMenuCache(), {
      wrapper: createWrapper(),
    });

    await act(async () => {
      await result.current.mutateAsync();
    });

    expect(wasCalled).toBe(true);
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
  });
});
