import { server } from '@/test-utils/msw/server';
import { http, HttpResponse } from 'msw';
import { getOrderSummaries } from '@/lib/api/endpoints/reporting';

describe('Reporting API', () => {
  describe('getOrderSummaries()', () => {
    it('should return order summaries via GraphQL', async () => {
      server.use(
        http.post('http://localhost:3000/api/graphql', async () => {
          return HttpResponse.json({
            data: {
              orderSummaries: {
                edges: [
                  { node: { id: 1, orderId: 'o-1', customerName: 'Alice', itemCount: 3, status: 'Completed', createdAt: '2024-01-15T10:00:00Z' }, cursor: 'c1' },
                ],
                pageInfo: { hasNextPage: false, hasPreviousPage: false },
              },
            },
          });
        }),
      );

      const result = await getOrderSummaries(10);
      expect(result.edges).toHaveLength(1);
      expect(result.edges[0].node.orderId).toBe('o-1');
    });
  });
});
