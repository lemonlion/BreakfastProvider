import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getToppings, createTopping, updateTopping, deleteTopping } from '@/lib/api/endpoints';
import type { ToppingRequest, UpdateToppingRequest, ToppingResponse } from '@/lib/api/types';

export function useToppings() {
  return useQuery({
    queryKey: ['toppings'],
    queryFn: getToppings,
  });
}

export function useCreateTopping() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: ToppingRequest) => createTopping(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['toppings'] });
    },
  });
}

export function useUpdateTopping() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateToppingRequest }) =>
      updateTopping(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['toppings'] });
    },
  });
}

export function useDeleteTopping() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (toppingId: string) => deleteTopping(toppingId),
    onMutate: async (toppingId) => {
      await queryClient.cancelQueries({ queryKey: ['toppings'] });
      const previous = queryClient.getQueryData<ToppingResponse[]>(['toppings']);
      queryClient.setQueryData<ToppingResponse[]>(['toppings'], (old) =>
        old?.filter((t) => t.toppingId !== toppingId) ?? [],
      );
      return { previous };
    },
    onError: (_err, _id, context) => {
      if (context?.previous) {
        queryClient.setQueryData(['toppings'], context.previous);
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['toppings'] });
    },
  });
}
