import { type Page, expect } from '@playwright/test';
import { BasePage } from './BasePage';

export class InventoryPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  async goto() {
    await this.page.goto('/inventory');
  }

  /** Use the global search input */
  async globalSearch(query: string) {
    await this.page.getByPlaceholder(/search/i).fill(query);
  }

  /** Click the export CSV button */
  async exportCSV() {
    await this.page.getByRole('button', { name: /export csv/i }).click();
  }
}
