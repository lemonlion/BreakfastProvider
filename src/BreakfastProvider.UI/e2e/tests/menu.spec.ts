import { test, expect } from '../fixtures';

test.describe('Menu', () => {
  test('should display menu items', { tag: '@smoke' }, async ({ menuPage, page }) => {
    await menuPage.goto();
    await expect(page.getByText('Classic Pancakes')).toBeVisible();
    await expect(page.getByText('Belgian Waffles')).toBeVisible();
  });

  test('should clear cache via client mutation', { tag: '@crud' }, async ({ menuPage, page }) => {
    await menuPage.goto();
    await page.getByRole('button', { name: 'Clear Cache (Client)' }).click();
    await expect(page.getByText('Menu cache cleared')).toBeVisible();
  });

  test('should clear cache via server action form', async ({ menuPage, page }) => {
    await menuPage.goto();

    /**
     * Learning point: Server Actions work via HTML form submission.
     * Even with JavaScript disabled, the form still works. We can test
     * this by verifying the form has method="POST" and action attribute.
     */
    const form = page.locator('form').filter({ has: page.getByText('Clear Cache (Server Action)') });
    await expect(form).toBeVisible();
    await form.getByRole('button').click();
  });
});
