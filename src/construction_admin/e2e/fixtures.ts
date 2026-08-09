import { expect, test as base, type Locator, type Page } from '@playwright/test';

/**
 * Shared setup for the browser suite.
 *
 * Two things every test needs and neither should repeat: a pinned language,
 * and a signed-in session.
 */

/** The account the API seeds for this suite. See playwright.config.ts. */
export const OPERATOR = {
  email: 'e2e@construction.local',
  password: 'E2ePassword123!',
};

/**
 * Pins the language before the app boots.
 *
 * Without this the suite reads whatever language Chromium's `Accept-Language`
 * happens to select, which is English on a CI runner and could be anything on
 * a developer's machine — so a selector written against one would fail on the
 * other for a reason that has nothing to do with the code. `bilingual.spec.ts`
 * is the one place that deliberately varies it.
 */
export async function setLanguage(page: Page, locale: 'sr' | 'en') {
  await page.addInitScript(
    (value) => window.localStorage.setItem('construction.locale', value),
    locale,
  );
}

/**
 * A navigation link, and only the one that is on screen.
 *
 * The layout keeps *two* drawers in the DOM at all times — a permanent one for
 * wide screens and a temporary one for narrow, mounted eagerly so opening it
 * is instant. So every nav link matches twice, and an unfiltered locator is a
 * strict-mode violation rather than a missing element: the failure reads
 * "waiting for…" and sends you looking for a link that is right there.
 */
export function navLink(page: Page, name: string): Locator {
  return page.getByRole('link', { name, exact: true }).filter({ visible: true });
}

/** The form's submit control, by role rather than by its label. */
export function submitButton(page: Page): Locator {
  return page.locator('form button[type="submit"]');
}

/** Signs in through the real form against the real API. */
export async function signIn(page: Page) {
  await page.goto('/login');

  // By input type rather than by label: this helper runs in both languages,
  // and the labels differ ("Email" / "E-mail", "Password" / "Lozinka").
  await page.locator('input[type="email"]').fill(OPERATOR.email);
  await page.locator('input[type="password"]').fill(OPERATOR.password);
  await submitButton(page).click();

  // Wait for the layout, not for a fixed number of milliseconds — and for the
  // app bar rather than the drawer: the docked drawer's outer element has no
  // box of its own, so Playwright reads it as hidden even on a wide screen.
  // The app bar is the same in both languages and only exists behind a
  // session, which makes it the honest signal that sign-in finished.
  await expect(page).not.toHaveURL(/\/login/);
  await expect(page.locator('.MuiAppBar-root')).toBeVisible();
}

/**
 * A test that starts signed in, in English.
 *
 * Signing in through the form each time rather than injecting a token: it is
 * two seconds, and it means every run exercises the one flow whose failure
 * locks everybody out.
 */
export const test = base.extend<{ signedIn: Page }>({
  // The second argument is Playwright's "now run the test" callback. Named
  // `run` rather than its conventional `use`, because a lint rule that watches
  // for React hooks reads `use(...)` as one and fails the build.
  signedIn: async ({ page }, run) => {
    await setLanguage(page, 'en');
    await signIn(page);
    await run(page);
  },
});

export { expect };

/**
 * A name nothing else will have.
 *
 * The suite runs against a database it shares with its own other tests, in
 * parallel. Fixed names collide, and a collision looks like a bug in the code
 * under test rather than in the test.
 */
export function unique(prefix: string): string {
  return `${prefix}-${Math.random().toString(36).slice(2, 8)}`;
}
