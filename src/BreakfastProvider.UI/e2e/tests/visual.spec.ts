import { test, expect } from '../fixtures';

/**
 * Visual regression tests using Playwright's screenshot comparison.
 *
 * Learning points:
 * - toHaveScreenshot() captures and compares against a baseline image
 * - First run creates the baseline; subsequent runs compare
 * - maxDiffPixelRatio allows small rendering differences (anti-aliasing)
 * - Update screenshots with: npx playwright test --update-snapshots
 */
test.describe('Visual Regression', () => {
  test('Dashboard layout', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveScreenshot('dashboard.png', {
      maxDiffPixelRatio: 0.01,
    });
  });

  test('Orders table', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveScreenshot('orders-table.png', {
      maxDiffPixelRatio: 0.01,
    });
  });

  test('Health grid', async ({ page }) => {
    await page.goto('/health');
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveScreenshot('health-grid.png', {
      maxDiffPixelRatio: 0.01,
    });
  });
});
