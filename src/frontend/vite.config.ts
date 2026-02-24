import { defineConfig } from 'vitest/config'
import { loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'
import path from 'path'
import { readFileSync } from 'fs'

// Read version from package.json for build-time injection
const packageJson = JSON.parse(
  readFileSync(path.resolve(__dirname, 'package.json'), 'utf-8'),
)

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

  // Determine if running in CI environment
  const isCI = process.env.CI === 'true'

  return {
    define: {
      __APP_VERSION__: JSON.stringify(packageJson.version),
      __BUILD_DATE__: JSON.stringify(new Date().toISOString()),
      __COMMIT_SHA__: JSON.stringify(process.env.GITHUB_SHA?.slice(0, 7) ?? 'local'),
    },
    plugins: [
      react(),
      VitePWA({
        disable: !pwaEnabled,
        registerType: pwaRegisterType,
        includeAssets: ['favicon.ico', 'favicon.svg', 'favicon-*.png', 'apple-touch-icon.png', 'icon-source-light.svg', 'icons/*.png'],
        manifest: {
          name: 'Cadence MSEL Manager',
          short_name: 'Cadence',
          description: 'HSEEP-compliant exercise management for emergency responders',
          theme_color: '#1e3a5f',
          background_color: '#ffffff',
          display: 'standalone',
          orientation: 'any',
          start_url: '/',
          icons: [
            {
              src: '/icons/icon-192x192.png',
              sizes: '192x192',
              type: 'image/png',
              purpose: 'any',
            },
            {
              src: '/icons/icon-512x512.png',
              sizes: '512x512',
              type: 'image/png',
              purpose: 'any',
            },
            {
              src: '/icons/icon-maskable-192x192.png',
              sizes: '192x192',
              type: 'image/png',
              purpose: 'maskable',
            },
            {
              src: '/icons/icon-maskable-512x512.png',
              sizes: '512x512',
              type: 'image/png',
              purpose: 'maskable',
            },
          ],
          categories: ['business', 'productivity'],
          shortcuts: [
            {
              name: 'My Exercises',
              url: '/exercises',
              icons: [{ src: '/icons/icon-192x192.png', sizes: '192x192', type: 'image/png' }],
            },
          ],
        },
        workbox: {
          // Precache app shell
          globPatterns: ['**/*.{js,css,html,ico,svg,woff2}'],
          // Increase limit to 3 MiB (default is 2 MiB) to accommodate larger bundles
          maximumFileSizeToCacheInBytes: 3 * 1024 * 1024,
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
      testTimeout: 10000, // 10 seconds to handle heavy test loads
      hookTimeout: 10000,

      // =========================================================================
      // Reporters - Different outputs for CI vs local development
      // =========================================================================
      // CI: default (minimal) + GitHub Actions annotations + JUnit XML for summary
      // Local: verbose console output
      reporters: isCI
        ? ['default', 'github-actions', 'junit']
        : ['verbose'],

      // JUnit XML output for CI test summary (used by dorny/test-reporter)
      outputFile: {
        junit: './test-results/junit.xml',
      },

      // =========================================================================
      // Performance - Optimized for CI
      // =========================================================================
      // Use forks for better isolation with heavy jsdom/React tests
      pool: 'forks',
      poolOptions: {
        forks: {
          // Don't isolate - tests don't share global state (faster)
          isolate: false,
          // Use more workers in CI (GitHub runners have 2-4 cores)
          minForks: isCI ? 2 : 1,
          maxForks: isCI ? 4 : undefined,
        },
      },
      // Faster file resolution
      fileParallelism: true,

      // =========================================================================
      // Coverage
      // =========================================================================
      coverage: {
        provider: 'v8',
        reporter: ['text', 'json', 'html'],
        reportsDirectory: './test-results/coverage',
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
