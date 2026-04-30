'use client';

import { useTransition } from 'react';
import { useForm } from '@tanstack/react-form';
import { z } from 'zod';
import { useCreateWaffle } from '@/hooks/use-waffles';
import { useToast } from '@/components/ui/Toast/Toast';
import { PageHeader } from '@/components/layout/PageHeader/PageHeader';
import { Card } from '@/components/ui/Card/Card';
import { Button } from '@/components/ui/Button/Button';
import { Input } from '@/components/ui/Input/Input';

/**
 * Waffle-specific Zod schema with cross-field validation.
 *
 * Learning point: .refine() adds cross-field validation — here,
 * heart-shaped waffles require crispiness >= 3 because the thin
 * edges burn at low crispiness. This can't be expressed with
 * per-field rules alone.
 */
const waffleSchema = z
  .object({
    recipeType: z.string().min(1, 'Recipe type is required'),
    quantity: z.number().int().min(1).max(50),
    crispinessLevel: z.number().int().min(1).max(5),
    shape: z.enum(['round', 'square', 'heart']),
    milkType: z.enum(['cow', 'goat']),
  })
  .refine(
    (data) => !(data.shape === 'heart' && data.crispinessLevel < 3),
    {
      message: 'Heart-shaped waffles require crispiness level 3 or higher',
      path: ['crispinessLevel'],
    },
  );

/**
 * Waffle creation form — same TanStack Form + Zod pattern as pancakes
 * but with waffle-specific fields and cross-field validation.
 *
 * Learning points:
 * - Different Zod schema with .refine() for cross-field validation
 * - form.Subscribe to conditionally show fields based on other field values
 */
export default function WafflesPage() {
  const [isPending, startTransition] = useTransition();
  const createWaffle = useCreateWaffle();
  const { addToast } = useToast();

  const form = useForm({
    defaultValues: {
      recipeType: 'classic',
      quantity: 1,
      crispinessLevel: 3,
      shape: 'round' as 'round' | 'square' | 'heart',
      milkType: 'cow' as 'cow' | 'goat',
    },
    onSubmit: async ({ value }) => {
      startTransition(async () => {
        try {
          await createWaffle.mutateAsync({
            milk: value.milkType === 'goat' ? 'goat' : 'whole',
            flour: 'all-purpose',
            eggs: 'free-range',
            butter: 'unsalted',
            toppings: [value.recipeType],
          });
          addToast('Waffle batch created!', 'success');
          form.reset();
        } catch {
          addToast('Failed to create waffle batch', 'error');
        }
      });
    },
    validators: {
      onSubmit: ({ value }) => {
        const result = waffleSchema.safeParse(value);
        return result.success ? undefined : result.error.issues.map(i => i.message).join(', ');
      },
    },
  });

  return (
    <div>
      <PageHeader title="Waffles" description="Create new waffle batches" />

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
            name="shape"
            children={(field) => (
              <div>
                <label>Shape</label>
                <select
                  value={field.state.value}
                  onChange={(e) => field.handleChange(e.target.value as 'round' | 'square' | 'heart')}
                >
                  <option value="round">Round</option>
                  <option value="square">Square</option>
                  <option value="heart">Heart</option>
                </select>
              </div>
            )}
          />

          <form.Field
            name="crispinessLevel"
            children={(field) => (
              <Input
                label="Crispiness Level (1–5)"
                type="number"
                value={String(field.state.value)}
                onChange={(e) => field.handleChange(Number(e.target.value))}
                onBlur={field.handleBlur}
                error={(field.state.meta.errors as unknown as string[])?.[0]}
              />
            )}
          />

          {/* Conditionally show milk type info based on shape */}
          <form.Subscribe
            selector={(state) => state.values.shape}
            children={(shape) =>
              shape === 'heart' ? (
                <p style={{ color: 'orange', fontSize: 14 }}>
                  Heart-shaped waffles require crispiness level 3+
                </p>
              ) : null
            }
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
