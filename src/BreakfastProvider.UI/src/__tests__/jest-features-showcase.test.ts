/**
 * This file demonstrates advanced Jest features for learning purposes.
 * Each section showcases a different Jest capability.
 */

// Retries flaky tests automatically
jest.retryTimes(2, { logErrorsBeforeRetry: true });

describe('Jest Features Showcase', () => {
  // --- Fake Timers ---
  describe('jest.useFakeTimers()', () => {
    beforeEach(() => jest.useFakeTimers());
    afterEach(() => jest.useRealTimers());

    it('should advance timers to test setTimeout', () => {
      const callback = jest.fn();
      setTimeout(callback, 1000);

      expect(callback).not.toHaveBeenCalled();
      jest.advanceTimersByTime(1000);
      expect(callback).toHaveBeenCalledTimes(1);
    });

    it('should run all pending timers', () => {
      const callback = jest.fn();
      setTimeout(callback, 5000);
      setTimeout(callback, 10000);

      jest.runAllTimers();
      expect(callback).toHaveBeenCalledTimes(2);
    });
  });

  // --- Asymmetric Matchers ---
  describe('Asymmetric Matchers', () => {
    it('should use expect.objectContaining for partial matching', () => {
      const order = {
        id: 'abc-123',
        status: 'Created',
        items: [{ name: 'Pancake', quantity: 2 }],
        createdAt: '2024-01-15T10:00:00Z',
      };

      expect(order).toEqual(
        expect.objectContaining({
          status: 'Created',
          items: expect.arrayContaining([
            expect.objectContaining({ name: 'Pancake' }),
          ]),
        }),
      );
    });

    it('should use expect.any() for type checking', () => {
      expect({ id: 'abc', count: 5 }).toEqual({
        id: expect.any(String),
        count: expect.any(Number),
      });
    });

    it('should use expect.stringMatching for pattern matching', () => {
      expect('order-abc-123').toEqual(expect.stringMatching(/^order-/));
    });
  });

  // --- Mock Functions ---
  describe('Mock Functions', () => {
    it('should track calls with jest.fn()', () => {
      const mockFn = jest.fn((x: number) => x * 2);

      mockFn(1);
      mockFn(2);
      mockFn(3);

      expect(mockFn).toHaveBeenCalledTimes(3);
      expect(mockFn.mock.calls).toEqual([[1], [2], [3]]);
      expect(mockFn.mock.results).toEqual([
        { type: 'return', value: 2 },
        { type: 'return', value: 4 },
        { type: 'return', value: 6 },
      ]);
    });

    it('should chain implementations with mockReturnValueOnce', () => {
      const mockFn = jest.fn()
        .mockReturnValueOnce('first')
        .mockReturnValueOnce('second')
        .mockReturnValue('default');

      expect(mockFn()).toBe('first');
      expect(mockFn()).toBe('second');
      expect(mockFn()).toBe('default');
      expect(mockFn()).toBe('default');
    });
  });

  // --- Error Testing ---
  describe('Error testing', () => {
    it('should test thrown errors with toThrow', () => {
      const throwError = () => {
        throw new Error('Something broke');
      };
      expect(throwError).toThrow('Something broke');
      expect(throwError).toThrow(Error);
    });

    it('should test async errors with rejects', async () => {
      const asyncError = async () => {
        throw new Error('Async failure');
      };
      await expect(asyncError()).rejects.toThrow('Async failure');
    });
  });
});
