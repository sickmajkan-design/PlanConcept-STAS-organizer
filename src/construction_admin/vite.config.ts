/**
 * The suite runs in the company's own time zone, whatever machine runs it.
 *
 * The bugs that hurt here are the ones that only show for people east of Greenwich: a date built through
 * `toISOString()` is the previous day for the first hours after local midnight, and a month's first day comes out
 * as the last day of the month before. On a machine set to UTC (CI) those bugs cannot happen, so a test that
 * passes there proves nothing. Set before any worker starts, so every one of them inherits it.
 */
process.env.TZ = 'Europe/Belgrade'

import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  test: {
    // jsdom only where a test actually renders. The bulk of this suite is
    // plain functions — query normalisation, date windows, plural selection —
    // and giving those a DOM costs start-up time for nothing. The files that
    // do render say so with a `@vitest-environment jsdom` docblock.
    environment: 'node',
    include: ['src/**/*.test.{ts,tsx}'],
    setupFiles: ['src/test/setup.ts'],
    restoreMocks: true,
    // The rendering tests import a whole screen on their first run, which on a busy machine (the full suite
    // runs dozens of jsdom files at once) can take longer than a default five seconds. A test that is
    // slow only because the machine is busy should wait, not fail; one that is wrong still fails.
    testTimeout: 60_000,
    hookTimeout: 60_000,
    // Half the cores, so the workers do not starve each other (and the browser tests running beside them).
    maxWorkers: '50%',
  },
})
