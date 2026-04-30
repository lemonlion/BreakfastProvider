'use client';

import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  getExpandedRowModel,
  flexRender,
  type ColumnDef,
  type SortingState,
  type ColumnFiltersState,
  type VisibilityState,
  type ExpandedState,
  type RowSelectionState,
  type Row,
} from '@tanstack/react-table';
import { useState, type ReactNode } from 'react';
import { Button } from '../Button/Button';
import * as styles from './DataTable.css';

/**
 * Generic DataTable component wrapping TanStack Table.
 *
 * Learning points:
 * - Type parameter <TData> makes the table generic over any data shape
 * - TanStack Table is headless — it manages state, we render the HTML
 * - Features enabled: sorting, global filter, column visibility,
 *   row selection, row expansion, pagination
 */
interface DataTableProps<TData> {
  data: TData[];
  columns: ColumnDef<TData, unknown>[];
  /** Render function for expanded row content */
  renderSubRow?: (row: Row<TData>) => ReactNode;
  /** Enable row selection checkboxes */
  enableRowSelection?: boolean;
  /** Callback when selection changes */
  onSelectionChange?: (selectedRows: TData[]) => void;
  /** Enable global text filter */
  enableGlobalFilter?: boolean;
  /** External pagination (server-driven) */
  pageCount?: number;
  /** Current page (0-indexed) for external pagination */
  pageIndex?: number;
  /** Page size for external pagination */
  pageSize?: number;
  /** Callback for page changes (external pagination) */
  onPageChange?: (page: number) => void;
}

export function DataTable<TData>({
  data,
  columns,
  renderSubRow,
  enableRowSelection = false,
  onSelectionChange,
  enableGlobalFilter = false,
  pageCount,
  pageIndex,
  pageSize = 10,
  onPageChange,
}: DataTableProps<TData>) {
  const [sorting, setSorting] = useState<SortingState>([]);
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([]);
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({});
  const [rowSelection, setRowSelection] = useState<RowSelectionState>({});
  const [expanded, setExpanded] = useState<ExpandedState>({});
  const [globalFilter, setGlobalFilter] = useState('');

  const isExternalPagination = pageCount !== undefined;

  const table = useReactTable({
    data,
    columns,
    state: {
      sorting,
      columnFilters,
      columnVisibility,
      rowSelection,
      expanded,
      globalFilter,
      ...(isExternalPagination && {
        pagination: { pageIndex: pageIndex ?? 0, pageSize },
      }),
    },
    // Feature models — each enables a table capability
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    getExpandedRowModel: getExpandedRowModel(),
    // State setters
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    onColumnVisibilityChange: setColumnVisibility,
    onRowSelectionChange: (updater) => {
      setRowSelection(updater);
      if (onSelectionChange) {
        const newSelection = typeof updater === 'function' ? updater(rowSelection) : updater;
        const selectedData = Object.keys(newSelection)
          .filter((key) => newSelection[key])
          .map((key) => data[parseInt(key)]);
        onSelectionChange(selectedData);
      }
    },
    onExpandedChange: setExpanded,
    onGlobalFilterChange: setGlobalFilter,
    enableRowSelection,
    manualPagination: isExternalPagination,
    pageCount: pageCount ?? undefined,
  });

  return (
    <div className={styles.wrapper}>
      {/* Toolbar: global filter + column visibility */}
      <div className={styles.toolbar}>
        {enableGlobalFilter && (
          <input
            className={styles.globalFilter}
            placeholder="Search all columns..."
            value={globalFilter}
            onChange={(e) => setGlobalFilter(e.target.value)}
          />
        )}
        {/* Column visibility dropdown */}
        <div className={styles.columnToggle}>
          <details>
            <summary className={styles.columnToggleButton}>Columns</summary>
            <div className={styles.columnToggleList}>
              {table.getAllLeafColumns().map((column) => (
                <label key={column.id} className={styles.columnToggleItem}>
                  <input
                    type="checkbox"
                    checked={column.getIsVisible()}
                    onChange={column.getToggleVisibilityHandler()}
                  />
                  {column.id}
                </label>
              ))}
            </div>
          </details>
        </div>
      </div>

      {/* Table */}
      <div className={styles.tableContainer}>
        <table className={styles.table}>
          <thead>
            {table.getHeaderGroups().map((headerGroup) => (
              <tr key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <th
                    key={header.id}
                    className={styles.th}
                    onClick={header.column.getToggleSortingHandler()}
                    style={{ cursor: header.column.getCanSort() ? 'pointer' : 'default' }}
                  >
                    {flexRender(header.column.columnDef.header, header.getContext())}
                    {/* Sorting indicator */}
                    {{ asc: ' ↑', desc: ' ↓' }[header.column.getIsSorted() as string] ?? ''}
                  </th>
                ))}
              </tr>
            ))}
          </thead>
          <tbody>
            {table.getRowModel().rows.map((row) => (
              <>
                <tr key={row.id} className={styles.tr}>
                  {row.getVisibleCells().map((cell) => (
                    <td key={cell.id} className={styles.td}>
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </td>
                  ))}
                </tr>
                {/* Expanded sub-row */}
                {row.getIsExpanded() && renderSubRow && (
                  <tr key={`${row.id}-expanded`}>
                    <td colSpan={row.getVisibleCells().length} className={styles.expandedRow}>
                      {renderSubRow(row)}
                    </td>
                  </tr>
                )}
              </>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      <div className={styles.pagination}>
        <span className={styles.pageInfo}>
          Page {table.getState().pagination.pageIndex + 1} of {table.getPageCount()}
        </span>
        <div className={styles.pageButtons}>
          <Button
            variant="secondary"
            size="sm"
            onClick={() =>
              isExternalPagination
                ? onPageChange?.(table.getState().pagination.pageIndex - 1)
                : table.previousPage()
            }
            disabled={!table.getCanPreviousPage()}
          >
            Previous
          </Button>
          <Button
            variant="secondary"
            size="sm"
            onClick={() =>
              isExternalPagination
                ? onPageChange?.(table.getState().pagination.pageIndex + 1)
                : table.nextPage()
            }
            disabled={!table.getCanNextPage()}
          >
            Next
          </Button>
        </div>
      </div>
    </div>
  );
}
