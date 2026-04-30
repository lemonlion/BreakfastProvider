import { createPancake } from '@/lib/api/endpoints/pancakes';
import { server } from '@/test-utils/msw/server';
import { http, HttpResponse } from 'msw';

describe('Pancakes API', () => {
  it('should create a pancake batch', async () => {
    const result = await createPancake({ toppings: ['chocolate'] });
    expect(result.batchId).toBe('batch-001');
    expect(result.ingredients).toBeDefined();
  });

  it('should handle validation errors', async () => {
    server.use(
      http.post('http://localhost:3000/api/pancakes', () => {
        return HttpResponse.json(
          { title: 'Validation Error', status: 400 },
          { status: 400 },
        );
      }),
    );

    await expect(createPancake({ toppings: [] })).rejects.toThrow();
  });
});
