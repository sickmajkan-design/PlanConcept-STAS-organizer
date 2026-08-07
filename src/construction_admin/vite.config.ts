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
  },
})
