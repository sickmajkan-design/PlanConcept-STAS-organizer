import { expect, test as base } from '@playwright/test';

import { navLink, OPERATOR, signIn, setLanguage } from './fixtures';

/**
 * Signing in, staying signed in, and the cookie that makes both work.
 *
 * The refresh token is an `HttpOnly` cookie, which is a claim no test outside
 * a browser can check: jsdom has no cookie jar the app does not control, and
 * the backend tests can only assert the `Set-Cookie` header. Here the browser
 * itself is the evidence — the cookie exists, script cannot read it, and a
 * reload still lands on a signed-in page.
 */
base.describe('the session', () => {
  base('a wrong password is refused, in words', async ({ page }) => {
    await setLanguage(page, 'en');
    await page.goto('/login');

    await page.getByLabel('Email').fill(OPERATOR.email);
    await page.getByLabel('Password', { exact: true }).fill('definitely-not-the-password');
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page.getByRole('alert')).toBeVisible();
    await expect(page).toHaveURL(/\/login$/);
  });

  base('the refresh token is a cookie no script can read', async ({ page, context }) => {
    await setLanguage(page, 'en');
    await signIn(page);

    const cookies = await context.cookies();
    const refresh = cookies.find((cookie) => cookie.name.toLowerCase().includes('refresh'));

    expect(refresh, 'the API set no refresh cookie').toBeDefined();

    // The whole point of moving it out of localStorage: an XSS on this page
    // must not be able to walk off with a credential that outlives the tab.
    expect(refresh!.httpOnly).toBe(true);
    expect(refresh!.sameSite).toBe('Strict');

    const readable = await page.evaluate(() => document.cookie);

    expect(readable).not.toContain(refresh!.value);

    // And what *is* in localStorage is the short-lived half.
    const stored = await page.evaluate(() =>
      JSON.stringify(window.localStorage),
    );

    expect(stored).not.toContain(refresh!.value);
  });

  base('a reload keeps the operator where they were', async ({ page }) => {
    await setLanguage(page, 'en');
    await signIn(page);

    await page.goto('/employees');
    await expect(navLink(page, 'Employees')).toBeVisible();

    await page.reload();

    // Not bounced to sign-in. The guard has a third state — still restoring —
    // and treating it as signed-out sends a returning operator to the login
    // screen on every reload.
    await expect(page).toHaveURL(/\/employees$/);
    await expect(navLink(page, 'Employees')).toBeVisible();
  });

  base('signing out ends the session for good', async ({ page }) => {
    await setLanguage(page, 'en');
    await signIn(page);

    // The avatar button: last in the app bar, and labelled only by the
    // operator's initials, so there is no stable name to ask for.
    await page.locator('.MuiAppBar-root button').last().click();
    await page.getByRole('menuitem', { name: 'Sign out' }).click();

    await expect(page).toHaveURL(/\/login$/);

    // Going back must not resurrect it. A sign-out that only navigates leaves
    // the next person at the shared site tablet inside somebody's account.
    await page.goto('/employees');
    await expect(page).toHaveURL(/\/login/);
  });
});
