import { getMenu, clearMenuCache } from '@/lib/api/endpoints/menu';
import { server } from '@/test-utils/msw/server';
import { http, HttpResponse } from 'msw';

describe('Menu API', () => {
  describe('getMenu()', () => {
    it('should return menu items', async () => {
      const result = await getMenu();
      expect(result).toBeDefined();
    });
  });

  describe('clearMenuCache()', () => {
    it('should handle 204 No Content response', async () => {
      await expect(clearMenuCache()).resolves.toBeUndefined();
    });

    it('should handle server errors', async () => {
      server.use(
        http.delete('http://localhost:3000/api/menu/cache', () => {
          return HttpResponse.json(
            { title: 'Server Error', status: 500 },
            { status: 500 },
          );
        }),
      );

      await expect(clearMenuCache()).rejects.toThrow();
    });
  });
});
