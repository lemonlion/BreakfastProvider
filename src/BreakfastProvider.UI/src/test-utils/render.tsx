import { render, type RenderOptions } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider } from '@/providers/theme-provider';
import { ToastProvider } from '@/components/ui/Toast/Toast';
import { type ReactElement } from 'react';

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: Infinity,
      },
    },
  });
}

function AllProviders({ children }: { children: React.ReactNode }) {
  const queryClient = createTestQueryClient();
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <ToastProvider>{children}</ToastProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

const customRender = (ui: ReactElement, options?: Omit<RenderOptions, 'wrapper'>) =>
  render(ui, { wrapper: AllProviders, ...options });

// Re-export everything from Testing Library
export * from '@testing-library/react';
// Override render with custom render
export { customRender as render };
