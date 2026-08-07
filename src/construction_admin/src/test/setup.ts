import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';

/**
 * Unmounts anything a test rendered.
 *
 * Testing Library appends each render to `document.body` and leaves it there.
 * Without this, the second test in a file queries a document containing both
 * its own render and the previous one — and `getByRole` throws "found multiple
 * elements", which reads as a bug in the component rather than in the setup.
 */
afterEach(() => {
  cleanup();
});
