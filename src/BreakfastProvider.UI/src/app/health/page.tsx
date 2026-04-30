'use client';

import { Suspense } from 'react';
import { useHealth, useHeartbeat } from '@/hooks/use-health';
import { Badge } from '@/components/ui/Badge/Badge';
import { Card } from '@/components/ui/Card/Card';
import { Skeleton } from '@/components/ui/Skeleton/Skeleton';
import { PageHeader } from '@/components/layout/PageHeader/PageHeader';
import { getStatusColor } from '@/lib/utils';

/**
 * Health dashboard — auto-refreshes every 30 seconds.
 *
 * Learning point: The useHealth hook has refetchInterval: 30_000.
 * The page updates automatically without user interaction.
 * Nested Suspense boundaries let the overall status appear before
 * individual dependency checks load.
 */
export default function HealthPage() {
  const { data: health, isLoading, dataUpdatedAt } = useHealth();
  const { data: heartbeat } = useHeartbeat();

  if (isLoading) {
    return (
      <div>
        <Skeleton height={32} width={200} />
        <Skeleton height={400} />
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title="System Health"
        description={`Last updated: ${new Date(dataUpdatedAt).toLocaleTimeString()}`}
      />

      {/* Overall status */}
      <Card variant="elevated" style={{ marginBottom: 16 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
          <h3>Overall Status</h3>
          <Badge variant={getStatusColor(health?.status ?? 'Unknown')} dot>
            {health?.status}
          </Badge>
        </div>
      </Card>

      {/* Individual dependency checks */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(250px, 1fr))', gap: 16 }}>
        {health?.entries && Object.entries(health.entries).map(([name, entry]) => (
          <Suspense key={name} fallback={<Skeleton height={80} />}>
            <Card variant="outlined">
              <h4>{name}</h4>
              <Badge variant={getStatusColor(entry.status)} dot>
                {entry.status}
              </Badge>
              {entry.description && <p style={{ opacity: 0.7 }}>{entry.description}</p>}
              {entry.duration && <p style={{ fontFamily: 'var(--font-mono)', fontSize: 12 }}>{entry.duration}</p>}
            </Card>
          </Suspense>
        ))}
      </div>
    </div>
  );
}
