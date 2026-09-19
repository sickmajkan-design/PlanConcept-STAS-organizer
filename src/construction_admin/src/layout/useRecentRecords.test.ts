/**
 * @vitest-environment jsdom
 */
import { describe, expect, it } from 'vitest';

import { filterRecentForRole } from './useRecentRecords';

const records = [
  { path: '/employees/1', label: 'Ana Maric' },
  { path: '/projects/2', label: 'Tower A' },
  { path: '/vehicles/3', label: 'Caddy' },
];

describe('filterRecentForRole', () => {
  it('shows a record only when its page is one the role can open', () => {
    // A demoted account: the directory pages are gone from its nav.
    const shown = filterRecentForRole(records, ['/', '/bulletin', '/time-entries']);

    expect(shown).toEqual([]);
  });

  it('keeps the records under pages the role still has', () => {
    const shown = filterRecentForRole(records, ['/projects', '/vehicles']);

    expect(shown.map((r) => r.label)).toEqual(['Tower A', 'Caddy']);
  });

  it('does not match a page whose path merely starts with the same letters', () => {
    expect(filterRecentForRole([{ path: '/employees-archive/1', label: 'x' }], ['/employees'])).toEqual([]);
  });

  it('never lets the home page vouch for everything', () => {
    expect(filterRecentForRole(records, ['/'])).toEqual([]);
  });

  it('does not delete anything — a promotion brings the records back', () => {
    const narrowed = filterRecentForRole(records, ['/projects']);
    const restored = filterRecentForRole(records, ['/employees', '/projects', '/vehicles']);

    expect(narrowed).toHaveLength(1);
    expect(restored).toHaveLength(3);
  });
});
