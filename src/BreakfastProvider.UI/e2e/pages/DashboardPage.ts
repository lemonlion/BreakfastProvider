import { type Page, expect } from '@playwright/test';
import { BasePage } from './BasePage';

export class DashboardPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  async goto() {
    await this.page.goto('/');
  }

  /** Wait for dashboard stats cards to load */
  async waitForStats() {
    await this.page.waitForSelector('text=Dashboard');
  }

  /** Get the health badge in the header */
  async getHealthBadge() {
    return this.page.locator('header').getByText(/healthy|degraded|unhealthy/i);
  }

  /** Navigate to a feature via sidebar */
  async navigateTo(label: string) {
    await this.page.getByRole('navigation').getByText(label).click();
  }
}
