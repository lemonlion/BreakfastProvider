import { expect } from '@jest/globals';

expect.extend({
  toHaveStatus(received: { status: string }, expected: string) {
    const pass = received.status === expected;
    return {
      pass,
      message: () =>
        `Expected status to be "${expected}", but received "${received.status}"`,
    };
  },

  toBeWithinRange(received: number, floor: number, ceiling: number) {
    const pass = received >= floor && received <= ceiling;
    return {
      pass,
      message: () =>
        `Expected ${received} to be within range ${floor} - ${ceiling}`,
    };
  },
});

// TypeScript module augmentation for custom matchers
declare global {
  namespace jest {
    interface Matchers<R> {
      toHaveStatus(expected: string): R;
      toBeWithinRange(floor: number, ceiling: number): R;
    }
  }
}
