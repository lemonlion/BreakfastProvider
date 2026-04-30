import { Skeleton } from '@/components/ui/Skeleton/Skeleton';

/**
 * Loading UI for orders route segment.
 *
 * Learning point: loading.tsx is a special Next.js file that wraps
 * the page in a <Suspense> boundary automatically. It shows while
 * the page component (and any data it depends on) is loading.
 */
export default function OrdersLoading() {
  return (
    <div>
      <Skeleton width={200} height={32} />
      <Skeleton width="100%" height={400} />
    </div>
  );
}
