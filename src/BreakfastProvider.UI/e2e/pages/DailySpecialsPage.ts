import { type Page, expect } from '@playwright/test';
import { BasePage } from './BasePage';

export class DailySpecialsPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  async goto() {
    await this.page.goto('/daily-specials');
  }

  /** Click order button for a specific special */
  async orderSpecial(name: string) {
    const card = this.page.locator(`[data-testid="special-card"]`).filter({ hasText: name });
    await card.getByRole('button', { name: /order/i }).click();
  }

  /** Get the progress bar for a named special */
  async getProgressBar(name: string) {
    const card = this.page.locator(`[data-testid="special-card"]`).filter({ hasText: name });
    return card.getByRole('progressbar');
  }
}
