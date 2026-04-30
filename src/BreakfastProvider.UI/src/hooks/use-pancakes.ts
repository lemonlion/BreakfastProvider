import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createPancake } from '@/lib/api/endpoints';
import type { PancakeRequest } from '@/lib/api/types';

export function useCreatePancake() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: PancakeRequest) => createPancake(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['menu'] });
    },
  });
}
