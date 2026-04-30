import { type Page, expect } from '@playwright/test';
import { BasePage } from './BasePage';

export class MenuPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  async goto() {
    await this.page.goto('/menu');
  }

  /** Get all menu item elements */
  async getMenuItems() {
    return this.page.locator('[data-testid="menu-item"]');
  }

  /** Click the clear cache button */
  async clickClearCache() {
    await this.page.getByRole('button', { name: /clear cache/i }).click();
  }

  /** Wait for menu data to refresh after cache clear */
  async waitForRefresh() {
    await this.page.waitForLoadState('networkidle');
  }
}
