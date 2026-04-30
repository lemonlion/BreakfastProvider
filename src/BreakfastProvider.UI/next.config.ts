import { createVanillaExtractPlugin } from '@vanilla-extract/next-plugin';

const withVanillaExtract = createVanillaExtractPlugin();

/** @type {import('next').NextConfig} */
const nextConfig = {
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: `${process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5080'}/:path*`,
      },
    ];
  },

  reactStrictMode: true,

  typedRoutes: true,

  // Use webpack for vanilla-extract compatibility
  turbopack: {},
};

export default withVanillaExtract(nextConfig);
