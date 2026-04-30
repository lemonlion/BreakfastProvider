import { type Page, expect } from '@playwright/test';
import { BasePage } from './BasePage';

export class IngredientsPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  async goto() {
    await this.page.goto('/ingredients');
  }

  /** Get all ingredient card elements */
  async getIngredientCards() {
    return this.page.locator('[data-testid="ingredient-card"]');
  }

  /** Get the availability badge for a named ingredient */
  async getAvailabilityBadge(name: string) {
    const card = this.page.locator('[data-testid="ingredient-card"]').filter({ hasText: name });
    return card.locator('[data-testid="availability-badge"]');
  }
}
