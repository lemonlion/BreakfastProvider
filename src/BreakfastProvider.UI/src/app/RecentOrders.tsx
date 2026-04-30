'use client';

import { useMemo } from 'react';
import { useOrders } from '@/hooks/use-orders';
import { DataTable } from '@/components/ui/DataTable/DataTable';
import { Badge } from '@/components/ui/Badge/Badge';
import { getStatusColor } from '@/lib/utils';
import Link from 'next/link';
import { type ColumnDef } from '@tanstack/react-table';
import type { OrderResponse } from '@/lib/api/types';

/**
 * Recent orders compact table for the dashboard.
 *
 * Learning point: useOrders with a small pageSize gives a compact
 * summary view. The same hook/query key is shared with the full
 * orders page — TanStack Query caches them separately by params.
 */
export function RecentOrders() {
  const { data } = useOrders(1, 5);

  const columns = useMemo<ColumnDef<OrderResponse, unknown>[]>(
    () => [
      {
        accessorKey: 'orderId',
        header: 'Order',
        cell: ({ row }) => (
          <Link href={`/orders/${row.original.orderId}`}>
            {row.original.orderId.slice(0, 8)}...
          </Link>
        ),
      },
      {
        accessorKey: 'status',
        header: 'Status',
        cell: ({ row }) => (
          <Badge variant={getStatusColor(row.original.status)}>
            {row.original.status}
          </Badge>
        ),
      },
      {
        accessorKey: 'createdAt',
        header: 'Created',
        cell: ({ row }) => new Date(row.original.createdAt).toLocaleString(),
      },
    ],
    [],
  );

  return (
    <div>
      <h3>Recent Orders</h3>
      <DataTable data={data?.items ?? []} columns={columns} />
    </div>
  );
}
