import { type Page, type Locator, expect } from '@playwright/test';

/**
 * Base Page Object Model — shared methods for all pages.
 *
 * Learning points:
 * - POM pattern encapsulates page interactions behind clear methods
 * - Locator over ElementHandle — Locators auto-retry and auto-wait
 * - Protected members let subclasses access `page` without exposing it
 */
export abstract class BasePage {
  protected readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  /** Navigate to this page's URL */
  abstract goto(): Promise<void>;

  /** Get the page header title text */
  async getPageTitle(): Promise<string> {
    return this.page.locator('h2').first().innerText();
  }

  /** Check the sidebar has an active nav item for this page */
  async expectNavActive(label: string): Promise<void> {
    const nav = this.page.getByRole('navigation', { name: 'Main navigation' });
    await expect(nav.getByText(label)).toHaveAttribute('class', /active/);
  }

  /** Wait for page load (no pending network requests) */
  async waitForLoad(): Promise<void> {
    await this.page.waitForLoadState('networkidle');
  }

  /** Click the theme toggle button */
  async toggleTheme(): Promise<void> {
    await this.page.getByLabel(/switch to/i).click();
  }

  /** Get all toast notifications currently visible */
  async getToasts(): Promise<Locator> {
    return this.page.locator('[aria-live="polite"] > div');
  }
}
