import { type Page, expect } from '@playwright/test';
import { BasePage } from './BasePage';

export class HealthPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  async goto() {
    await this.page.goto('/health');
  }

  /** Wait for auto-refresh to trigger a new health request */
  async waitForAutoRefresh() {
    await this.page.waitForRequest(
      (req) => req.url().includes('/health') && req.method() === 'GET',
      { timeout: 35_000 },
    );
  }

  /** Get the status element for a named dependency */
  async getDependencyStatus(name: string) {
    const card = this.page.locator('[data-testid="dependency-card"]').filter({ hasText: name });
    return card.locator('[data-testid="status-badge"]');
  }
}
