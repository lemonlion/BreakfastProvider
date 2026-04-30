import { getStaff, createStaffMember, deleteStaffMember } from '@/lib/api/endpoints/staff';
import { server } from '@/test-utils/msw/server';
import { http, HttpResponse } from 'msw';

describe('Staff API', () => {
  it('should return staff members', async () => {
    const result = await getStaff();
    expect(result).toHaveLength(1);
    expect(result[0].name).toBe('Chef Alice');
  });

  it('should create a staff member', async () => {
    const result = await createStaffMember({
      name: 'Bob',
      role: 'Server',
      email: 'bob@breakfast.com',
      isActive: true,
    });
    expect(result).toBeDefined();
  });

  it('should delete a staff member (204)', async () => {
    await expect(deleteStaffMember(1)).resolves.toBeUndefined();
  });
});
