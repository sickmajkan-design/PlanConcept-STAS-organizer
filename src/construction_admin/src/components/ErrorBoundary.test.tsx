/**
 * @vitest-environment jsdom
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { renderScreen } from '../test/renderScreen';
import { ErrorBoundary } from './ErrorBoundary';
import { RootErrorFallback } from './RootErrorFallback';
import { RouteErrorFallback } from './RouteErrorFallback';

/**
 * The boundary, exercised by actually throwing during render.
 *
 * There is no way to test this honestly without a component that throws: the
 * behaviour being asserted is React's unmount-the-whole-tree rule, and the
 * only thing that triggers it is a real exception on the render path.
 *
 * React writes every caught error to the console, so a passing run would print
 * three stack traces and teach whoever reads the output to ignore red text.
 * The spy silences that — and doubles as the assertion in the last test, which
 * checks the component stack is still reported to somebody.
 */
beforeEach(() => {
  window.localStorage.clear();
  vi.spyOn(console, 'error').mockImplementation(() => {});
});

function Boom({ explode }: { explode: boolean }) {
  if (explode) {
    throw new Error('render exploded');
  }

  return <div>screen content</div>;
}

describe('ErrorBoundary', () => {
  it('renders its children while nothing throws', () => {
    renderScreen(
      <ErrorBoundary fallback={() => <p>fallback</p>}>
        <Boom explode={false} />
      </ErrorBoundary>,
    );

    expect(screen.getByText('screen content')).toBeDefined();
    expect(screen.queryByText('fallback')).toBeNull();
  });

  it('replaces a screen that throws instead of taking the page down with it', () => {
    renderScreen(
      <ErrorBoundary fallback={() => <p>fallback</p>}>
        <Boom explode />
      </ErrorBoundary>,
    );

    expect(screen.getByText('fallback')).toBeDefined();

    // The half that matters: React has unmounted the subtree, so a half-drawn
    // screen is not left behind the message.
    expect(screen.queryByText('screen content')).toBeNull();
  });

  it('hands the thrown error to the fallback', () => {
    renderScreen(
      <ErrorBoundary
        fallback={({ error }) => <p>{(error as Error).message}</p>}
      >
        <Boom explode />
      </ErrorBoundary>,
    );

    expect(screen.getByText('render exploded')).toBeDefined();
  });

  it('renders the children again when the fallback resets', async () => {
    // The retry button. Worth pinning because a boundary that cannot be reset
    // is a one-way door: every later render returns the fallback from state,
    // however healthy the children have become.
    const user = userEvent.setup();

    function Flaky() {
      const [attempt, setAttempt] = useState(0);

      return (
        <ErrorBoundary
          fallback={({ reset }) => (
            <button
              type="button"
              onClick={() => {
                setAttempt(1);
                reset();
              }}
            >
              try again
            </button>
          )}
        >
          <Boom explode={attempt === 0} />
        </ErrorBoundary>
      );
    }

    renderScreen(<Flaky />);

    await user.click(screen.getByRole('button', { name: 'try again' }));

    expect(await screen.findByText('screen content')).toBeDefined();
  });

  it('clears itself when the reset key changes, so navigating away recovers', async () => {
    // Without `resetKey` the operator is stuck: the fallback stays mounted
    // wherever they navigate next, and only a full page reload clears it. The
    // button is outside the boundary on purpose — that is where the real
    // navigation lives, in the layout around the crashed screen.
    const user = userEvent.setup();

    function Navigable() {
      const [path, setPath] = useState('/broken');

      return (
        <>
          <button type="button" onClick={() => setPath('/fine')}>
            go elsewhere
          </button>
          <ErrorBoundary resetKey={path} fallback={() => <p>fallback</p>}>
            <Boom explode={path === '/broken'} />
          </ErrorBoundary>
        </>
      );
    }

    renderScreen(<Navigable />);

    expect(screen.getByText('fallback')).toBeDefined();

    await user.click(screen.getByRole('button', { name: 'go elsewhere' }));

    expect(await screen.findByText('screen content')).toBeDefined();
    expect(screen.queryByText('fallback')).toBeNull();
  });

  it('reports the error and the component stack rather than swallowing them', () => {
    const seen: unknown[] = [];

    renderScreen(
      <ErrorBoundary
        onError={(error) => seen.push(error)}
        fallback={() => <p>fallback</p>}
      >
        <Boom explode />
      </ErrorBoundary>,
    );

    expect((seen[0] as Error).message).toBe('render exploded');

    // A boundary that shows a friendly message and tells nobody what happened
    // makes the bug invisible: the operator sees a screen they can navigate
    // away from, and nothing anywhere records why.
    const logged = vi.mocked(console.error).mock.calls;

    expect(
      logged.some(([first]) => first === 'Unhandled render error'),
    ).toBe(true);
  });
});

describe('RouteErrorFallback', () => {
  /**
   * Both languages, because a screen that fails in the wrong one fails twice.
   *
   * The locale is set through `localStorage` rather than left to the default:
   * jsdom reports `en-US` as the browser preference, so a test that asserted
   * "whatever it happens to render" would be asserting English on every run
   * and would keep passing if the Serbian text disappeared entirely.
   */
  it.each([
    ['sr', 'Ova stranica ne može da se prikaže', 'Pokušaj ponovo', 'Nazad na početak'],
    ['en', 'This page could not be displayed', 'Try again', 'Back to the start'],
  ])('explains the failure in %s and offers a way out', (locale, title, retry, home) => {
    window.localStorage.setItem('construction.locale', locale);

    renderScreen(
      <ErrorBoundary fallback={(props) => <RouteErrorFallback {...props} />}>
        <Boom explode />
      </ErrorBoundary>,
    );

    expect(screen.getByText(title)).toBeDefined();
    expect(screen.getByRole('button', { name: retry })).toBeDefined();

    // A link rather than a reset: the screen that just threw is the one the
    // operator needs to leave, and re-rendering it in place is the other
    // button.
    expect(screen.getByRole('link', { name: home }).getAttribute('href')).toBe('/');
  });

  it('does not put the exception message on screen', () => {
    renderScreen(
      <ErrorBoundary fallback={(props) => <RouteErrorFallback {...props} />}>
        <Boom explode />
      </ErrorBoundary>,
    );

    // On a production build this is minified React internals at best, and at
    // worst a fragment of the record that failed to render — a name, an
    // address — shown to whoever is standing at the screen.
    expect(screen.queryByText(/render exploded/)).toBeNull();
  });
});

describe('RootErrorFallback', () => {
  it('renders with no providers at all', () => {
    // Rendered bare on purpose, and this is the assertion rather than a
    // shortcut: it is the fallback for the boundary above `I18nProvider` and
    // `ThemeProvider`, so needing either of them is the one way it can fail —
    // by throwing inside the boundary that was meant to be the last one.
    render(<RootErrorFallback />);

    expect(screen.getByText('Aplikacija se nije učitala')).toBeDefined();

    // Both languages at once. It cannot ask which one to use without reaching
    // for the provider that may be the thing that just threw.
    expect(screen.getByText('The application failed to load')).toBeDefined();
    expect(screen.getByRole('button', { name: /Ponovo učitaj \/ Reload/ })).toBeDefined();
  });
});
