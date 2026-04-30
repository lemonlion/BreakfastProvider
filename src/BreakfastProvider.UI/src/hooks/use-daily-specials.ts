import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getDailySpecials, orderDailySpecial, resetDailySpecialOrders } from '@/lib/api/endpoints';
import type { DailySpecialOrderRequest } from '@/lib/api/types';

export function useDailySpecials() {
  return useQuery({
    queryKey: ['daily-specials'],
    queryFn: getDailySpecials,
  });
}

export function useOrderDailySpecial() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      request,
      idempotencyKey,
    }: {
      request: DailySpecialOrderRequest;
      idempotencyKey?: string;
    }) => orderDailySpecial(request, idempotencyKey),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['daily-specials'] });
    },
  });
}

export function useResetDailySpecialOrders() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (specialId?: string) => resetDailySpecialOrders(specialId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['daily-specials'] });
    },
  });
}
