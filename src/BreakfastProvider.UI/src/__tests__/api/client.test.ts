import { get, post, del, ApiError } from '@/lib/api/client';
import { server } from '@/test-utils/msw/server';
import { http, HttpResponse } from 'msw';

const API = 'http://localhost:3000/api';

describe('API Client', () => {
  describe('get()', () => {
    it('should return JSON data for successful requests', async () => {
      const data = await get<{ items: unknown[] }>('/menu');
      expect(data).toBeDefined();
    });

    it('should include X-Correlation-Id header', async () => {
      let capturedHeaders: Headers | undefined;

      server.use(
        http.get(`${API}/menu`, ({ request }) => {
          capturedHeaders = request.headers;
          return HttpResponse.json({ items: [] });
        }),
      );

      await get('/menu');
      expect(capturedHeaders?.get('x-correlation-id')).toBeDefined();
    });

    it('should throw ApiError for 404 responses', async () => {
      server.use(
        http.get(`${API}/orders/nonexistent`, () => {
          return HttpResponse.json(
            { title: 'Not Found', status: 404, detail: 'Order not found' },
            { status: 404 },
          );
        }),
      );

      await expect(get('/orders/nonexistent')).rejects.toThrow(ApiError);
      await expect(get('/orders/nonexistent')).rejects.toMatchObject({
        status: 404,
      });
    });
  });

  describe('post()', () => {
    it('should send JSON body and return response', async () => {
      const result = await post<{ batchId: string }>('/pancakes', {
        body: { recipeType: 'classic', quantity: 1 },
      });
      expect(result.batchId).toBe('batch-001');
    });

    it('should throw ApiError for validation errors (400)', async () => {
      server.use(
        http.post(`${API}/pancakes`, () => {
          return HttpResponse.json(
            {
              title: 'Validation Error',
              status: 400,
              errors: { quantity: ['Must be positive'] },
            },
            { status: 400 },
          );
        }),
      );

      try {
        await post('/pancakes', { body: { quantity: -1 } });
        fail('Should have thrown');
      } catch (err) {
        expect(err).toBeInstanceOf(ApiError);
        expect((err as ApiError).isValidation).toBe(true);
      }
    });
  });

  describe('del()', () => {
    it('should handle 204 No Content responses', async () => {
      await expect(del('/menu/cache')).resolves.toBeUndefined();
    });
  });
});
