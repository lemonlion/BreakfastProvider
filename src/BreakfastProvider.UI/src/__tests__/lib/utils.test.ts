import { getNextStatuses, getStatusColor, toCSV, formatDate } from '@/lib/utils';

describe('utils', () => {
  describe('getNextStatuses()', () => {
    it.each`
      current        | expected
      ${'Created'}   | ${['Preparing', 'Cancelled']}
      ${'Preparing'} | ${['Ready']}
      ${'Ready'}     | ${['Completed']}
      ${'Completed'} | ${[]}
      ${'Cancelled'} | ${[]}
    `('from $current should return $expected', ({ current, expected }) => {
      expect(getNextStatuses(current)).toEqual(expected);
    });
  });

  describe('getStatusColor()', () => {
    it.each([
      ['Healthy', 'success'],
      ['Degraded', 'warning'],
      ['Unhealthy', 'error'],
      ['Unknown', 'neutral'],
    ])('should map %s to %s badge variant', (status, expected) => {
      expect(getStatusColor(status)).toBe(expected);
    });
  });

  describe('toCSV()', () => {
    it('should convert array of objects to CSV string', () => {
      const data = [
        { name: 'Pancake', count: 10 },
        { name: 'Waffle', count: 5 },
      ];
      const csv = toCSV(data, ['name', 'count']);
      expect(csv).toContain('name,count');
      expect(csv).toContain('Pancake,10');
      expect(csv).toContain('Waffle,5');
    });

    it('should handle empty array', () => {
      const csv = toCSV([], ['name']);
      expect(csv).toBe('name');
    });
  });

  describe('formatDate()', () => {
    it('should format ISO string to readable date', () => {
      const result = formatDate('2024-01-15T10:30:00Z');
      expect(result).toBeDefined();
      expect(typeof result).toBe('string');
    });
  });
});
