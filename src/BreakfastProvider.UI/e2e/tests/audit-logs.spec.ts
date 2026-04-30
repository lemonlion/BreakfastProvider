import { test, expect } from '../fixtures';

test.describe('Audit Logs', () => {
  test('should toggle between table and infinite scroll', async ({ page }) => {
    await page.goto('/audit-logs');

    await test.step('Default is table view', async () => {
      await expect(page.locator('table')).toBeVisible();
    });

    await test.step('Switch to infinite scroll', async () => {
      await page.getByRole('button', { name: 'Infinite Scroll' }).click();
      await expect(page.locator('table')).not.toBeVisible();
    });

    await test.step('Switch back to table', async () => {
      await page.getByRole('button', { name: 'Table View' }).click();
      await expect(page.locator('table')).toBeVisible();
    });
  });
});
