import { get, del } from '../client';
import { v4 as uuidv4 } from 'uuid';
import { apiClient } from '../client';
import type {
  DailySpecialResponse,
  DailySpecialOrderRequest,
  DailySpecialOrderResponse,
} from '../types';

export function getDailySpecials(): Promise<DailySpecialResponse[]> {
  return get<DailySpecialResponse[]>('/daily-specials');
}

export function orderDailySpecial(
  request: DailySpecialOrderRequest,
  idempotencyKey?: string,
): Promise<DailySpecialOrderResponse> {
  return apiClient<DailySpecialOrderResponse>('/daily-specials/orders', {
    method: 'POST',
    body: request,
    headers: {
      'Idempotency-Key': idempotencyKey ?? uuidv4(),
    },
  });
}

export function resetDailySpecialOrders(specialId?: string): Promise<void> {
  return del('/daily-specials/orders', {
    params: specialId ? { specialId } : undefined,
  });
}
