import { getReservations, createReservation, deleteReservation } from '@/lib/api/endpoints/reservations';
import { server } from '@/test-utils/msw/server';
import { http, HttpResponse } from 'msw';

describe('Reservations API', () => {
  it('should return reservations', async () => {
    const result = await getReservations();
    expect(result).toHaveLength(1);
    expect(result[0].customerName).toBe('Alice');
  });

  it('should create a reservation', async () => {
    const result = await createReservation({
      customerName: 'Bob',
      tableNumber: 3,
      partySize: 2,
      reservedAt: '2024-01-16T19:00:00Z',
    });
    expect(result).toBeDefined();
  });

  it('should delete a reservation (204)', async () => {
    await expect(deleteReservation(1)).resolves.toBeUndefined();
  });
});
