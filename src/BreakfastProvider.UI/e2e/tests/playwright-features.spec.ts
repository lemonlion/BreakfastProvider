import { test, expect } from '../fixtures';

/**
 * Playwright features showcase — demonstrates advanced capabilities.
 */
test.describe('Playwright Features', () => {
  // --- test.fixme() ---
  test.fixme('should handle WebSocket connections', async () => {
    /**
     * Learning point: test.fixme() marks a known-broken test.
     * It appears in the report as "fixme" rather than "skipped".
     * Unlike test.skip(), it signals intent to fix.
     */
  });

  // --- test.skip() ---
  test.skip(process.env.CI === 'true', 'Skipping in CI — requires local API');
  test('should connect to local API directly', async ({ page }) => {
    // This test only runs locally
    await page.goto('/health');
  });

  // --- test.describe.serial ---
  /**
   * Learning point: serial mode runs tests in order within this describe.
   * If one fails, the rest are skipped. Useful for dependent test sequences.
   */
  // test.describe.serial('Order lifecycle', () => {
  //   Used when tests depend on each other (create → update → delete)
  // });

  // --- expect.poll() ---
  test('should eventually load data (polling assertion)', async ({ page }) => {
    await page.goto('/');

    /**
     * Learning point: expect.poll() repeatedly evaluates a function
     * until the assertion passes or times out. Unlike waitFor, it can
     * check any arbitrary condition, not just DOM visibility.
     */
    await expect.poll(
      async () => {
        const count = await page.locator('table tbody tr').count();
        return count;
      },
      { timeout: 10_000, intervals: [500, 1000, 2000] },
    ).toBeGreaterThan(0);
  });

  // --- Network interception ---
  test('should handle rate limiting (429)', { tag: '@error' }, async ({ page }) => {
    /**
     * Learning point: Route interception can simulate any HTTP scenario.
     * Testing 429 (rate limit) ensures the UI handles back-pressure correctly.
     */
    await page.route('**/orders', (route) => {
      route.fulfill({
        status: 429,
        headers: { 'Retry-After': '5' },
        body: JSON.stringify({ title: 'Too Many Requests' }),
      });
    });

    await page.goto('/orders');
    // UI should show rate limit message or error state
  });

  // --- Multiple pages (tabs) ---
  test('should handle multiple browser tabs', async ({ context }) => {
    /**
     * Learning point: context.newPage() creates a new tab in the same
     * browser context (shares cookies, storage). Useful for testing
     * multi-tab scenarios like order updates visible across tabs.
     */
    const page1 = await context.newPage();
    const page2 = await context.newPage();

    await page1.goto('/orders');
    await page2.goto('/orders');

    // Both pages should show the same data
    const count1 = await page1.locator('table tbody tr').count();
    const count2 = await page2.locator('table tbody tr').count();
    expect(count1).toBe(count2);

    await page1.close();
    await page2.close();
  });
});
