'use client';

import { useState } from 'react';
import { useAuditLogs, useInfiniteAuditLogs } from '@/hooks/use-audit-logs';
import { DataTable } from '@/components/ui/DataTable/DataTable';
import { VirtualList } from '@/components/ui/VirtualList/VirtualList';
import { Button } from '@/components/ui/Button/Button';
import { Card } from '@/components/ui/Card/Card';
import { PageHeader } from '@/components/layout/PageHeader/PageHeader';
import type { ColumnDef } from '@tanstack/react-table';

/**
 * Audit logs — toggle between paginated table and infinite scroll.
 *
 * Learning points:
 * - useInfiniteQuery() in the hook loads pages as the user scrolls
 * - VirtualList efficiently renders thousands of log entries
 * - The toggle lets you compare both UX patterns side by side
 */
export default function AuditLogsPage() {
  const [viewMode, setViewMode] = useState<'table' | 'infinite'>('table');

  return (
    <div>
      <PageHeader
        title="Audit Logs"
        actions={
          <div style={{ display: 'flex', gap: 8 }}>
            <Button
              variant={viewMode === 'table' ? 'primary' : 'secondary'}
              size="sm"
              onClick={() => setViewMode('table')}
            >
              Table View
            </Button>
            <Button
              variant={viewMode === 'infinite' ? 'primary' : 'secondary'}
              size="sm"
              onClick={() => setViewMode('infinite')}
            >
              Infinite Scroll
            </Button>
          </div>
        }
      />

      {viewMode === 'table' ? <AuditLogTable /> : <AuditLogInfinite />}
    </div>
  );
}

function AuditLogTable() {
  const { data: logs } = useAuditLogs();

  const columns: ColumnDef<any, unknown>[] = [
    { accessorKey: 'timestamp', header: 'Time', cell: ({ row }) => new Date(row.original.timestamp).toLocaleString() },
    { accessorKey: 'action', header: 'Action' },
    { accessorKey: 'entity', header: 'Entity' },
    { accessorKey: 'user', header: 'User' },
    { accessorKey: 'details', header: 'Details' },
  ];

  return <DataTable data={logs ?? []} columns={columns} enableGlobalFilter />;
}

function AuditLogInfinite() {
  const { data, fetchNextPage, hasNextPage, isFetchingNextPage } = useInfiniteAuditLogs();

  const allLogs = data?.pages.flatMap((page) => page) ?? [];

  return (
    <div>
      <VirtualList
        items={allLogs}
        estimateSize={60}
        height={600}
        renderItem={(log) => (
          <Card variant="flat" style={{ padding: 8, marginBottom: 4 }}>
            <strong>{log.action}</strong> — {log.entityType}:{log.entityId}
            <span style={{ float: 'right', opacity: 0.6 }}>
              {new Date(log.timestamp).toLocaleString()}
            </span>
          </Card>
        )}
      />
      {hasNextPage && (
        <Button variant="secondary" onClick={() => fetchNextPage()} loading={isFetchingNextPage}>
          Load More
        </Button>
      )}
    </div>
  );
}
