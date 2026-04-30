import { test, expect } from '../fixtures';

test.describe('Reporting', () => {
  test('should render charts in each tab', { tag: '@smoke' }, async ({ page }) => {
    await page.goto('/reporting');

    await test.step('Order Summary tab has bar chart', async () => {
      await expect(page.locator('.recharts-bar')).toBeVisible();
    });

    await test.step('Switch to Recipes tab', async () => {
      await page.getByText('Recipes').click();
      await expect(page.locator('.recharts-line')).toBeVisible();
    });

    await test.step('Switch to Recipe Types tab has pie chart', async () => {
      await page.getByText('Recipe Types').click();
      await expect(page.locator('.recharts-pie')).toBeVisible();
    });
  });

  test('should export JSON data', async ({ page }) => {
    await page.goto('/reporting');

    /**
     * Learning point: page.waitForEvent('download') tracks file downloads.
     * We verify the export button triggers a download and check the filename.
     */
    const downloadPromise = page.waitForEvent('download');
    await page.getByRole('button', { name: 'Export JSON' }).click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toBe('order-summary.json');
  });
});
