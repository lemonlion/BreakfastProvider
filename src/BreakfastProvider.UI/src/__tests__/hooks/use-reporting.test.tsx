import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useOrderSummaries, useIngredientUsage, usePopularRecipes } from '@/hooks/use-reporting';
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

describe('useOrderSummaries', () => {
  it('should fetch and select order summaries', async () => {
    server.use(
      http.post('http://localhost:3000/api/graphql', async () => {
        return HttpResponse.json({
          data: {
            orderSummaries: {
              edges: [
                {
                  node: {
                    id: 1,
                    orderId: 'o-1',
                    customerName: 'Alice',
                    itemCount: 3,
                    status: 'Completed',
                    createdAt: '2024-01-15T10:00:00Z',
                  },
                  cursor: 'c1',
                },
              ],
              pageInfo: { hasNextPage: false, hasPreviousPage: false },
            },
          },
        });
      }),
    );

    const { result } = renderHook(() => useOrderSummaries(10), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    // select() transforms Connection<T> into { items, pageInfo }
    expect(result.current.data?.items).toHaveLength(1);
    expect(result.current.data?.items[0].orderId).toBe('o-1');
  });
});

describe('useIngredientUsage', () => {
  it('should fetch ingredient usage data', async () => {
    server.use(
      http.post('http://localhost:3000/api/graphql', async () => {
        return HttpResponse.json({
          data: {
            ingredientUsage: [
              { ingredient: 'Flour', count: 42 },
              { ingredient: 'Eggs', count: 30 },
            ],
          },
        });
      }),
    );

    const { result } = renderHook(() => useIngredientUsage(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data).toHaveLength(2);
  });
});

describe('usePopularRecipes', () => {
  it('should fetch popular recipes data', async () => {
    server.use(
      http.post('http://localhost:3000/api/graphql', async () => {
        return HttpResponse.json({
          data: {
            popularRecipes: [
              { recipeType: 'Pancake', count: 100 },
            ],
          },
        });
      }),
    );

    const { result } = renderHook(() => usePopularRecipes(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data).toHaveLength(1);
  });
});
