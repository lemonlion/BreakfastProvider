import { get } from '../client';
import type { AuditLogResponse } from '../types';

export function getAuditLogs(
  entityType?: string,
  entityId?: string,
): Promise<AuditLogResponse[]> {
  return get<AuditLogResponse[]>('/audit-logs', {
    params: {
      entityType: entityType || undefined,
      entityId: entityId || undefined,
    },
  });
}
