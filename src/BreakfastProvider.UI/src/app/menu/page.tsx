'use client';

import { useMenu, useClearMenuCache } from '@/hooks/use-menu';
import { Card } from '@/components/ui/Card/Card';
import { Badge } from '@/components/ui/Badge/Badge';
import { Button } from '@/components/ui/Button/Button';
import { PageHeader } from '@/components/layout/PageHeader/PageHeader';
import { Skeleton } from '@/components/ui/Skeleton/Skeleton';
import { clearMenuCacheAction } from './actions';
import { useToast } from '@/components/ui/Toast/Toast';

/**
 * Menu page — demonstrates Server Action alongside client mutation.
 *
 * Learning point: Two approaches to the same operation (cache clearing):
 * 1. TanStack Query mutation (useClearMenuCache) — runs in the browser
 * 2. Server Action (clearMenuCacheAction) — runs on the server
 *
 * Both call the same API endpoint, but the Server Action demonstrates
 * how form actions can trigger server-side logic without JavaScript.
 */
export default function MenuPage() {
  const { data: menu, isLoading, isFetching } = useMenu();
  const clearCache = useClearMenuCache();
  const { addToast } = useToast();

  const handleClearCache = async () => {
    try {
      await clearCache.mutateAsync();
      addToast('Menu cache cleared', 'success');
    } catch {
      addToast('Failed to clear cache', 'error');
    }
  };

  if (isLoading) {
    return (
      <div>
        <Skeleton height={32} width={200} />
        {Array.from({ length: 4 }).map((_, i) => (
          <Skeleton key={i} height={100} />
        ))}
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title="Menu"
        description="Today's breakfast menu (cached for 5 minutes)"
        actions={
          <div style={{ display: 'flex', gap: 8 }}>
            {/* Client-side mutation */}
            <Button variant="secondary" onClick={handleClearCache} loading={clearCache.isPending}>
              Clear Cache (Client)
            </Button>

            {/* Server Action — works without JS too */}
            <form action={clearMenuCacheAction}>
              <Button type="submit" variant="secondary">
                Clear Cache (Server Action)
              </Button>
            </form>
          </div>
        }
      />

      {isFetching && <p>Refreshing...</p>}

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: 16 }}>
        {menu?.map((item) => (
          <Card key={item.name} variant="outlined">
            <h3>{item.name}</h3>
            <p>{item.description}</p>
            <Badge variant={item.isAvailable ? 'success' : 'error'}>
              {item.isAvailable ? 'Available' : 'Unavailable'}
            </Badge>
          </Card>
        ))}
      </div>
    </div>
  );
}
