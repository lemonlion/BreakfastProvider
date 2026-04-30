'use client';

import { useForm } from '@tanstack/react-form';
import { z } from 'zod';
import { StepWizard } from '@/components/ui/StepWizard/StepWizard';
import { Input } from '@/components/ui/Input/Input';
import { Button } from '@/components/ui/Button/Button';
import { Card } from '@/components/ui/Card/Card';
import { useCreateOrder } from '@/hooks/use-orders';
import { useToast } from '@/components/ui/Toast/Toast';
import { useRouter } from 'next/navigation';

/**
 * Multi-step order creation wizard.
 *
 * Learning points:
 * - TanStack Form's FieldArray manages a dynamic list of order items
 * - Each wizard step validates independently before proceeding
 * - Step 3 shows a summary with no editable fields (read-only form state)
 * - On submit, the mutation fires and redirects to the new order page
 */

const orderItemSchema = z.object({
  name: z.string().min(1, 'Item name is required'),
  quantity: z.number().int().min(1).max(99),
  specialInstructions: z.string().optional(),
});

const orderSchema = z.object({
  customerName: z.string().min(1, 'Customer name is required'),
  items: z.array(orderItemSchema).min(1, 'At least one item is required'),
  notes: z.string().optional(),
});

export default function NewOrderPage() {
  const createOrder = useCreateOrder();
  const { addToast } = useToast();
  const router = useRouter();

  const form = useForm({
    defaultValues: {
      customerName: '',
      items: [{ name: '', quantity: 1, specialInstructions: '' }],
      notes: '',
    },
    validators: {
      onSubmit: ({ value }) => {
        const result = orderSchema.safeParse(value);
        return result.success ? undefined : result.error.issues.map(i => i.message).join(', ');
      },
    },
    onSubmit: async ({ value }) => {
      try {
        const result = await createOrder.mutateAsync({
          customerName: value.customerName,
          items: value.items.map((item) => ({
            itemType: item.name,
            quantity: item.quantity,
          })),
          tableNumber: undefined,
        });
        addToast('Order created successfully!', 'success');
        router.push(`/orders/${result.orderId}`);
      } catch {
        addToast('Failed to create order', 'error');
      }
    },
  });

  const steps = [
    {
      label: 'Customer',
      content: (
        <div>
          <form.Field
            name="customerName"
            children={(field) => (
              <Input
                label="Customer Name"
                value={field.state.value}
                onChange={(e) => field.handleChange(e.target.value)}
                onBlur={field.handleBlur}
                error={(field.state.meta.errors as unknown as string[])?.[0]}
              />
            )}
          />
        </div>
      ),
      validate: async () => {
        const name = form.getFieldValue('customerName');
        return !!name && name.length > 0;
      },
    },
    {
      label: 'Items',
      content: (
        <div>
          {/**
           * FieldArray — dynamically add/remove items.
           *
           * Learning point: form.Field with array name + index gives
           * each sub-field its own validation state. pushValue() adds
           * a new item; removeValue() removes by index.
           */}
          <form.Field
            name="items"
            mode="array"
            children={(field) => (
              <div>
                {field.state.value.map((_: unknown, index: number) => (
                  <Card key={index} variant="flat" style={{ marginBottom: 8 }}>
                    <form.Field
                      name={`items[${index}].name`}
                      children={(subField) => (
                        <Input
                          label="Item Name"
                          value={subField.state.value}
                          onChange={(e) => subField.handleChange(e.target.value)}
                          error={(subField.state.meta.errors as unknown as string[])?.[0]}
                        />
                      )}
                    />
                    <form.Field
                      name={`items[${index}].quantity`}
                      children={(subField) => (
                        <Input
                          label="Quantity"
                          type="number"
                          value={String(subField.state.value)}
                          onChange={(e) => subField.handleChange(Number(e.target.value))}
                        />
                      )}
                    />
                    {field.state.value.length > 1 && (
                      <Button
                        variant="danger"
                        size="sm"
                        onClick={() => field.removeValue(index)}
                      >
                        Remove
                      </Button>
                    )}
                  </Card>
                ))}
                <Button
                  variant="secondary"
                  onClick={() => field.pushValue({ name: '', quantity: 1, specialInstructions: '' })}
                >
                  + Add Item
                </Button>
              </div>
            )}
          />
        </div>
      ),
      validate: async () => {
        const items = form.getFieldValue('items');
        return items.length > 0 && items.every((item: { name: string }) => item.name.length > 0);
      },
    },
    {
      label: 'Review',
      content: (
        <div>
          <h3>Order Summary</h3>
          <form.Subscribe
            selector={(state) => state.values}
            children={(values) => (
              <div>
                <p><strong>Customer:</strong> {values.customerName}</p>
                <ul>
                  {values.items.map((item, i) => (
                    <li key={i}>{item.name} × {item.quantity}</li>
                  ))}
                </ul>
                {values.notes && <p><strong>Notes:</strong> {values.notes}</p>}
              </div>
            )}
          />
        </div>
      ),
    },
  ];

  return (
    <div>
      <h2>New Order</h2>
      <StepWizard
        steps={steps}
        onComplete={() => form.handleSubmit()}
        showStepIndicator
      />
    </div>
  );
}
