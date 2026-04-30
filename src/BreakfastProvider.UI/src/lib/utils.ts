import { format, formatDistanceToNow, isAfter, parseISO } from 'date-fns';
import type { OrderStatus } from './api/types';

export function formatDate(isoDate: string): string {
  return format(parseISO(isoDate), 'PPp');
}

export function formatRelativeTime(isoDate: string): string {
  return formatDistanceToNow(parseISO(isoDate), { addSuffix: true });
}

export function isFutureDate(isoDate: string): boolean {
  return isAfter(parseISO(isoDate), new Date());
}

export function getNextStatuses(currentStatus: OrderStatus): OrderStatus[] {
  const transitions: Record<OrderStatus, OrderStatus[]> = {
    Created: ['Preparing', 'Cancelled'],
    Preparing: ['Ready'],
    Ready: ['Completed'],
    Completed: [],
    Cancelled: [],
  };
  return transitions[currentStatus] ?? [];
}

export function getStatusColor(status: string): 'success' | 'warning' | 'error' | 'info' | 'neutral' {
  switch (status.toLowerCase()) {
    case 'created': return 'info';
    case 'preparing': return 'warning';
    case 'ready': return 'success';
    case 'completed': return 'success';
    case 'cancelled': return 'error';
    case 'healthy': return 'success';
    case 'degraded': return 'warning';
    case 'unhealthy': return 'error';
    default: return 'neutral';
  }
}

export function toCSV<T extends Record<string, unknown>>(data: T[], columns: (keyof T)[]): string {
  const header = columns.join(',');
  const rows = data.map(row =>
    columns
      .map(col => {
        const val = row[col];
        const str = String(val ?? '');
        return str.includes(',') || str.includes('"') || str.includes('\n')
          ? `"${str.replace(/"/g, '""')}"`
          : str;
      })
      .join(','),
  );
  return [header, ...rows].join('\n');
}

export function downloadFile(content: string, filename: string, mimeType = 'text/csv'): void {
  const blob = new Blob([content], { type: mimeType });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}
