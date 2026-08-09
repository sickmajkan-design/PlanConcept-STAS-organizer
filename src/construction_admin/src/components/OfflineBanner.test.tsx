/**
 * @vitest-environment jsdom
 */
import { act, fireEvent, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { renderScreen } from '../test/renderScreen';
import { OfflineBanner } from './OfflineBanner';

/**
 * The banner that explains a screen which has stopped moving.
 *
 * Everything here turns on `navigator.onLine`, which jsdom exposes as a plain
 * getter that always says `true`. Redefining it is the only way to reach the
 * offline branch at all; dispatching the event without redefining it would
 * have the component re-read the store and find itself online again, so the
 * two always go together.
 */
function setOnline(value: boolean) {
  Object.defineProperty(window.navigator, 'onLine', {
    value,
    configurable: true,
  });
}

/** What the browser does: flip the flag, then fire the event. */
function goOffline() {
  setOnline(false);
  fireEvent(window, new Event('offline'));
}

function goOnline() {
  setOnline(true);
  fireEvent(window, new Event('online'));
}

beforeEach(() => {
  window.localStorage.setItem('construction.locale', 'sr');
  setOnline(true);
});

afterEach(() => {
  setOnline(true);
});

describe('OfflineBanner', () => {
  it('says nothing while the connection is up', () => {
    renderScreen(<OfflineBanner />);

    expect(screen.queryByRole('status')).toBeNull();
  });

  it('appears when the connection drops', () => {
    renderScreen(<OfflineBanner />);

    goOffline();

    // The point of the whole component: React Query pauses rather than fails
    // when offline, so without this the operator sees a spinner that never
    // resolves and no error anywhere.
    expect(screen.getByText('Nema veze sa internetom')).toBeDefined();
    expect(screen.getByRole('status')).toBeDefined();
  });

  it('is already there when the screen opens with no connection', () => {
    // The event never fires in this case — the tab was opened, or restored
    // from the background, while already offline. A component that only
    // listened for the event would stay silent through the whole outage.
    setOnline(false);

    renderScreen(<OfflineBanner />);

    expect(screen.getByText('Nema veze sa internetom')).toBeDefined();
  });

  it('confirms when the connection comes back, then gets out of the way', () => {
    vi.useFakeTimers();

    try {
      renderScreen(<OfflineBanner />);

      goOffline();
      goOnline();

      expect(screen.getByText(/Veza je uspostavljena/)).toBeDefined();

      act(() => {
        vi.advanceTimersByTime(6_000);
      });

      // A success message that stays becomes furniture, and then the next one
      // is not read either.
      expect(screen.queryByText(/Veza je uspostavljena/)).toBeNull();
      expect(screen.queryByRole('status')).toBeNull();
    } finally {
      vi.useRealTimers();
    }
  });

  it('does not congratulate a connection that never dropped', () => {
    // `online` fires on some browsers when an interface changes — switching
    // from wireless to wired, or a VPN connecting — with no outage in between.
    renderScreen(<OfflineBanner />);

    goOnline();

    expect(screen.queryByRole('status')).toBeNull();
  });

  it('speaks the operator’s language', () => {
    window.localStorage.setItem('construction.locale', 'en');

    renderScreen(<OfflineBanner />);

    goOffline();

    expect(screen.getByText('No connection')).toBeDefined();
  });
});
