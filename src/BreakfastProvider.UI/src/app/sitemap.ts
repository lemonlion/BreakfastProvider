import type { MetadataRoute } from 'next';

/**
 * Dynamic sitemap generation.
 *
 * Learning point: Next.js recognises sitemap.ts and serves it at /sitemap.xml.
 * The function runs at build time (static) or request time (dynamic).
 */
export default function sitemap(): MetadataRoute.Sitemap {
  const baseUrl = process.env.NEXT_PUBLIC_APP_URL ?? 'http://localhost:3000';

  return [
    { url: baseUrl, lastModified: new Date(), changeFrequency: 'daily', priority: 1 },
    { url: `${baseUrl}/pancakes`, lastModified: new Date(), changeFrequency: 'daily', priority: 0.8 },
    { url: `${baseUrl}/waffles`, lastModified: new Date(), changeFrequency: 'daily', priority: 0.8 },
    { url: `${baseUrl}/orders`, lastModified: new Date(), changeFrequency: 'always', priority: 0.9 },
    { url: `${baseUrl}/menu`, lastModified: new Date(), changeFrequency: 'daily', priority: 0.7 },
    { url: `${baseUrl}/daily-specials`, lastModified: new Date(), changeFrequency: 'daily', priority: 0.8 },
    { url: `${baseUrl}/reporting`, lastModified: new Date(), changeFrequency: 'weekly', priority: 0.5 },
  ];
}
