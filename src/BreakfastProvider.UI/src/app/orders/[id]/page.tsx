'use client';

import { use } from 'react';
import { useOrder } from '@/hooks/use-orders';
import { Badge } from '@/components/ui/Badge/Badge';
import { Card } from '@/components/ui/Card/Card';
import { Spinner } from '@/components/ui/Spinner/Spinner';
import { getStatusColor } from '@/lib/utils';

/**
 * Order detail page — dynamic route segment [id].
 *
 * Learning points:
 * - [id] folder creates a dynamic route: /orders/abc → params.id = 'abc'
 * - React 19's use() hook unwraps the params promise (Next.js 15 change)
 * - Data was likely already prefetched by hover on the orders list
 */
interface OrderDetailProps {
  params: Promise<{ id: string }>;
}

export default function OrderDetailPage({ params }: OrderDetailProps) {
  const { id } = use(params);
  const { data: order, isLoading, error } = useOrder(id);

  if (isLoading) return <Spinner size={48} />;
  if (error) return <div>Error loading order: {error.message}</div>;
  if (!order) return <div>Order not found</div>;

  return (
    <div>
      <h2>Order {order.orderId}</h2>
      <Badge variant={getStatusColor(order.status)}>{order.status}</Badge>

      <Card variant="outlined">
        <h3>Items</h3>
        <ul>
          {order.items?.map((item, i) => (
            <li key={i}>
              {item.itemType} × {item.quantity}
            </li>
          ))}
        </ul>
      </Card>

      <Card variant="outlined">
        <h3>Timeline</h3>
        <p>Created: {new Date(order.createdAt).toLocaleString()}</p>
      </Card>
    </div>
  );
}
