import { type Page, expect } from '@playwright/test';
import { BasePage } from './BasePage';

export class OrdersPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  async goto() {
    await this.page.goto('/orders');
  }

  /** Click the 'New Order' button */
  async clickNewOrder() {
    await this.page.getByRole('link', { name: 'New Order' }).click();
  }

  /** Get all order rows in the table */
  async getOrderRows() {
    return this.page.locator('table tbody tr');
  }

  /** Filter by status tab */
  async filterByStatus(status: string) {
    await this.page.getByRole('button', { name: status }).click();
  }

  /** Search orders using global filter */
  async searchOrders(query: string) {
    await this.page.getByPlaceholder('Search all columns...').fill(query);
  }

  /** Click on an order to view details */
  async clickOrder(orderId: string) {
    await this.page.getByText(orderId).click();
  }

  /** Click a status transition button on a specific row */
  async transitionOrder(rowIndex: number, newStatus: string) {
    const row = this.page.locator('table tbody tr').nth(rowIndex);
    await row.getByText(`→ ${newStatus}`).click();
  }

  /** Navigate to a specific page */
  async goToPage(pageNum: number) {
    // URL-driven pagination
    await this.page.goto(`/orders?page=${pageNum}`);
  }

  /** Get current URL search params */
  async getCurrentParams(): Promise<URLSearchParams> {
    const url = new URL(this.page.url());
    return url.searchParams;
  }
}
