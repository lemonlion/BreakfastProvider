import { type Page } from '@playwright/test';
import { BasePage } from './BasePage';

export class PancakesPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  async goto() {
    await this.page.goto('/pancakes');
  }

  async fillRecipeType(value: string) {
    await this.page.getByLabel('Recipe Type').fill(value);
  }

  async fillQuantity(value: number) {
    await this.page.getByLabel('Quantity').fill(String(value));
  }

  async selectMilkType(value: 'cow' | 'goat') {
    await this.page.getByLabel('Milk Type').selectOption(value);
  }

  async submitForm() {
    await this.page.getByRole('button', { name: /create batch/i }).click();
  }
}
