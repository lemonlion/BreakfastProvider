import { useQuery, useInfiniteQuery } from '@tanstack/react-query';
import { getAuditLogs } from '@/lib/api/endpoints';

export function useAuditLogs(entityType?: string, entityId?: string) {
  return useQuery({
    queryKey: ['audit-logs', { entityType, entityId }],
    queryFn: () => getAuditLogs(entityType, entityId),
    enabled: !!(entityType || entityId),
  });
}

export function useInfiniteAuditLogs(entityType?: string, entityId?: string) {
  return useInfiniteQuery({
    queryKey: ['audit-logs-infinite', { entityType, entityId }],
    queryFn: () => getAuditLogs(entityType, entityId),
    initialPageParam: 0,
    getNextPageParam: (lastPage, allPages) => {
      const totalFetched = allPages.flat().length;
      return totalFetched < lastPage.length ? totalFetched : undefined;
    },
    enabled: !!(entityType || entityId),
  });
}
