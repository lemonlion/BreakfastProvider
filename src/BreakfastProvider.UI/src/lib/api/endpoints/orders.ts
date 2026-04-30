import { get, post, patch } from '../client';
import type {
  OrderRequest,
  OrderResponse,
  UpdateOrderStatusRequest,
  PaginatedResponse,
} from '../types';

export function getOrders(page = 1, pageSize = 10): Promise<PaginatedResponse<OrderResponse>> {
  return get<PaginatedResponse<OrderResponse>>('/orders', {
    params: { page, pageSize },
  });
}

export function getOrder(orderId: string): Promise<OrderResponse> {
  return get<OrderResponse>(`/orders/${encodeURIComponent(orderId)}`);
}

export function createOrder(request: OrderRequest): Promise<OrderResponse> {
  return post<OrderResponse>('/orders', { body: request });
}

export function updateOrderStatus(
  orderId: string,
  request: UpdateOrderStatusRequest,
): Promise<OrderResponse> {
  return patch<OrderResponse>(`/orders/${encodeURIComponent(orderId)}/status`, { body: request });
}
