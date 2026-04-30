import {
  useQuery,
  useMutation,
  useQueryClient,
  keepPreviousData,
} from '@tanstack/react-query';
import { getOrders, getOrder, createOrder, updateOrderStatus } from '@/lib/api/endpoints';
import type { OrderRequest, UpdateOrderStatusRequest, OrderResponse } from '@/lib/api/types';

export function useOrders(page = 1, pageSize = 10) {
  return useQuery({
    queryKey: ['orders', { page, pageSize }],
    queryFn: () => getOrders(page, pageSize),
    placeholderData: keepPreviousData,
  });
}

export function useOrder(orderId: string) {
  return useQuery({
    queryKey: ['orders', orderId],
    queryFn: () => getOrder(orderId),
    enabled: !!orderId,
  });
}

export function usePrefetchOrder() {
  const queryClient = useQueryClient();
  return (orderId: string) => {
    queryClient.prefetchQuery({
      queryKey: ['orders', orderId],
      queryFn: () => getOrder(orderId),
      staleTime: 30_000,
    });
  };
}

export function useCreateOrder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: OrderRequest) => createOrder(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['orders'] });
    },
  });
}

export function useUpdateOrderStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      orderId,
      request,
    }: {
      orderId: string;
      request: UpdateOrderStatusRequest;
    }) => updateOrderStatus(orderId, request),
    onMutate: async ({ orderId, request }) => {
      await queryClient.cancelQueries({ queryKey: ['orders', orderId] });
      const previousOrder = queryClient.getQueryData<OrderResponse>(['orders', orderId]);
      if (previousOrder) {
        queryClient.setQueryData<OrderResponse>(['orders', orderId], {
          ...previousOrder,
          status: request.status ?? previousOrder.status,
        });
      }
      return { previousOrder };
    },
    onError: (_error, { orderId }, context) => {
      if (context?.previousOrder) {
        queryClient.setQueryData(['orders', orderId], context.previousOrder);
      }
    },
    onSettled: (_data, _error, { orderId }) => {
      queryClient.invalidateQueries({ queryKey: ['orders', orderId] });
      queryClient.invalidateQueries({ queryKey: ['orders'] });
    },
  });
}
