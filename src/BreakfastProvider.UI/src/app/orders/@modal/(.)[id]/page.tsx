'use client';

import { use } from 'react';
import { useRouter } from 'next/navigation';
import { Modal } from '@/components/ui/Modal/Modal';
import { useOrder } from '@/hooks/use-orders';

/**
 * Intercepting route for order detail modal.
 *
 * Learning point: (.) prefix means "intercept one level up".
 * When navigating from /orders to /orders/abc via a <Link>,
 * this intercepting route renders the order as a MODAL overlay
 * instead of a full page. Direct URL access still shows the
 * full page. This creates the Instagram-like photo modal UX.
 *
 * @modal is a parallel route slot — it renders alongside the
 * orders list page, not replacing it.
 */
interface Props {
  params: Promise<{ id: string }>;
}

export default function OrderModal({ params }: Props) {
  const { id } = use(params);
  const router = useRouter();
  const { data: order } = useOrder(id);

  return (
    <Modal isOpen={true} onClose={() => router.back()} title={`Order ${id.slice(0, 8)}`} size="lg">
      {order ? (
        <div>
          <p>Status: {order.status}</p>
          <p>Items: {order.items?.length ?? 0}</p>
          <p>Created: {new Date(order.createdAt).toLocaleString()}</p>
        </div>
      ) : (
        <p>Loading...</p>
      )}
    </Modal>
  );
}
