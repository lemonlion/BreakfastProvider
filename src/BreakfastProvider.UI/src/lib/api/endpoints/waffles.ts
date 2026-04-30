import { post } from '../client';
import type { WaffleRequest, WaffleResponse } from '../types';

export function createWaffle(request: WaffleRequest): Promise<WaffleResponse> {
  return post<WaffleResponse>('/waffles', { body: request });
}
