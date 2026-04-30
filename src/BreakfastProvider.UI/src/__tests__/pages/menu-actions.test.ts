import { clearMenuCacheAction } from '@/app/menu/actions';
import { server } from '@/test-utils/msw/server';
import { http, HttpResponse } from 'msw';

// Mock next/cache since it's a server-only module
jest.mock('next/cache', () => ({
  revalidatePath: jest.fn(),
}));

describe('clearMenuCacheAction', () => {
  it('should call DELETE /menu/cache', async () => {
    let wasCalled = false;

    // Server action fetches directly from API_BASE_URL (not through /api proxy)
    server.use(
      http.delete('http://localhost:5080/menu/cache', () => {
        wasCalled = true;
        return new HttpResponse(null, { status: 204 });
      }),
    );

    await clearMenuCacheAction();
    expect(wasCalled).toBe(true);
  });

  it('should call revalidatePath for /menu', async () => {
    const { revalidatePath } = require('next/cache');

    await clearMenuCacheAction();

    expect(revalidatePath).toHaveBeenCalledWith('/menu');
  });
});
