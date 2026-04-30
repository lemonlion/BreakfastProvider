import { EmptyState } from '@/components/ui/EmptyState/EmptyState';
import Link from 'next/link';
import { Button } from '@/components/ui/Button/Button';

/**
 * Global 404 page.
 *
 * Learning point: not-found.tsx at the app root catches all unmatched routes.
 * It can also be triggered programmatically via notFound() from next/navigation.
 */
export default function NotFound() {
  return (
    <EmptyState
      icon="🔍"
      title="Page Not Found"
      description="The page you're looking for doesn't exist or has been moved."
      action={
        <Link href="/">
          <Button variant="primary">Back to Dashboard</Button>
        </Link>
      }
    />
  );
}
