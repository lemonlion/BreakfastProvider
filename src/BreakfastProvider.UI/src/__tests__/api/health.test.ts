import { getHealth, getHeartbeat } from '@/lib/api/endpoints/health';
import { server } from '@/test-utils/msw/server';
import { http, HttpResponse } from 'msw';

describe('Health API', () => {
  describe('getHealth()', () => {
    it('should return health status with entries', async () => {
      const result = await getHealth();
      expect(result.status).toBe('Healthy');
      expect(result.entries).toBeDefined();
    });
  });

  describe('getHeartbeat()', () => {
    it('should return heartbeat status', async () => {
      server.use(
        http.get('http://localhost:3000/api/', () => {
          return HttpResponse.json({ status: 'ok' });
        }),
      );

      const result = await getHeartbeat();
      expect(result.status).toBeDefined();
    });
  });
});
