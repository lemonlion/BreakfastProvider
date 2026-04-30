import { get, post, put, del } from '../client';
import type { InventoryItemRequest, InventoryItemResponse } from '../types';

export function getInventory(): Promise<InventoryItemResponse[]> {
  return get<InventoryItemResponse[]>('/inventory');
}

export function getInventoryItem(id: number): Promise<InventoryItemResponse> {
  return get<InventoryItemResponse>(`/inventory/${id}`);
}

export function createInventoryItem(request: InventoryItemRequest): Promise<InventoryItemResponse> {
  return post<InventoryItemResponse>('/inventory', { body: request });
}

export function updateInventoryItem(
  id: number,
  request: InventoryItemRequest,
): Promise<InventoryItemResponse> {
  return put<InventoryItemResponse>(`/inventory/${id}`, { body: request });
}

export function deleteInventoryItem(id: number): Promise<void> {
  return del(`/inventory/${id}`);
}
