import { defineConfig } from 'vitest/config'
import { loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'
import path from 'path'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  // PWA register type: 'autoUpdate' for dev/UAT, 'prompt' for production
  // Set via VITE_PWA_REGISTER_TYPE env var or defaults based on mode
  const pwaRegisterType = (env.VITE_PWA_REGISTER_TYPE ||
    (mode === 'production' ? 'prompt' : 'autoUpdate')) as 'prompt' | 'autoUpdate'

  // Disable PWA in development to avoid service worker caching issues
  // Set VITE_PWA_ENABLED=true to enable PWA in development for testing
  const pwaEnabled = mode === 'production' || env.VITE_PWA_ENABLED === 'true'

  return {
    plugins: [
      react(),
      VitePWA({
        disabled: !pwaEnabled,
        registerType: pwaRegisterType,
        includeAssets: ['dynamis-favicon.svg', 'dynamis-logo.jpg', 'icons/*.svg'],
        manifest: {
          name: 'Cadence MSEL Manager',
          short_name: 'Cadence',
          description: 'HSEEP-compliant exercise management for emergency responders',
          theme_color: '#0020c2',
          background_color: '#ffffff',
          display: 'standalone',
          orientation: 'any',
          start_url: '/',
          icons: [
            {
              src: '/icons/icon-192.svg',
              sizes: '192x192',
              type: 'image/svg+xml',
              purpose: 'any maskable',
            },
            {
              src: '/icons/icon-512.svg',
              sizes: '512x512',
              type: 'image/svg+xml',
              purpose: 'any maskable',
            },
          ],
          categories: ['business', 'productivity'],
          shortcuts: [
            {
              name: 'My Exercises',
              url: '/exercises',
              icons: [{ src: '/icons/icon-192.svg', sizes: '192x192' }],
            },
          ],
        },
        workbox: {
          // Precache app shell
          globPatterns: ['**/*.{js,css,html,ico,svg,woff2}'],
          // Runtime caching strategies
          runtimeCaching: [
            {
              // API calls - NetworkOnly (app-level sync service handles data caching)
              urlPattern: /^https?:\/\/.*\/api\//,
              handler: 'NetworkOnly',
            },
            {
              // SignalR - NetworkOnly (real-time connections)
              urlPattern: /\/hubs\//,
              handler: 'NetworkOnly',
            },
            {
              // Cache fonts
              urlPattern: /\.(?:woff|woff2|ttf|otf)$/,
              handler: 'CacheFirst',
              options: {
                cacheName: 'fonts',
                expiration: {
                  maxEntries: 20,
                  maxAgeSeconds: 60 * 60 * 24 * 365, // 1 year
                },
              },
            },
            {
              // Cache images
              urlPattern: /\.(?:png|jpg|jpeg|svg|gif|webp)$/,
              handler: 'CacheFirst',
              options: {
                cacheName: 'images',
                expiration: {
                  maxEntries: 100,
                  maxAgeSeconds: 60 * 60 * 24 * 30, // 30 days
                },
              },
            },
          ],
          // Offline fallback
          navigateFallback: '/index.html',
          navigateFallbackDenylist: [/^\/api/, /^\/hubs/],
        },
      }),
    ],
    resolve: {
      alias: {
        '@': path.resolve(__dirname, './src'),
      },
    },
    server: {
      port: 5197,
    },
    preview: {
      port: 5197,
    },
    test: {
      globals: true,
      environment: 'jsdom',
      setupFiles: './src/test/setup.ts',
      include: ['src/**/*.{test,spec}.{ts,tsx}'],
      coverage: {
        provider: 'v8',
        reporter: ['text', 'json', 'html'],
        exclude: [
          'node_modules/',
          'src/test/',
          '**/*.d.ts',
          '**/*.config.*',
          'dist/',
        ],
      },
    },
  }
})
