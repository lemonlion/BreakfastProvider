import { v4 as uuidv4 } from 'uuid';
import { ApiError, type ProblemDetails } from './types';

export { ApiError };

const API_BASE = '/api';

interface RequestOptions extends Omit<RequestInit, 'body'> {
  body?: unknown;
  params?: Record<string, string | number | undefined>;
}

export async function apiClient<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { body, params, headers: customHeaders, ...fetchOptions } = options;

  const url = new URL(`${API_BASE}${path}`, window.location.origin);
  if (params) {
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined) {
        url.searchParams.set(key, String(value));
      }
    });
  }

  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    'X-Correlation-Id': uuidv4(),
    ...customHeaders,
  };

  const response = await fetch(url.toString(), {
    ...fetchOptions,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });

  if (response.status === 204) {
    return undefined as T;
  }

  if (!response.ok) {
    let problemDetails: ProblemDetails | undefined;
    try {
      problemDetails = await response.json();
    } catch {
      // Response body isn't JSON
    }
    throw new ApiError(response.status, response.statusText, problemDetails);
  }

  return response.json();
}

export function get<T>(path: string, options?: Omit<RequestOptions, 'body'>): Promise<T> {
  return apiClient<T>(path, { ...options, method: 'GET' });
}

export function post<T>(path: string, options?: RequestOptions): Promise<T> {
  return apiClient<T>(path, { ...options, method: 'POST' });
}

export function put<T>(path: string, options?: RequestOptions): Promise<T> {
  return apiClient<T>(path, { ...options, method: 'PUT' });
}

export function patch<T>(path: string, options?: RequestOptions): Promise<T> {
  return apiClient<T>(path, { ...options, method: 'PATCH' });
}

export function del<T = void>(path: string, options?: RequestOptions): Promise<T> {
  return apiClient<T>(path, { ...options, method: 'DELETE' });
}
