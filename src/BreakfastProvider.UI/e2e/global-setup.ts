import { type FullConfig } from '@playwright/test';

/**
 * Global setup runs once before all test files.
 *
 * Learning point: Use this for one-time expensive operations like
 * seeding a database, creating test accounts, or verifying the API
 * is reachable. The FullConfig parameter gives access to Playwright config.
 */
export default async function globalSetup(config: FullConfig) {
  // Verify the API is reachable before running tests
  const apiUrl = process.env.API_BASE_URL ?? 'http://localhost:5080';

  try {
    const response = await fetch(`${apiUrl}/health`);
    if (!response.ok) {
      console.warn(`⚠️  API health check returned ${response.status}. Tests may fail.`);
    } else {
      console.log('✅ API is healthy');
    }
  } catch {
    console.warn('⚠️  API is not reachable. E2E tests require the API to be running.');
  }
}
