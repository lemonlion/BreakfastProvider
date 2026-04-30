import { createWaffle } from '@/lib/api/endpoints/waffles';
import { server } from '@/test-utils/msw/server';
import { http, HttpResponse } from 'msw';

describe('Waffles API', () => {
  it('should create a waffle batch', async () => {
    const result = await createWaffle({ toppings: ['syrup'] });
    expect(result.batchId).toBeDefined();
  });

  it('should handle server errors', async () => {
    server.use(
      http.post('http://localhost:3000/api/waffles', () => {
        return HttpResponse.json(
          { title: 'Internal Server Error', status: 500 },
          { status: 500 },
        );
      }),
    );

    await expect(createWaffle({ toppings: [] })).rejects.toThrow();
  });
});
