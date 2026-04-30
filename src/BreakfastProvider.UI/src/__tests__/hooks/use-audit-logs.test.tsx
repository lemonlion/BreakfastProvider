import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useAuditLogs } from '@/hooks/use-audit-logs';

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe('useAuditLogs', () => {
  it('should fetch audit logs when entityType is provided', async () => {
    const { result } = renderHook(() => useAuditLogs('Order', 'order-1'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data).toHaveLength(1);
  });

  it('should not fetch when no filters are provided', () => {
    const { result } = renderHook(() => useAuditLogs(), {
      wrapper: createWrapper(),
    });

    // enabled: !!(entityType || entityId) — should not fetch
    expect(result.current.isLoading).toBe(false);
    expect(result.current.fetchStatus).toBe('idle');
  });
});
