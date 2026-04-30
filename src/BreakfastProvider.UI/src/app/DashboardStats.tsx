'use client';

import { useOrders } from '@/hooks/use-orders';
import { useMenu } from '@/hooks/use-menu';
import { Card } from '@/components/ui/Card/Card';

/**
 * Dashboard statistics cards.
 *
 * Learning point: Multiple hooks composed in one component — each
 * fires an independent query. TanStack Query deduplicates if the
 * same query is used elsewhere on the page.
 */
export function DashboardStats() {
  const { data: orders } = useOrders(1, 1);
  const { data: menu } = useMenu();

  return (
    <>
      <Card variant="elevated">
        <h3>Total Orders</h3>
        <p style={{ fontSize: 32, fontWeight: 700 }}>{orders?.totalCount ?? '—'}</p>
      </Card>

      <Card variant="elevated">
        <h3>Menu Items</h3>
        <p style={{ fontSize: 32, fontWeight: 700 }}>{menu?.length ?? '—'}</p>
      </Card>

      <Card variant="elevated">
        <h3>Active Today</h3>
        <p style={{ fontSize: 32, fontWeight: 700 }}>—</p>
      </Card>
    </>
  );
}
