import { get, post, put, del } from '../client';
import type { ToppingRequest, UpdateToppingRequest, ToppingResponse } from '../types';

export function getToppings(): Promise<ToppingResponse[]> {
  return get<ToppingResponse[]>('/toppings');
}

export function createTopping(request: ToppingRequest): Promise<ToppingResponse> {
  return post<ToppingResponse>('/toppings', { body: request });
}

export function updateTopping(
  toppingId: string,
  request: UpdateToppingRequest,
): Promise<ToppingResponse> {
  return put<ToppingResponse>(`/toppings/${encodeURIComponent(toppingId)}`, { body: request });
}

export function deleteTopping(toppingId: string): Promise<void> {
  return del(`/toppings/${encodeURIComponent(toppingId)}`);
}
