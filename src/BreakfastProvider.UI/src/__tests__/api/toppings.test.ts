import { getToppings, createTopping, updateTopping, deleteTopping } from '@/lib/api/endpoints/toppings';
import { server } from '@/test-utils/msw/server';
import { http, HttpResponse } from 'msw';

describe('Toppings API', () => {
  describe('getToppings()', () => {
    it('should return list of toppings', async () => {
      const result = await getToppings();
      expect(result).toHaveLength(2);
      expect(result[0].name).toBe('Chocolate Chips');
    });
  });

  describe('createTopping()', () => {
    it('should create a new topping', async () => {
      const result = await createTopping({ name: 'Blueberries', category: 'Fruit' });
      expect(result).toBeDefined();
    });
  });

  describe('updateTopping()', () => {
    it('should update an existing topping', async () => {
      server.use(
        http.put('http://localhost:3000/api/toppings/:id', () => {
          return HttpResponse.json({ toppingId: 'top-1', name: 'Updated', category: 'Sweet' });
        }),
      );

      const result = await updateTopping('top-1', { name: 'Updated', category: 'Sweet' });
      expect(result.name).toBe('Updated');
    });
  });

  describe('deleteTopping()', () => {
    it('should delete a topping (204)', async () => {
      await expect(deleteTopping('top-1')).resolves.toBeUndefined();
    });
  });
});
