import { test, expect } from '../fixtures';

test.describe('Daily Specials', () => {
  test('should show progress bar for order limits', { tag: '@smoke' }, async ({ page }) => {
    await page.goto('/daily-specials');
    await expect(page.getByRole('progressbar')).toBeVisible();
  });

  test('should handle sold-out specials', { tag: '@error' }, async ({ page }) => {
    /**
     * Learning point: page.route() intercepts and modifies responses.
     * Here we fake a 409 Conflict to test the "sold out" UX without
     * needing to actually exhaust the order limit.
     */
    await page.route('**/daily-specials/orders', (route) => {
      route.fulfill({
        status: 409,
        contentType: 'application/json',
        body: JSON.stringify({ title: 'Conflict', detail: 'Special sold out' }),
      });
    });

    await page.goto('/daily-specials');
    await page.getByRole('button', { name: 'Order Now' }).click();
    await expect(page.getByText('Sorry, this special is sold out!')).toBeVisible();
  });
});
