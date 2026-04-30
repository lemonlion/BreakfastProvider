import { type Page, expect } from '@playwright/test';
import { BasePage } from './BasePage';

export class ToppingsPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  async goto() {
    await this.page.goto('/toppings');
  }

  /** Select rows by their indices (0-based) */
  async selectRows(indices: number[]) {
    const checkboxes = this.page.getByRole('checkbox');
    for (const index of indices) {
      await checkboxes.nth(index).check();
    }
  }

  /** Click the bulk delete button */
  async clickBulkDelete() {
    await this.page.getByRole('button', { name: /delete selected/i }).click();
  }

  /** Open the create topping modal */
  async openCreateModal() {
    await this.page.getByRole('button', { name: /add topping/i }).click();
  }
}
