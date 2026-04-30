'use client';

import { useSearchParams, useRouter, usePathname } from 'next/navigation';
import { useTransition, useOptimistic, useMemo, Suspense } from 'react';
import { useOrders, usePrefetchOrder, useUpdateOrderStatus } from '@/hooks/use-orders';
import { DataTable } from '@/components/ui/DataTable/DataTable';
import { Badge } from '@/components/ui/Badge/Badge';
import { Button } from '@/components/ui/Button/Button';
import { PageHeader } from '@/components/layout/PageHeader/PageHeader';
import { getStatusColor, getNextStatuses } from '@/lib/utils';
import Link from 'next/link';
import { type ColumnDef } from '@tanstack/react-table';
import type { OrderResponse, OrderStatus } from '@/lib/api/types';

/**
 * Orders list with URL-driven state, optimistic updates, hover prefetching.
 *
 * Learning points:
 *
 * 1. URL State Management — useSearchParams() drives pagination and filters.
 *    The URL is the single source of truth: /orders?page=2&status=Preparing.
 *    This makes the page shareable and back-button friendly.
 *
 * 2. Hover Prefetching — Hovering an order row triggers prefetchQuery()
 *    for the order detail page. By the time the user clicks, the data
 *    is already cached. Zero loading spinner experience.
 *
 * 3. Optimistic Updates (React 19) — useOptimistic() displays new status
 *    immediately while the mutation is in flight. If it fails, React
 *    automatically reverts to the server state. The TanStack Query cache
 *    is also updated optimistically in the hook.
 *
 * 4. useTransition — Wraps router.push() so page navigation doesn't block
 *    the current UI render. The URL updates are deferred.
 */
export default function OrdersPage() {
  return (
    <Suspense fallback={<p>Loading...</p>}>
      <OrdersPageContent />
    </Suspense>
  );
}

function OrdersPageContent() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();
  const [isPending, startTransition] = useTransition();

  // URL-driven state
  const page = Number(searchParams.get('page') ?? '1');
  const pageSize = Number(searchParams.get('pageSize') ?? '10');
  const statusFilter = searchParams.get('status') ?? undefined;

  const { data, isLoading } = useOrders(page, pageSize);
  const prefetchOrder = usePrefetchOrder();
  const updateStatus = useUpdateOrderStatus();

  // React 19 useOptimistic for instant status feedback
  const [optimisticOrders, setOptimisticOrder] = useOptimistic(
    data?.items ?? [],
    (currentOrders: OrderResponse[], updatedOrder: { orderId: string; status: string }) =>
      currentOrders.map((order) =>
        order.orderId === updatedOrder.orderId ? { ...order, status: updatedOrder.status } : order,
      ),
  );

  const handleStatusChange = (orderId: string, newStatus: string) => {
    // Show optimistic update immediately
    setOptimisticOrder({ orderId: orderId, status: newStatus });
    // Fire the actual mutation
    updateStatus.mutate({ orderId, request: { status: newStatus } });
  };

  // URL state updater
  const setSearchParam = (key: string, value: string | null) => {
    const params = new URLSearchParams(searchParams.toString());
    if (value === null) {
      params.delete(key);
    } else {
      params.set(key, value);
    }
    startTransition(() => {
      router.push(`${pathname}?${params.toString()}` as any);
    });
  };

  const columns = useMemo<ColumnDef<OrderResponse, unknown>[]>(
    () => [
      {
        accessorKey: 'orderId',
        header: 'Order ID',
        cell: ({ row }) => (
          <Link
            href={`/orders/${row.original.orderId}`}
            // Prefetch on hover — data loads before click
            onMouseEnter={() => prefetchOrder(row.original.orderId)}
          >
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
        accessorKey: 'itemCount',
        header: 'Items',
      },
      {
        accessorKey: 'createdAt',
        header: 'Created',
        cell: ({ row }) => new Date(row.original.createdAt).toLocaleString(),
      },
      {
        id: 'actions',
        header: 'Actions',
        cell: ({ row }) => {
          const nextStatuses = getNextStatuses(row.original.status as OrderStatus);
          return (
            <div style={{ display: 'flex', gap: 4 }}>
              {nextStatuses.map((status) => (
                <Button
                  key={status}
                  variant="ghost"
                  size="sm"
                  onClick={() => handleStatusChange(row.original.orderId, status)}
                >
                  → {status}
                </Button>
              ))}
            </div>
          );
        },
      },
    ],
    [prefetchOrder, handleStatusChange],
  );

  return (
    <div>
      <PageHeader
        title="Orders"
        description="Manage breakfast orders"
        actions={
          <Link href="/orders/new">
            <Button>New Order</Button>
          </Link>
        }
      />

      {/* Status filter tabs */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
        {['All', 'Created', 'Preparing', 'Ready', 'Completed', 'Cancelled'].map((status) => (
          <Button
            key={status}
            variant={statusFilter === (status === 'All' ? undefined : status) ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setSearchParam('status', status === 'All' ? null : status)}
          >
            {status}
          </Button>
        ))}
      </div>

      <DataTable
        data={optimisticOrders}
        columns={columns}
        enableGlobalFilter
        enableRowSelection
        onSelectionChange={(selected) => {
          // Could enable bulk status updates
          console.log('Selected orders:', selected.length);
        }}
        pageCount={data ? Math.ceil(data.totalCount / pageSize) : 0}
        pageIndex={page - 1}
        pageSize={pageSize}
        onPageChange={(newPage) => setSearchParam('page', String(newPage + 1))}
      />
    </div>
  );
}
