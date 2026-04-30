'use client';

import { Suspense } from 'react';
import { useIngredients } from '@/hooks/use-ingredients';
import { Card } from '@/components/ui/Card/Card';
import { Badge } from '@/components/ui/Badge/Badge';
import { Skeleton } from '@/components/ui/Skeleton/Skeleton';
import { PageHeader } from '@/components/layout/PageHeader/PageHeader';

const INGREDIENT_LABELS = ['Milk', 'Goat Milk', 'Eggs', 'Flour'];

export default function IngredientsPage() {
  const results = useIngredients();

  return (
    <div>
      <PageHeader title="Ingredients" description="Check ingredient availability from downstream suppliers" />

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 16 }}>
        {INGREDIENT_LABELS.map((name, index) => {
          const query = results[index];
          return (
            <Suspense key={name} fallback={<Skeleton height={80} />}>
              <Card variant="outlined">
                <h4>{name}</h4>
                {query?.isLoading ? (
                  <Skeleton height={20} width={80} />
                ) : query?.isError ? (
                  <Badge variant="error">Unavailable</Badge>
                ) : (
                  <Badge variant="success">Available</Badge>
                )}
              </Card>
            </Suspense>
          );
        })}
      </div>
    </div>
  );
}
