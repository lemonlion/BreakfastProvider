import type { Config } from 'jest';
import nextJest from 'next/jest.js';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const createJestConfig = nextJest({
  dir: './',
});

const config: Config = {
  displayName: 'BreakfastProvider.UI',
  testEnvironment: 'jsdom',
  testEnvironmentOptions: {
    customExportConditions: [''],
    url: 'http://localhost:3000',
  },
  setupFiles: ['<rootDir>/src/__tests__/jest.polyfills.ts'],
  setupFilesAfterEnv: ['<rootDir>/src/__tests__/setup.ts'],
  testMatch: [
    '<rootDir>/src/__tests__/**/*.test.{ts,tsx}',
  ],
  moduleNameMapper: {
    '\\.css(\\.ts)?$': '<rootDir>/src/__mocks__/vanillaExtractMock.js',
    '^@/(.*)$': '<rootDir>/src/$1',
  },
  collectCoverageFrom: [
    'src/**/*.{ts,tsx}',
    '!src/**/*.stories.{ts,tsx}',
    '!src/**/*.css.ts',
    '!src/lib/api/generated-types.ts',
  ],
  coverageThreshold: {
    global: {
      branches: 80,
      functions: 80,
      lines: 80,
      statements: 80,
    },
  },
};

const baseConfigFn = createJestConfig(config);

// next/jest overwrites transformIgnorePatterns and moduleNameMapper.
// Override both after resolution.
export default async () => {
  const resolved = await baseConfigFn();
  resolved.transformIgnorePatterns = [
    '^.+\\.module\\.(css|sass|scss)$',
  ];
  // Replace next/jest's CSS mock with our deep proxy that handles
  // vanilla-extract recipe() calls and nested theme token access.
  const __dirname = dirname(fileURLToPath(import.meta.url));
  const mockPath = resolve(__dirname, 'src/__mocks__/vanillaExtractMock.js');
  if (resolved.moduleNameMapper) {
    const mapper = resolved.moduleNameMapper as Record<string, string>;
    for (const key of Object.keys(mapper)) {
      if (key.includes('css') && !key.includes('module')) {
        mapper[key] = mockPath;
      }
    }
  }
  return resolved;
};
