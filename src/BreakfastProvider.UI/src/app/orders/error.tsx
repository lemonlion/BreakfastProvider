'use client';

/**
 * Error boundary for the orders route.
 *
 * Learning point: error.tsx catches runtime errors in the page/layout
 * tree. It MUST be a client component. The `reset` function retries
 * rendering the segment.
 */
export default function OrdersError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <div>
      <h2>Something went wrong loading orders</h2>
      <p>{error.message}</p>
      <button onClick={reset}>Try again</button>
    </div>
  );
}
