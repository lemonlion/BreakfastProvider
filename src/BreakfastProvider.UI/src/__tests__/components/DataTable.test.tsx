import { render, screen, within } from '@/test-utils/render';
import userEvent from '@testing-library/user-event';
import { DataTable } from '@/components/ui/DataTable/DataTable';

const sampleData = [
  { id: '1', name: 'Alpha', value: 10 },
  { id: '2', name: 'Beta', value: 20 },
  { id: '3', name: 'Gamma', value: 5 },
];

const columns = [
  { accessorKey: 'name' as const, header: 'Name' },
  { accessorKey: 'value' as const, header: 'Value' },
];

describe('DataTable', () => {
  it('should render all rows', () => {
    render(<DataTable data={sampleData} columns={columns} />);
    expect(screen.getAllByRole('row')).toHaveLength(4); // 1 header + 3 data
  });

  it('should sort by column when header is clicked', async () => {
    const user = userEvent.setup();
    render(<DataTable data={sampleData} columns={columns} />);

    await user.click(screen.getByText('Name'));

    const rows = screen.getAllByRole('row');
    const firstDataRow = rows[1];
    expect(within(firstDataRow).getByText('Alpha')).toBeInTheDocument();
  });

  it('should filter rows with global filter', async () => {
    const user = userEvent.setup();
    render(<DataTable data={sampleData} columns={columns} enableGlobalFilter />);

    await user.type(screen.getByPlaceholderText('Search all columns...'), 'Beta');

    expect(screen.getAllByRole('row')).toHaveLength(2); // header + 1 match
  });

  it('should toggle column visibility', async () => {
    const user = userEvent.setup();
    render(<DataTable data={sampleData} columns={columns} />);

    // Open column toggle
    await user.click(screen.getByText('Columns'));

    // Uncheck 'value' column
    const valueCheckbox = screen.getByLabelText('value');
    await user.click(valueCheckbox);

    // Value column should be hidden
    expect(screen.queryByText('Value')).not.toBeInTheDocument();
  });

  it('should call onSelectionChange when rows are selected', async () => {
    const onSelectionChange = jest.fn();
    const user = userEvent.setup();

    // TanStack Table requires explicit column definition for row selection checkboxes
    const selectionColumns = [
      {
        id: 'select',
        header: ({ table }: { table: { getToggleAllRowsSelectedHandler: () => (e: unknown) => void; getIsAllRowsSelected: () => boolean } }) => (
          <input
            type="checkbox"
            checked={table.getIsAllRowsSelected()}
            onChange={table.getToggleAllRowsSelectedHandler()}
          />
        ),
        cell: ({ row }: { row: { getToggleSelectedHandler: () => (e: unknown) => void; getIsSelected: () => boolean } }) => (
          <input
            type="checkbox"
            checked={row.getIsSelected()}
            onChange={row.getToggleSelectedHandler()}
          />
        ),
      },
      ...columns,
    ];

    render(
      <DataTable
        data={sampleData}
        columns={selectionColumns}
        enableRowSelection
        onSelectionChange={onSelectionChange}
      />,
    );

    // Find checkboxes within table rows (not column toggle checkboxes in toolbar)
    const table = screen.getByRole('table');
    const rowCheckboxes = within(table).getAllByRole('checkbox');
    // rowCheckboxes[0] is "select all" header, [1+] are data rows
    await user.click(rowCheckboxes[1]);

    expect(onSelectionChange).toHaveBeenCalled();
  });
});
