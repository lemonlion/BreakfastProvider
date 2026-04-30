import { type Page, expect } from '@playwright/test';
import { BasePage } from './BasePage';

export class AuditLogsPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  async goto() {
    await this.page.goto('/audit-logs');
  }

  /** Switch to infinite scroll view */
  async switchToInfiniteScroll() {
    await this.page.getByRole('button', { name: 'Infinite Scroll' }).click();
  }

  /** Scroll down to trigger loading more items */
  async scrollToLoadMore() {
    await this.page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
  }
}
