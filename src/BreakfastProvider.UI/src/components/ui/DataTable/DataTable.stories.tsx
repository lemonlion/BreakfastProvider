import type { Meta, StoryObj } from '@storybook/react';
import { within, userEvent, expect } from '@storybook/test';
import { DataTable } from './DataTable';

const sampleData = [
  { id: '1', name: 'Chocolate Chip', category: 'Sweet', price: 1.50, available: true },
  { id: '2', name: 'Maple Syrup', category: 'Sweet', price: 0.75, available: true },
  { id: '3', name: 'Bacon Bits', category: 'Savoury', price: 2.00, available: false },
  { id: '4', name: 'Fresh Berries', category: 'Fruit', price: 1.25, available: true },
  { id: '5', name: 'Whipped Cream', category: 'Sweet', price: 0.50, available: true },
];

const columns = [
  { accessorKey: 'name', header: 'Name' },
  { accessorKey: 'category', header: 'Category' },
  { accessorKey: 'price', header: 'Price', cell: ({ row }: any) => `£${row.original.price.toFixed(2)}` },
  { accessorKey: 'available', header: 'Available', cell: ({ row }: any) => row.original.available ? '✅' : '❌' },
];

const meta: Meta<typeof DataTable> = {
  title: 'UI/DataTable',
  component: DataTable,
  tags: ['autodocs'],
  decorators: [(Story) => <div style={{ maxWidth: 800 }}><Story /></div>],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: { data: sampleData, columns },
};

export const WithGlobalFilter: Story = {
  args: { data: sampleData, columns, enableGlobalFilter: true },
};

export const WithRowSelection: Story = {
  args: { data: sampleData, columns, enableRowSelection: true },
};

/**
 * Interaction test — sorting by clicking a column header.
 *
 * Learning point: play() functions can simulate complex user workflows.
 * This test clicks the "Name" header to sort, then verifies the first
 * row changed.
 */
export const SortInteraction: Story = {
  args: { data: sampleData, columns },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const nameHeader = canvas.getByText('Name');

    // Click to sort ascending
    await userEvent.click(nameHeader);

    // First row should now be Bacon Bits (alphabetical)
    const cells = canvas.getAllByRole('cell');
    await expect(cells[0]).toHaveTextContent('Bacon Bits');
  },
};

/** Filter interaction test */
export const FilterInteraction: Story = {
  args: { data: sampleData, columns, enableGlobalFilter: true },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const filterInput = canvas.getByPlaceholderText('Search all columns...');

    await userEvent.type(filterInput, 'Maple');

    // Should only show Maple Syrup row
    const rows = canvas.getAllByRole('row');
    // Header row + 1 data row = 2 rows total
    await expect(rows).toHaveLength(2);
  },
};
