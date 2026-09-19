/**
 * @vitest-environment jsdom
 */
import { beforeEach, describe, expect, it } from 'vitest';

import {
  purgeLegacyUiStorage,
  readScoped,
  storageScope,
  writeScoped,
} from './userScopedStorage';

beforeEach(() => {
  window.localStorage.clear();
});

const admin = { id: 'user-1', role: 'Admin' as const };
const foreman = { id: 'user-2', role: 'Foreman' as const };

describe('storageScope', () => {
  it('is nothing without a signed-in user', () => {
    expect(storageScope(null)).toBeNull();
    expect(storageScope(undefined)).toBeNull();
  });

  it('differs per account and per role of the same account', () => {
    expect(storageScope(admin)).not.toBe(storageScope(foreman));
    expect(storageScope(admin)).not.toBe(storageScope({ ...admin, role: 'Foreman' }));
  });
});

describe('scoped reads and writes', () => {
  it('never shows one account what another stored', () => {
    writeScoped(storageScope(admin), 'nav.recentRecords', [{ path: '/employees/1', label: 'Ana' }]);

    expect(readScoped(storageScope(foreman), 'nav.recentRecords', [])).toEqual([]);
    expect(readScoped(storageScope(admin), 'nav.recentRecords', [])).toHaveLength(1);
  });

  it('starts an account over when its role changes', () => {
    writeScoped(storageScope(admin), 'nav.favorites', ['/users']);

    expect(readScoped(storageScope({ ...admin, role: 'Foreman' }), 'nav.favorites', [])).toEqual([]);
  });

  it('reads and writes nothing while nobody is signed in', () => {
    writeScoped(null, 'nav.favorites', ['/users']);

    expect(window.localStorage.length).toBe(0);
    expect(readScoped(null, 'nav.favorites', ['fallback'])).toEqual(['fallback']);
  });

  it('survives a corrupt stored value', () => {
    window.localStorage.setItem(`u.${storageScope(admin)}.nav.favorites`, '{not json');

    expect(readScoped(storageScope(admin), 'nav.favorites', ['x'])).toEqual(['x']);
  });
});

describe('purgeLegacyUiStorage', () => {
  it('deletes what older versions left under shared keys, and keeps scoped data', () => {
    window.localStorage.setItem('nav.recentRecords', '[]');
    window.localStorage.setItem('nav.favorites', '[]');
    window.localStorage.setItem('nav.expandedGroups', '[]');
    window.localStorage.setItem('savedViews.employees', '[]');
    window.localStorage.setItem('construction.locale', 'sr');
    writeScoped(storageScope(admin), 'savedViews.employees', [{ id: '1' }]);

    purgeLegacyUiStorage();

    expect(window.localStorage.getItem('nav.recentRecords')).toBeNull();
    expect(window.localStorage.getItem('nav.favorites')).toBeNull();
    expect(window.localStorage.getItem('nav.expandedGroups')).toBeNull();
    expect(window.localStorage.getItem('savedViews.employees')).toBeNull();
    expect(window.localStorage.getItem('construction.locale')).toBe('sr');
    expect(readScoped(storageScope(admin), 'savedViews.employees', [])).toHaveLength(1);
  });
});
