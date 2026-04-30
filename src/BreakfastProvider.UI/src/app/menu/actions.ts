'use server';

import { revalidatePath } from 'next/cache';

/**
 * Server Action to clear the menu cache.
 *
 * Learning points:
 * - 'use server' directive marks this as a Server Action
 * - It runs on the Node.js server, not in the browser
 * - revalidatePath() tells Next.js to re-fetch data for /menu
 * - Can be used as a <form action={...}> — works without JavaScript
 */
export async function clearMenuCacheAction() {
  const apiUrl = process.env.API_BASE_URL ?? 'http://localhost:5080';

  await fetch(`${apiUrl}/menu/cache`, { method: 'DELETE' });

  // Tell Next.js to revalidate the menu page data
  revalidatePath('/menu');
}
