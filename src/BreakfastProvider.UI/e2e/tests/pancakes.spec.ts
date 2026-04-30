import { test, expect } from '../fixtures';

test.describe('Pancakes', () => {
  test('should create a pancake batch', { tag: ['@smoke', '@crud'] }, async ({ pancakesPage, page }) => {
    await pancakesPage.goto();

    await test.step('Fill in recipe form', async () => {
      await pancakesPage.fillRecipeType('classic');
      await pancakesPage.fillQuantity(5);
      await pancakesPage.selectMilkType('cow');
    });

    await test.step('Submit form', async () => {
      await pancakesPage.submitForm();
    });

    await test.step('Verify success toast', async () => {
      await expect(page.getByText('Pancake batch created!')).toBeVisible();
    });
  });

  test('should show validation errors for empty form', { tag: '@error' }, async ({ pancakesPage, page }) => {
    await pancakesPage.goto();
    await pancakesPage.fillRecipeType('');
    await pancakesPage.submitForm();

    await expect(page.getByText('Recipe type is required')).toBeVisible();
  });
});
