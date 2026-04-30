import { test, expect } from '../fixtures';

test.describe('Health', () => {
  test('should display all dependency statuses', { tag: '@smoke' }, async ({ healthPage, page }) => {
    await healthPage.goto();

    await expect(page.getByText('CowService')).toBeVisible();
    await expect(page.getByText('GoatService')).toBeVisible();
    await expect(page.getByText('CosmosDB')).toBeVisible();
  });

  test('should show overall status badge', async ({ healthPage, page }) => {
    await healthPage.goto();
    await expect(page.getByText('Overall Status')).toBeVisible();
    await expect(page.getByText('Healthy')).toBeVisible();
  });

  /**
   * Learning point: test.slow() triples the default timeout.
   * Use for known slow tests (like waiting for auto-refresh).
   */
  test('should auto-refresh health data', async ({ healthPage, page }) => {
    test.slow(); // Triple the timeout

    await healthPage.goto();

    // Wait for a refetch to occur (checking network)
    const refreshPromise = page.waitForRequest(
      (req) => req.url().includes('/health') && req.method() === 'GET',
      { timeout: 35_000 },
    );

    const request = await refreshPromise;
    expect(request).toBeTruthy();
  });
});
