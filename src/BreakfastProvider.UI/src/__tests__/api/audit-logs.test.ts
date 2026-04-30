import { getAuditLogs } from '@/lib/api/endpoints/audit-logs';

describe('Audit Logs API', () => {
  it('should return audit logs', async () => {
    const result = await getAuditLogs('Order', 'order-1');
    expect(result).toHaveLength(1);
    expect(result[0].action).toBe('Created');
  });
});
