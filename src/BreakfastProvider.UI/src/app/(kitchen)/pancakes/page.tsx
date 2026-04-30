'use client';

import { useTransition } from 'react';
import { useForm } from '@tanstack/react-form';
import { z } from 'zod';
import { useCreatePancake } from '@/hooks/use-pancakes';
import { useToast } from '@/components/ui/Toast/Toast';
import { PageHeader } from '@/components/layout/PageHeader/PageHeader';
import { Card } from '@/components/ui/Card/Card';
import { Button } from '@/components/ui/Button/Button';
import { Input } from '@/components/ui/Input/Input';

const pancakeSchema = z.object({
  recipeType: z.string().min(1, 'Recipe type is required'),
  quantity: z.number().int().min(1).max(100),
  milkType: z.enum(['cow', 'goat']),
});

/**
 * Pancake creation form using TanStack Form + Zod validation.
 *
 * Learning points:
 * - useForm() from @tanstack/react-form manages form state
 * - zodValidator integrates Zod schemas for field-level validation
 * - form.Field renders each field with built-in error state
 * - useTransition() wraps the mutation to keep the UI responsive
 *   while the request is in flight (React 19 concurrent feature)
 */
export default function PancakesPage() {
  const [isPending, startTransition] = useTransition();
  const createPancake = useCreatePancake();
  const { addToast } = useToast();

  const form = useForm({
    defaultValues: {
      recipeType: 'classic',
      quantity: 1,
      milkType: 'cow' as 'cow' | 'goat',
    },
    onSubmit: async ({ value }) => {
      startTransition(async () => {
        try {
          await createPancake.mutateAsync({
            milk: value.milkType === 'goat' ? 'goat' : 'whole',
            flour: 'all-purpose',
            eggs: 'free-range',
            toppings: [value.recipeType],
          });
          addToast('Pancake batch created!', 'success');
          form.reset();
        } catch {
          addToast('Failed to create pancake batch', 'error');
        }
      });
    },
    validators: {
      onSubmit: ({ value }) => {
        const result = pancakeSchema.safeParse(value);
        return result.success ? undefined : result.error.issues.map(i => i.message).join(', ');
      },
    },
  });

  return (
    <div>
      <PageHeader title="Pancakes" description="Create new pancake batches" />

      <Card variant="outlined">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            e.stopPropagation();
            form.handleSubmit();
          }}
        >
          <form.Field
            name="recipeType"
            children={(field) => (
              <Input
                label="Recipe Type"
                value={field.state.value}
                onChange={(e) => field.handleChange(e.target.value)}
                onBlur={field.handleBlur}
                error={(field.state.meta.errors as unknown as string[])?.[0]}
              />
            )}
          />

          <form.Field
            name="quantity"
            children={(field) => (
              <Input
                label="Quantity"
                type="number"
                value={String(field.state.value)}
                onChange={(e) => field.handleChange(Number(e.target.value))}
                onBlur={field.handleBlur}
                error={(field.state.meta.errors as unknown as string[])?.[0]}
              />
            )}
          />

          <form.Field
            name="milkType"
            children={(field) => (
              <div>
                <label>Milk Type</label>
                <select
                  value={field.state.value}
                  onChange={(e) => field.handleChange(e.target.value as 'cow' | 'goat')}
                >
                  <option value="cow">Cow Milk</option>
                  <option value="goat">Goat Milk</option>
                </select>
              </div>
            )}
          />

          <Button type="submit" loading={isPending}>
            Create Batch
          </Button>
        </form>
      </Card>
    </div>
  );
}
