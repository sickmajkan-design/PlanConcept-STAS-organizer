import { defineConfig, devices } from '@playwright/test';

/**
 * The browser-level suite.
 *
 * The Vitest suite covers whole screens in jsdom, where every element has zero
 * height and nothing is ever painted. That is enough for validation, payloads
 * and which component rendered — and structurally blind to the things that
 * only exist in a browser: a grid that virtualises rows out of the DOM, a
 * dialog that opens behind the app bar, a layout that puts the save button off
 * the bottom of a tablet. This suite is for those, and it deliberately does
 * not repeat what jsdom already proves.
 *
 * It runs against the *real* API over a real database, so it also covers the
 * one seam neither suite could: that the client and the server still agree
 * about the shape of a request. A fake network answers whatever the test
 * author believed the API returns.
 *
 * Both servers are started by the config rather than by a README instruction.
 * A suite that needs three terminals in the right order is a suite that gets
 * run once.
 */
const API_PORT = Number(process.env.E2E_API_PORT ?? 5199);
const WEB_PORT = Number(process.env.E2E_WEB_PORT ?? 5173);

const apiUrl = `http://localhost:${API_PORT}`;

/**
 * The database the API is pointed at.
 *
 * Its own, never the development one: these tests create and delete records,
 * and a suite that quietly empties the database somebody was demonstrating
 * from is a suite people learn to fear.
 */
const connectionString =
  process.env.E2E_CONNECTION_STRING ??
  'Host=localhost;Port=5432;Database=construction_e2e;Username=postgres;Password=postgres';

export default defineConfig({
  testDir: './e2e',

  // Generous, because the first test also pays for the API's startup
  // migrations. Individual expectations have their own, much shorter, waits.
  timeout: 60_000,
  expect: { timeout: 10_000 },

  // A test that only passes when it runs alone is not a passing test, and
  // finding that out in CI months later is expensive. The exception is CI's
  // single worker, which is about machine size rather than about isolation.
  fullyParallel: true,
  workers: process.env.CI ? 1 : undefined,

  // Never on a developer's machine: a retry that goes green hides a race
  // instead of reporting it. Once in CI, where the alternative is a red build
  // for a hiccup nobody can reproduce.
  retries: process.env.CI ? 1 : 0,
  forbidOnly: !!process.env.CI,

  reporter: process.env.CI ? [['github'], ['list']] : [['list']],

  use: {
    baseURL: `http://localhost:${WEB_PORT}`,

    // Kept only for a failure. A trace per test is gigabytes and nobody opens
    // the green ones.
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'off',
  },

  projects: [
    {
      name: 'desktop',
      // The tablet specs are about a viewport, so running them at 1280 as
      // well would assert nothing and fail on the drawer being visible.
      testIgnore: /layout\.spec\.ts/,
      use: {
        ...devices['Desktop Chrome'],
        // Set this when the machine already has a Chromium that Playwright
        // did not install — a container image with one baked in, typically.
        // Empty means "use the one `playwright install` put there".
        launchOptions: process.env.PLAYWRIGHT_CHROMIUM_PATH
          ? { executablePath: process.env.PLAYWRIGHT_CHROMIUM_PATH }
          : {},
      },
    },
    {
      // The screen this is actually used on half the time. A layout that
      // works at 1280 and hides the save button at 800 is a bug nobody sees
      // until a foreman is standing in a portakabin with a tablet.
      name: 'tablet',
      testMatch: /layout\.spec\.ts/,
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 834, height: 1112 },
        launchOptions: process.env.PLAYWRIGHT_CHROMIUM_PATH
          ? { executablePath: process.env.PLAYWRIGHT_CHROMIUM_PATH }
          : {},
      },
    },
  ],

  webServer: [
    {
      // `dotnet run` rather than a published build: the point is to test what
      // a developer has, and the startup cost is paid once for the suite.
      command: 'dotnet run --project ../Construction.API --no-launch-profile',
      url: `${apiUrl}/health/ready`,
      reuseExistingServer: !process.env.CI,
      timeout: 180_000,
      stdout: 'pipe',
      stderr: 'pipe',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: apiUrl,
        ConnectionStrings__DefaultConnection: connectionString,
        Database__ApplyMigrationsOnStartup: 'true',
        // Known credentials, because the suite has to sign in as somebody.
        // Local-only by construction: this configuration never leaves the
        // developer's machine or the CI runner.
        Seed__SuperAdmin__Email: 'e2e@construction.local',
        Seed__SuperAdmin__Password: 'E2ePassword123!',
        JwtSettings__SecretKey: 'end-to-end-signing-key-at-least-32-characters',
        Cors__AllowedOrigins__0: `http://localhost:${WEB_PORT}`,
      },
    },
    {
      command: `npm run dev -- --port ${WEB_PORT} --strictPort`,
      url: `http://localhost:${WEB_PORT}`,
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      env: {
        VITE_API_BASE_URL: apiUrl,
        // Left empty on purpose: one of the tests asserts that the map page
        // explains the missing key instead of rendering a broken frame, which
        // is the state every developer's machine is actually in.
        VITE_GOOGLE_MAPS_API_KEY: '',
      },
    },
  ],
});
