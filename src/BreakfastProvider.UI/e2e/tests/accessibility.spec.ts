import { test, expect } from '../fixtures';
import AxeBuilder from '@axe-core/playwright';

/**
 * Accessibility audit using @axe-core/playwright.
 *
 * Learning points:
 * - AxeBuilder runs axe-core against the page DOM
 * - .analyze() returns a results object with violations array
 * - Each violation has an id, description, and affected nodes
 * - Tags like 'wcag2a', 'wcag2aa' filter by WCAG level
 * - exclude() skips known third-party widgets
 */
test.describe('Accessibility', { tag: '@a11y' }, () => {
  const pages = [
    { name: 'Dashboard', url: '/' },
    { name: 'Orders', url: '/orders' },
    { name: 'Pancakes', url: '/pancakes' },
    { name: 'Menu', url: '/menu' },
    { name: 'Toppings', url: '/toppings' },
    { name: 'Health', url: '/health' },
    { name: 'Reporting', url: '/reporting' },
  ];

  for (const { name, url } of pages) {
    test(`${name} page should have no WCAG 2.1 AA violations`, async ({ page }) => {
      await page.goto(url);
      await page.waitForLoadState('networkidle');

      const results = await new AxeBuilder({ page })
        .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
        .exclude('.recharts-wrapper') // Recharts SVGs may have known issues
        .analyze();

      /**
       * Learning point: expect.soft() records the failure but continues
       * the test. This way we see ALL violations, not just the first one.
       */
      for (const violation of results.violations) {
        expect.soft(
          violation.nodes,
          `${violation.id}: ${violation.description} (${violation.nodes.length} occurrences)`,
        ).toHaveLength(0);
      }

      // Hard fail if any violations exist
      expect(results.violations).toHaveLength(0);
    });
  }
});
