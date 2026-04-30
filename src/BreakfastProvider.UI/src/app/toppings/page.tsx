'use client';

import { useState, useMemo } from 'react';
import { type ColumnDef } from '@tanstack/react-table';
import { useToppings, useCreateTopping, useDeleteTopping } from '@/hooks/use-toppings';
import { DataTable } from '@/components/ui/DataTable/DataTable';
import { Button } from '@/components/ui/Button/Button';
import { Modal } from '@/components/ui/Modal/Modal';
import { Input } from '@/components/ui/Input/Input';
import { PageHeader } from '@/components/layout/PageHeader/PageHeader';
import { useToast } from '@/components/ui/Toast/Toast';
import type { ToppingResponse } from '@/lib/api/types';

/**
 * Toppings page — CRUD with row selection for bulk delete.
 *
 * Learning points:
 * - Row selection enables selecting multiple rows for bulk actions
 * - Column visibility lets users hide/show columns via the dropdown
 * - Optimistic delete in the mutation hook removes from cache immediately
 */
export default function ToppingsPage() {
  const { data: toppings } = useToppings();
  const createTopping = useCreateTopping();
  const deleteTopping = useDeleteTopping();
  const { addToast } = useToast();
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [selectedToppings, setSelectedToppings] = useState<ToppingResponse[]>([]);

  const handleBulkDelete = async () => {
    for (const topping of selectedToppings) {
      await deleteTopping.mutateAsync(topping.toppingId);
    }
    addToast(`Deleted ${selectedToppings.length} toppings`, 'success');
    setSelectedToppings([]);
  };

  const columns = useMemo<ColumnDef<ToppingResponse, unknown>[]>(
    () => [
      { accessorKey: 'name', header: 'Name' },
      { accessorKey: 'category', header: 'Category' },
      {
        id: 'actions',
        cell: ({ row }) => (
          <Button variant="danger" size="sm" onClick={() => deleteTopping.mutate(row.original.toppingId)}>
            Delete
          </Button>
        ),
      },
    ],
    [deleteTopping],
  );

  return (
    <div>
      <PageHeader
        title="Toppings"
        description="Manage breakfast toppings"
        actions={
          <div style={{ display: 'flex', gap: 8 }}>
            {selectedToppings.length > 0 && (
              <Button variant="danger" onClick={handleBulkDelete}>
                Delete Selected ({selectedToppings.length})
              </Button>
            )}
            <Button onClick={() => setIsCreateOpen(true)}>Add Topping</Button>
          </div>
        }
      />

      <DataTable
        data={toppings ?? []}
        columns={columns}
        enableRowSelection
        enableGlobalFilter
        onSelectionChange={setSelectedToppings}
      />

      <Modal isOpen={isCreateOpen} onClose={() => setIsCreateOpen(false)} title="Add Topping">
        {/* Create topping form — uses TanStack Form like pancakes page */}
        <p>Form with name, category, price fields</p>
      </Modal>
    </div>
  );
}
