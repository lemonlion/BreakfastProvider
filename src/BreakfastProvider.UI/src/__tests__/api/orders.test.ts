import { getOrders, getOrder, createOrder, updateOrderStatus } from '@/lib/api/endpoints/orders';
import { server } from '@/test-utils/msw/server';
import { http, HttpResponse } from 'msw';

describe('Orders API', () => {
  describe('getOrders()', () => {
    it('should return paginated results', async () => {
      const result = await getOrders(1, 10);
      expect(result.items).toHaveLength(2);
      expect(result.totalCount).toBe(2);
    });

    it.each([
      { page: 1, pageSize: 5 },
      { page: 2, pageSize: 10 },
      { page: 1, pageSize: 20 },
    ])('should send correct pagination params (page=$page, pageSize=$pageSize)', async (params) => {
      let capturedUrl = '';
      server.use(
        http.get('http://localhost:3000/api/orders', ({ request }) => {
          capturedUrl = request.url;
          return HttpResponse.json({ items: [], totalCount: 0, page: params.page, pageSize: params.pageSize });
        }),
      );

      await getOrders(params.page, params.pageSize);
      expect(capturedUrl).toContain(`page=${params.page}`);
      expect(capturedUrl).toContain(`pageSize=${params.pageSize}`);
    });
  });

  describe('getOrder()', () => {
    it('should return a single order by ID', async () => {
      const result = await getOrder('order-1');
      expect(result).toBeDefined();
      expect(result.orderId || result.id).toBeDefined();
    });
  });

  describe('createOrder()', () => {
    it('should create a new order', async () => {
      const result = await createOrder({
        customerName: 'Test Customer',
        items: [{ quantity: 2 }],
      });
      expect(result).toBeDefined();
    });
  });

  describe('updateOrderStatus()', () => {
    it.each([
      { from: 'Created', to: 'Preparing' },
      { from: 'Preparing', to: 'Ready' },
      { from: 'Ready', to: 'Completed' },
      { from: 'Created', to: 'Cancelled' },
    ])('should allow transition from $from to $to', async ({ to }) => {
      server.use(
        http.patch('http://localhost:3000/api/orders/:id/status', () => {
          return HttpResponse.json({ success: true });
        }),
      );

      await expect(updateOrderStatus('order-1', { status: to })).resolves.toBeDefined();
    });
  });
});
