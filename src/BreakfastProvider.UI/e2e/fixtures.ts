import { test as base, expect, type Page } from '@playwright/test';
import { DashboardPage } from './pages/DashboardPage';
import { OrdersPage } from './pages/OrdersPage';
import { PancakesPage } from './pages/PancakesPage';
import { MenuPage } from './pages/MenuPage';
import { ToppingsPage } from './pages/ToppingsPage';
import { HealthPage } from './pages/HealthPage';

/**
 * Custom test fixture extending Playwright's base test.
 *
 * Learning points:
 * - test.extend<T>() creates a new test function with extra fixtures
 * - Fixtures are lazily instantiated — only created when used in a test
 * - Fixtures can depend on other fixtures (here, all depend on `page`)
 * - Using fixtures avoids repetitive Page Object construction in every test
 */
export const test = base.extend<{
  dashboardPage: DashboardPage;
  ordersPage: OrdersPage;
  pancakesPage: PancakesPage;
  menuPage: MenuPage;
  toppingsPage: ToppingsPage;
  healthPage: HealthPage;
}>({
  dashboardPage: async ({ page }, use) => {
    await use(new DashboardPage(page));
  },
  ordersPage: async ({ page }, use) => {
    await use(new OrdersPage(page));
  },
  pancakesPage: async ({ page }, use) => {
    await use(new PancakesPage(page));
  },
  menuPage: async ({ page }, use) => {
    await use(new MenuPage(page));
  },
  toppingsPage: async ({ page }, use) => {
    await use(new ToppingsPage(page));
  },
  healthPage: async ({ page }, use) => {
    await use(new HealthPage(page));
  },
});

export { expect };
