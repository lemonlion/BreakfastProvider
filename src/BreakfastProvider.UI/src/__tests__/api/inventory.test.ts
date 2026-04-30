import { getInventory, createInventoryItem, deleteInventoryItem } from '@/lib/api/endpoints/inventory';
import { server } from '@/test-utils/msw/server';
import { http, HttpResponse } from 'msw';

describe('Inventory API', () => {
  it('should return inventory items', async () => {
    const result = await getInventory();
    expect(result).toHaveLength(1);
    expect(result[0].name).toBe('Flour');
  });

  it('should create an inventory item', async () => {
    const result = await createInventoryItem({
      name: 'Sugar',
      category: 'Dry',
      quantity: 25,
      unit: 'kg',
      reorderLevel: 5,
    });
    expect(result).toBeDefined();
  });

  it('should delete an inventory item (204)', async () => {
    await expect(deleteInventoryItem(1)).resolves.toBeUndefined();
  });
});
