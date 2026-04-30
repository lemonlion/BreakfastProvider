import { test, expect } from '../fixtures';

/**
 * Navigation & layout tests.
 *
 * Learning points:
 * - test.describe() groups related tests
 * - test.beforeEach() runs before each test in the describe block
 * - Tags: @smoke runs in the fast CI pipeline, @a11y for accessibility
 */
test.describe('Navigation', () => {
  test.beforeEach(async ({ dashboardPage }) => {
    await dashboardPage.goto();
  });

  test('should render sidebar with all nav items', { tag: '@smoke' }, async ({ page }) => {
    const nav = page.getByRole('navigation', { name: 'Main navigation' });
    await expect(nav).toBeVisible();

    const navItems = ['Dashboard', 'Pancakes', 'Waffles', 'Orders', 'Menu',
      'Daily Specials', 'Ingredients', 'Toppings', 'Inventory',
      'Reservations', 'Staff', 'Audit Logs', 'Reporting', 'Health'];

    for (const item of navItems) {
      await expect(nav.getByText(item)).toBeVisible();
    }
  });

  test('should navigate between pages via sidebar', async ({ dashboardPage, page }) => {
    await dashboardPage.navigateTo('Orders');
    await expect(page).toHaveURL(/\/orders/);
    await expect(page.getByText('Orders')).toBeVisible();
  });

  test('should toggle sidebar collapse', async ({ page }) => {
    const toggleBtn = page.getByLabel('Toggle sidebar');
    await toggleBtn.click();
    // Sidebar should be narrow (64px)
    await expect(page.locator('nav')).toHaveCSS('width', '64px');
  });

  test('should toggle light/dark theme', async ({ dashboardPage, page }) => {
    await dashboardPage.toggleTheme();
    // Verify theme class changed on html element
    const html = page.locator('html');
    // Theme is applied via data attribute or class — check body background changes
    const bgBefore = await page.evaluate(() => getComputedStyle(document.body).backgroundColor);
    await dashboardPage.toggleTheme();
    const bgAfter = await page.evaluate(() => getComputedStyle(document.body).backgroundColor);
    expect(bgBefore).not.toEqual(bgAfter);
  });
});
