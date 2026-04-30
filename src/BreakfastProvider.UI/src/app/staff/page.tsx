'use client';

import { useMemo, Suspense } from 'react';
import { useSearchParams, useRouter, usePathname } from 'next/navigation';
import { useTransition } from 'react';
import { type ColumnDef } from '@tanstack/react-table';
import { DataTable } from '@/components/ui/DataTable/DataTable';
import { Button } from '@/components/ui/Button/Button';
import { Badge } from '@/components/ui/Badge/Badge';
import { Card } from '@/components/ui/Card/Card';
import { PageHeader } from '@/components/layout/PageHeader/PageHeader';

/**
 * Staff page with URL-driven role filter and row expansion.
 *
 * Learning points:
 * - URL-driven role filter (/staff?role=Chef) using same pattern as orders
 * - DataTable with row expansion showing staff details
 * - renderSubRow callback displaying shift schedule, contact info
 */

interface StaffMember {
  id: string;
  name: string;
  role: string;
  email: string;
  phone: string;
  shift: string;
  status: string;
}

const ROLES = ['All', 'Chef', 'Server', 'Host', 'Manager', 'Barista'];

export default function StaffPage() {
  return (
    <Suspense fallback={<p>Loading...</p>}>
      <StaffPageContent />
    </Suspense>
  );
}

function StaffPageContent() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();
  const [isPending, startTransition] = useTransition();

  const roleFilter = searchParams.get('role') ?? undefined;

  // Placeholder data — replace with useStaff() hook when available
  const staff: StaffMember[] = [];

  const setSearchParam = (key: string, value: string | null) => {
    const params = new URLSearchParams(searchParams.toString());
    if (value === null) {
      params.delete(key);
    } else {
      params.set(key, value);
    }
    startTransition(() => {
      router.push(`${pathname}?${params.toString()}` as any);
    });
  };

  const columns = useMemo<ColumnDef<StaffMember, unknown>[]>(
    () => [
      { accessorKey: 'name', header: 'Name' },
      {
        accessorKey: 'role',
        header: 'Role',
        cell: ({ row }) => <Badge variant="info">{row.original.role}</Badge>,
      },
      { accessorKey: 'shift', header: 'Shift' },
      {
        accessorKey: 'status',
        header: 'Status',
        cell: ({ row }) => (
          <Badge variant={row.original.status === 'Active' ? 'success' : 'warning'}>
            {row.original.status}
          </Badge>
        ),
      },
    ],
    [],
  );

  return (
    <div>
      <PageHeader title="Staff" description="Manage kitchen and service staff" />

      {/* Role filter tabs */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
        {ROLES.map((role) => (
          <Button
            key={role}
            variant={roleFilter === (role === 'All' ? undefined : role) ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setSearchParam('role', role === 'All' ? null : role)}
          >
            {role}
          </Button>
        ))}
      </div>

      <DataTable
        data={staff}
        columns={columns}
        enableGlobalFilter
        renderSubRow={(row) => (
          <Card variant="flat" style={{ padding: 16 }}>
            <h4>Contact Details</h4>
            <p>Email: {row.original.email}</p>
            <p>Phone: {row.original.phone}</p>
            <h4 style={{ marginTop: 8 }}>Shift Schedule</h4>
            <p>{row.original.shift}</p>
          </Card>
        )}
      />
    </div>
  );
}
