import { type Page, expect } from '@playwright/test';
import { BasePage } from './BasePage';

export class ReportingPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  async goto() {
    await this.page.goto('/reporting');
  }

  /** Select a tab by name */
  async selectTab(name: string) {
    await this.page.getByText(name).click();
  }

  /** Click export JSON button */
  async exportJSON() {
    await this.page.getByRole('button', { name: 'Export JSON' }).click();
  }

  /** Click export CSV button */
  async exportCSV() {
    await this.page.getByRole('button', { name: 'Export CSV' }).click();
  }
}
