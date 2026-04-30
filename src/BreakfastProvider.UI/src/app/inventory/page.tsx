'use client';

import { useState, useMemo } from 'react';
import { type ColumnDef } from '@tanstack/react-table';
import { DataTable } from '@/components/ui/DataTable/DataTable';
import { Button } from '@/components/ui/Button/Button';
import { Modal } from '@/components/ui/Modal/Modal';
import { PageHeader } from '@/components/layout/PageHeader/PageHeader';
import { useToast } from '@/components/ui/Toast/Toast';
import { downloadFile, toCSV } from '@/lib/utils';

/**
 * Inventory page with global filter and CSV export.
 *
 * Learning points:
 * - DataTable with enableGlobalFilter for full-text search across all columns
 * - CSV export button using toCSV() + downloadFile() utility
 * - CRUD modal for creating/editing inventory items
 */

interface InventoryItem {
  id: string;
  name: string;
  category: string;
  quantity: number;
  unit: string;
  reorderLevel: number;
  lastRestocked: string;
}

export default function InventoryPage() {
  const { addToast } = useToast();
  const [isCreateOpen, setIsCreateOpen] = useState(false);

  // Placeholder data — replace with useInventory() hook when available
  const items: InventoryItem[] = [];

  const handleExportCSV = () => {
    if (items.length === 0) {
      addToast('No data to export', 'warning');
      return;
    }
    downloadFile(toCSV(items as unknown as Record<string, unknown>[], ['id', 'name', 'category', 'quantity', 'unit', 'reorderLevel']), 'inventory.csv', 'text/csv');
    addToast('CSV exported', 'success');
  };

  const columns = useMemo<ColumnDef<InventoryItem, unknown>[]>(
    () => [
      { accessorKey: 'name', header: 'Name' },
      { accessorKey: 'category', header: 'Category' },
      {
        accessorKey: 'quantity',
        header: 'Quantity',
        cell: ({ row }) => `${row.original.quantity} ${row.original.unit}`,
      },
      { accessorKey: 'reorderLevel', header: 'Reorder Level' },
      {
        accessorKey: 'lastRestocked',
        header: 'Last Restocked',
        cell: ({ row }) => new Date(row.original.lastRestocked).toLocaleDateString(),
      },
      {
        id: 'actions',
        cell: () => (
          <Button variant="ghost" size="sm">
            Edit
          </Button>
        ),
      },
    ],
    [],
  );

  return (
    <div>
      <PageHeader
        title="Inventory"
        description="Track ingredient stock levels"
        actions={
          <div style={{ display: 'flex', gap: 8 }}>
            <Button variant="secondary" onClick={handleExportCSV}>
              Export CSV
            </Button>
            <Button onClick={() => setIsCreateOpen(true)}>Add Item</Button>
          </div>
        }
      />

      <DataTable
        data={items}
        columns={columns}
        enableGlobalFilter
      />

      <Modal isOpen={isCreateOpen} onClose={() => setIsCreateOpen(false)} title="Add Inventory Item">
        <p>Form with name, category, quantity, unit, reorder level fields</p>
      </Modal>
    </div>
  );
}
