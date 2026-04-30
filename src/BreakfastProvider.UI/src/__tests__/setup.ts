import '@testing-library/jest-dom';
import { server } from '../test-utils/msw/server';

// Start MSW before all tests
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

// Reset handlers after each test (remove runtime overrides)
afterEach(() => server.resetHandlers());

// Clean up
afterAll(() => server.close());
