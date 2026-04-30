import { getDailySpecials, orderDailySpecial } from '@/lib/api/endpoints/daily-specials';
import { server } from '@/test-utils/msw/server';
import { http, HttpResponse } from 'msw';

describe('Daily Specials API', () => {
  describe('getDailySpecials()', () => {
    it('should return list of daily specials', async () => {
      const result = await getDailySpecials();
      expect(result).toHaveLength(1);
      expect(result[0].name).toBe('Sunrise Stack');
    });
  });

  describe('orderDailySpecial()', () => {
    it('should create an order with idempotency key', async () => {
      let capturedHeaders: Headers | undefined;

      server.use(
        http.post('http://localhost:3000/api/daily-specials/orders', ({ request }) => {
          capturedHeaders = request.headers;
          return HttpResponse.json({ orderId: 'ds-order-1' }, { status: 201 });
        }),
      );

      await orderDailySpecial({ specialId: 'ds-1', quantity: 1 }, 'test-idempotency-key');
      expect(capturedHeaders?.get('idempotency-key')).toBe('test-idempotency-key');
    });

    it('should handle 409 Conflict (sold out)', async () => {
      server.use(
        http.post('http://localhost:3000/api/daily-specials/orders', () => {
          return HttpResponse.json(
            { title: 'Conflict', status: 409, detail: 'Sold out' },
            { status: 409 },
          );
        }),
      );

      await expect(
        orderDailySpecial({ specialId: 'ds-1', quantity: 1 }),
      ).rejects.toThrow();
    });
  });
});
