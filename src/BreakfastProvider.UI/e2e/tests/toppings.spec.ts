import { test, expect } from '../fixtures';

test.describe('Toppings', () => {
  test('should select and bulk delete toppings', { tag: '@crud' }, async ({ toppingsPage, page }) => {
    await toppingsPage.goto();

    await test.step('Select rows', async () => {
      const checkboxes = page.getByRole('checkbox');
      await checkboxes.nth(1).check();
      await checkboxes.nth(2).check();
    });

    await test.step('Click bulk delete', async () => {
      await page.getByRole('button', { name: /delete selected/i }).click();
    });

    await test.step('Verify success', async () => {
      await expect(page.getByText(/deleted 2 toppings/i)).toBeVisible();
    });
  });
});
