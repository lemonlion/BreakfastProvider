'use client';

import { useDailySpecials, useOrderDailySpecial } from '@/hooks/use-daily-specials';
import { Card } from '@/components/ui/Card/Card';
import { Button } from '@/components/ui/Button/Button';
import { ProgressBar } from '@/components/ui/ProgressBar/ProgressBar';
import { Badge } from '@/components/ui/Badge/Badge';
import { PageHeader } from '@/components/layout/PageHeader/PageHeader';
import { useToast } from '@/components/ui/Toast/Toast';

/**
 * Daily specials with order limits and idempotent ordering.
 *
 * Learning points:
 * - ProgressBar uses assignInlineVars for dynamic width
 * - The API uses Idempotency-Key header to prevent duplicate orders
 * - 409 Conflict = sold out (special-cased in the hook)
 */
export default function DailySpecialsPage() {
  const { data: specials, isLoading } = useDailySpecials();
  const orderSpecial = useOrderDailySpecial();
  const { addToast } = useToast();

  const handleOrder = async (specialId: string) => {
    try {
      await orderSpecial.mutateAsync({ request: { specialId, quantity: 1 } });
      addToast('Order placed!', 'success');
    } catch (err: unknown) {
      if (err instanceof Error && err.message.includes('409')) {
        addToast('Sorry, this special is sold out!', 'warning');
      } else {
        addToast('Failed to place order', 'error');
      }
    }
  };

  return (
    <div>
      <PageHeader title="Daily Specials" description="Limited availability items" />

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: 16 }}>
        {specials?.map((special) => {
          const isSoldOut = special.remainingQuantity <= 0;

          return (
            <Card key={special.specialId} variant="elevated">
              <h3>{special.name}</h3>
              <p>{special.description}</p>

              <p>
                {isSoldOut ? (
                  <Badge variant="error">Sold Out</Badge>
                ) : (
                  <Badge variant="success">{special.remainingQuantity} remaining</Badge>
                )}
              </p>

              <Button
                variant="primary"
                disabled={isSoldOut}
                onClick={() => handleOrder(special.specialId)}
                loading={orderSpecial.isPending}
              >
                {isSoldOut ? 'Sold Out' : 'Order Now'}
              </Button>
            </Card>
          );
        })}
      </div>
    </div>
  );
}
