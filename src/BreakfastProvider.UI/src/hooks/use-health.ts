import { useQuery } from '@tanstack/react-query';
import { getHealth, getHeartbeat } from '@/lib/api/endpoints';

export function useHealth() {
  return useQuery({
    queryKey: ['health'],
    queryFn: getHealth,
    refetchInterval: 30_000,
    refetchIntervalInBackground: true,
  });
}

export function useHeartbeat() {
  return useQuery({
    queryKey: ['heartbeat'],
    queryFn: getHeartbeat,
    refetchInterval: 60_000,
  });
}
