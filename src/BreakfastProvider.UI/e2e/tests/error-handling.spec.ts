import { test, expect } from '../fixtures';

test.describe('Error Handling', { tag: '@error' }, () => {
  test('should show error boundary on runtime error', async ({ page }) => {
    /**
     * Learning point: page.route() can simulate API failures to test
     * error handling in the UI. By returning a 500, we trigger the
     * error.tsx boundary.
     */
    await page.route('**/orders*', (route) => {
      route.fulfill({ status: 500, body: 'Internal Server Error' });
    });

    await page.goto('/orders');
    await expect(page.getByText('Something went wrong')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Try again' })).toBeVisible();
  });

  test('should show 404 page for unknown routes', async ({ page }) => {
    await page.goto('/nonexistent-page');
    await expect(page.getByText('Page Not Found')).toBeVisible();
  });

  test('should show validation errors on invalid input', async ({ page }) => {
    await page.goto('/pancakes');
    // Submit without filling required fields
    await page.getByRole('button', { name: /create batch/i }).click();
    await expect(page.getByRole('alert')).toBeVisible();
  });
});
