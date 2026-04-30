'use client';

import { useHealth } from '@/hooks/use-health';
import { Badge } from '@/components/ui/Badge/Badge';
import { Card } from '@/components/ui/Card/Card';
import { getStatusColor } from '@/lib/utils';

/**
 * Health overview card for the dashboard.
 *
 * Learning point: useHealth() has refetchInterval: 30_000 configured
 * in the hook, so this component auto-refreshes without user action.
 */
export function HealthOverview() {
  const { data: health } = useHealth();

  return (
    <Card variant="elevated">
      <h3>System Health</h3>
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, marginTop: 8 }}>
        {health?.entries ? Object.entries(health.entries).map(([name, entry]) => (
          <Badge key={name} variant={getStatusColor(entry.status)} dot>
            {name}
          </Badge>
        )) : <p>Loading...</p>}
      </div>
    </Card>
  );
}
