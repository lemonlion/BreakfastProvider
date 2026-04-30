import { Suspense } from 'react';
import type { Metadata } from 'next';
import { PageHeader } from '@/components/layout/PageHeader/PageHeader';
import { DashboardStats } from './DashboardStats';
import { RecentOrders } from './RecentOrders';
import { HealthOverview } from './HealthOverview';
import { Skeleton } from '@/components/ui/Skeleton/Skeleton';

export const metadata: Metadata = {
  title: 'Dashboard',
};

/**
 * Dashboard with nested Suspense boundaries.
 *
 * Learning point: Each <Suspense> boundary independently streams in
 * once its data is ready. The three sections load in parallel — if
 * health loads first, it appears before orders/stats finish.
 * This is Next.js 15's streaming SSR in action.
 */
export default function DashboardPage() {
  return (
    <div>
      <PageHeader
        title="Dashboard"
        description="Breakfast operations at a glance"
      />

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 16 }}>
        <Suspense fallback={<Skeleton height={120} />}>
          <DashboardStats />
        </Suspense>

        <Suspense fallback={<Skeleton height={120} />}>
          <HealthOverview />
        </Suspense>
      </div>

      <Suspense fallback={<Skeleton height={300} />}>
        <RecentOrders />
      </Suspense>
    </div>
  );
}
