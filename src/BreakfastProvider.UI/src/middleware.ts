import { NextResponse, type NextRequest } from 'next/server';

/**
 * Edge middleware executed before every page request.
 *
 * Learning points:
 * - middleware.ts at the root of src/ intercepts ALL matched routes
 * - The matcher config limits which routes are processed
 * - Here we add a correlation ID header for tracing
 * - Could also be used for auth redirects, geolocation, A/B testing
 */
export function middleware(request: NextRequest) {
  const response = NextResponse.next();

  // Add correlation ID if not present
  const correlationId = request.headers.get('x-correlation-id') ?? crypto.randomUUID();
  response.headers.set('x-correlation-id', correlationId);

  return response;
}

export const config = {
  matcher: [
    // Match all routes except static files, images, and API proxy
    '/((?!_next/static|_next/image|favicon.ico).*)',
  ],
};
