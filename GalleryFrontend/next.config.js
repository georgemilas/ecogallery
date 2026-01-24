const isProd = process.env.NODE_ENV === 'production';

/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  // Keep dev and prod outputs separate so they can run simultaneously
  distDir: isProd ? '.next' : '.next-dev',
  // Enable standalone output for Docker deployment
  output: isProd ? 'standalone' : undefined,
};

module.exports = nextConfig;
