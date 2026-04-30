import { get, del } from '../client';
import type { MenuItemResponse } from '../types';

export function getMenu(): Promise<MenuItemResponse[]> {
  return get<MenuItemResponse[]>('/menu');
}

export function clearMenuCache(): Promise<void> {
  return del('/menu/cache');
}
