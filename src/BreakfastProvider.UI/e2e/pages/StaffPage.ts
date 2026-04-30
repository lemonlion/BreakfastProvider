import { type Page, expect } from '@playwright/test';
import { BasePage } from './BasePage';

export class StaffPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  async goto() {
    await this.page.goto('/staff');
  }

  /** Filter staff list by role */
  async filterByRole(role: string) {
    await this.page.getByRole('button', { name: role }).click();
  }

  /** Expand a row to show details */
  async expandRow(index: number) {
    const row = this.page.locator('table tbody tr').nth(index);
    await row.getByRole('button', { name: /expand/i }).click();
  }
}
