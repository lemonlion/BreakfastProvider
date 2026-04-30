import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createWaffle } from '@/lib/api/endpoints';
import type { WaffleRequest } from '@/lib/api/types';

export function useCreateWaffle() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: WaffleRequest) => createWaffle(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['menu'] });
    },
  });
}
