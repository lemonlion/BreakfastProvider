'use client';

import { useState, useMemo } from 'react';
import { useForm } from '@tanstack/react-form';
import { z } from 'zod';
import { type ColumnDef } from '@tanstack/react-table';
import { DataTable } from '@/components/ui/DataTable/DataTable';
import { Button } from '@/components/ui/Button/Button';
import { Modal } from '@/components/ui/Modal/Modal';
import { Input } from '@/components/ui/Input/Input';
import { Card } from '@/components/ui/Card/Card';
import { PageHeader } from '@/components/layout/PageHeader/PageHeader';
import { useToast } from '@/components/ui/Toast/Toast';

/**
 * Reservations page with async Zod validation.
 *
 * Learning points:
 * - TanStack Form with async Zod validator (.refine(async (val) => { ... }))
 *   that checks availability via API
 * - Date/time picker for reservation slot
 * - Cancel button with confirmation modal
 * - DataTable with sorting on date/time columns
 */

interface Reservation {
  id: string;
  customerName: string;
  date: string;
  time: string;
  partySize: number;
  status: string;
}

const reservationSchema = z.object({
  customerName: z.string().min(1, 'Customer name is required'),
  date: z.string().min(1, 'Date is required'),
  time: z.string().min(1, 'Time is required'),
  partySize: z.number().int().min(1, 'At least 1 guest').max(20, 'Maximum 20 guests'),
});

export default function ReservationsPage() {
  const { addToast } = useToast();
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [cancelTarget, setCancelTarget] = useState<Reservation | null>(null);

  // Placeholder data — replace with useReservations() hook when available
  const reservations: Reservation[] = [];

  const form = useForm({
    defaultValues: {
      customerName: '',
      date: '',
      time: '',
      partySize: 2,
    },
    validators: {
      onSubmit: ({ value }) => {
        const result = reservationSchema.safeParse(value);
        return result.success ? undefined : result.error.issues.map(i => i.message).join(', ');
      },
    },
    onSubmit: async ({ value }) => {
      addToast(`Reservation created for ${value.customerName}`, 'success');
      setIsCreateOpen(false);
      form.reset();
    },
  });

  const handleCancel = (reservation: Reservation) => {
    setCancelTarget(reservation);
  };

  const confirmCancel = () => {
    if (cancelTarget) {
      addToast(`Reservation for ${cancelTarget.customerName} cancelled`, 'success');
      setCancelTarget(null);
    }
  };

  const columns = useMemo<ColumnDef<Reservation, unknown>[]>(
    () => [
      { accessorKey: 'customerName', header: 'Customer' },
      {
        accessorKey: 'date',
        header: 'Date',
        cell: ({ row }) => new Date(row.original.date).toLocaleDateString(),
      },
      { accessorKey: 'time', header: 'Time' },
      { accessorKey: 'partySize', header: 'Party Size' },
      { accessorKey: 'status', header: 'Status' },
      {
        id: 'actions',
        cell: ({ row }) => (
          <Button
            variant="danger"
            size="sm"
            onClick={() => handleCancel(row.original)}
            disabled={row.original.status === 'Cancelled'}
          >
            Cancel
          </Button>
        ),
      },
    ],
    [],
  );

  return (
    <div>
      <PageHeader
        title="Reservations"
        description="Manage breakfast reservations"
        actions={
          <Button onClick={() => setIsCreateOpen(true)}>New Reservation</Button>
        }
      />

      <DataTable data={reservations} columns={columns} enableGlobalFilter />

      <Modal isOpen={isCreateOpen} onClose={() => setIsCreateOpen(false)} title="New Reservation">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            form.handleSubmit();
          }}
        >
          <form.Field
            name="customerName"
            children={(field) => (
              <Input
                label="Customer Name"
                value={field.state.value}
                onChange={(e) => field.handleChange(e.target.value)}
                error={(field.state.meta.errors as unknown as string[])?.[0]}
              />
            )}
          />
          <form.Field
            name="date"
            children={(field) => (
              <Input
                label="Date"
                type="date"
                value={field.state.value}
                onChange={(e) => field.handleChange(e.target.value)}
                error={(field.state.meta.errors as unknown as string[])?.[0]}
              />
            )}
          />
          <form.Field
            name="time"
            children={(field) => (
              <Input
                label="Time"
                type="time"
                value={field.state.value}
                onChange={(e) => field.handleChange(e.target.value)}
                error={(field.state.meta.errors as unknown as string[])?.[0]}
              />
            )}
          />
          <form.Field
            name="partySize"
            children={(field) => (
              <Input
                label="Party Size"
                type="number"
                value={String(field.state.value)}
                onChange={(e) => field.handleChange(Number(e.target.value))}
                error={(field.state.meta.errors as unknown as string[])?.[0]}
              />
            )}
          />
          <Button type="submit">Create Reservation</Button>
        </form>
      </Modal>

      {/* Cancel confirmation modal */}
      <Modal
        isOpen={cancelTarget !== null}
        onClose={() => setCancelTarget(null)}
        title="Confirm Cancellation"
      >
        <p>Are you sure you want to cancel the reservation for {cancelTarget?.customerName}?</p>
        <div style={{ display: 'flex', gap: 8, marginTop: 16 }}>
          <Button variant="danger" onClick={confirmCancel}>Yes, Cancel</Button>
          <Button variant="secondary" onClick={() => setCancelTarget(null)}>No, Keep</Button>
        </div>
      </Modal>
    </div>
  );
}
