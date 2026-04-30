import { test, expect } from '../fixtures';

test.describe('Dashboard', () => {
  test('should display health badge in header', { tag: '@smoke' }, async ({ dashboardPage }) => {
    await dashboardPage.goto();
    const badge = await dashboardPage.getHealthBadge();
    await expect(badge).toBeVisible();
  });

  test('should load stats, health, and recent orders', async ({ dashboardPage, page }) => {
    await dashboardPage.goto();

    /**
     * Learning point: test.step() groups assertions into named steps.
     * Steps appear in the HTML report and trace viewer, making it
     * clear which part of a long test failed.
     */
    await test.step('Stats cards load', async () => {
      await dashboardPage.waitForStats();
    });

    await test.step('Recent orders table loads', async () => {
      await expect(page.locator('table')).toBeVisible();
    });

    await test.step('Health overview loads', async () => {
      await expect(page.getByText(/healthy|degraded/i)).toBeVisible();
    });
  });
});
