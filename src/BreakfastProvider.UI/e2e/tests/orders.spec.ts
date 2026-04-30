import { test, expect } from '../fixtures';

test.describe('Orders', () => {
  test.beforeEach(async ({ ordersPage }) => {
    await ordersPage.goto();
  });

  test('should display paginated order list', { tag: '@smoke' }, async ({ ordersPage }) => {
    const rows = await ordersPage.getOrderRows();
    await expect(rows).toHaveCount(2);
  });

  test('should filter orders by status tab', { tag: '@crud' }, async ({ ordersPage, page }) => {
    await ordersPage.filterByStatus('Created');

    /**
     * Learning point: expect.poll() retries an assertion until it passes
     * or times out. Useful when the UI updates asynchronously (e.g.,
     * after URL change triggers a new API call).
     */
    await expect.poll(
      async () => ordersPage.getCurrentParams().then((p) => p.get('status')),
      { timeout: 5000 },
    ).toBe('Created');
  });

  test('should navigate to order detail on click', async ({ ordersPage, page }) => {
    await ordersPage.clickOrder('order-1');
    await expect(page).toHaveURL(/\/orders\/order-1/);
  });

  test('should update URL params when paginating', async ({ ordersPage, page }) => {
    /**
     * Learning point: URL-driven state means we can verify page changes
     * via the URL rather than inspecting DOM elements.
     */
    await page.getByRole('button', { name: 'Next' }).click();

    await expect(page).toHaveURL(/page=2/);
  });

  test('should prefetch order data on hover', async ({ ordersPage, page }) => {
    /**
     * Learning point: page.route() intercepts network requests in Playwright.
     * We use it to verify that hovering triggers a prefetch GET request.
     */
    const prefetchPromise = page.waitForRequest(
      (request) => request.url().includes('/orders/order-1') && request.method() === 'GET',
    );

    await page.getByText('order-1').hover();
    const prefetchRequest = await prefetchPromise;
    expect(prefetchRequest.url()).toContain('/orders/order-1');
  });
});

test.describe('Order Creation Wizard', () => {
  test('should complete multi-step order creation', { tag: '@crud' }, async ({ page }) => {
    await page.goto('/orders/new');

    await test.step('Step 1: Customer details', async () => {
      await page.getByLabel('Customer Name').fill('Test Customer');
      await page.getByRole('button', { name: 'Next' }).click();
    });

    await test.step('Step 2: Add items', async () => {
      await page.getByLabel('Item Name').fill('Pancake');
      await page.getByLabel('Quantity').fill('2');
      await page.getByRole('button', { name: '+ Add Item' }).click();
      // Fill second item
      await page.getByLabel('Item Name').last().fill('Waffle');
      await page.getByRole('button', { name: 'Next' }).click();
    });

    await test.step('Step 3: Review and submit', async () => {
      await expect(page.getByText('Test Customer')).toBeVisible();
      await expect(page.getByText('Pancake × 2')).toBeVisible();
      await expect(page.getByText('Waffle')).toBeVisible();
      await page.getByRole('button', { name: 'Submit' }).click();
    });

    await test.step('Verify redirect to new order', async () => {
      await expect(page).toHaveURL(/\/orders\//);
      await expect(page.getByText('Order created successfully!')).toBeVisible();
    });
  });
});
