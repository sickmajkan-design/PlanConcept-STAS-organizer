import { expect, test as base } from '@playwright/test';

import { navLink, OPERATOR, signIn, setLanguage } from './fixtures';

/**
 * Both languages, in the browser that has to render them.
 *
 * The Vitest suite checks the dictionaries against each other and the screens
 * against the dictionaries. What it cannot check is the `lang` attribute on
 * `<html>`, because jsdom has no screen reader and no browser translate
 * prompt to be misled by it — and that attribute is set by an effect that
 * nothing else asserts.
 */
base.describe('language', () => {
  base('opens in Serbian and says so to a screen reader', async ({ page }) => {
    await setLanguage(page, 'sr');
    await page.goto('/login');

    await expect(page.getByRole('button', { name: 'Prijavi se' })).toBeVisible();

    // `index.html` ships `lang="en"`. Without the effect that corrects it, a
    // screen reader pronounces Serbian text with English phonemes for exactly
    // the readers the translation was for.
    await expect(page.locator('html')).toHaveAttribute('lang', 'sr-Latn');
  });

  base('every screen is translated, not only the chrome', async ({ page }) => {
    // The failure this catches is a single hardcoded string — which is what it
    // found on its first run: the employee form's submit button read "Create
    // employee" in the Serbian UI, with the translation sitting unused in the
    // dictionary. The dictionary-parity test could not see it, because the key
    // was present and correct; nothing was calling it.
    await setLanguage(page, 'sr');
    await signIn(page);

    await page.goto('/employees/new');

    await expect(page.getByRole('button', { name: 'Kreiraj zaposlenog' })).toBeVisible();

    const form = page.locator('form');

    await expect(form).not.toContainText('Create employee');
    await expect(form).not.toContainText('Save changes');
  });

  base('switches without a reload, and the choice survives one', async ({ page }) => {
    // Deliberately *not* using `setLanguage` here: it pins the preference on
    // every page load, which is exactly what this test needs not to happen.
    // Chromium reports en-US, so the app opens in English on its own and the
    // switch under test is the one a Serbian-speaking operator makes.
    await signIn(page);

    await expect(navLink(page, 'Employees')).toBeVisible();

    await page.getByRole('button', { name: 'Language' }).click();
    await page.getByRole('menuitem', { name: 'Srpski' }).click();

    // No reload in between — the whole app re-renders in place.
    await expect(navLink(page, 'Zaposleni')).toBeVisible();
    await expect(page.locator('html')).toHaveAttribute('lang', 'sr-Latn');

    await page.reload();

    // The choice is a preference, not a session setting. Losing it on every
    // reload is the kind of thing people stop reporting and start working
    // around.
    await expect(navLink(page, 'Zaposleni')).toBeVisible();
    await expect(page.locator('html')).toHaveAttribute('lang', 'sr-Latn');
  });

  base('the sign-in screen itself is translated', async ({ page }) => {
    // Reached before any preference can be read from a profile, and the one
    // screen a worker who cannot read English has to get through first.
    await setLanguage(page, 'sr');
    await page.goto('/login');

    await expect(page.getByLabel('Lozinka', { exact: true })).toBeVisible();
    await expect(page.getByLabel('E-mail', { exact: true })).toBeVisible();

    await page.locator('input[type="email"]').fill(OPERATOR.email);
    await page.locator('input[type="password"]').fill('wrong-password');
    await page.locator('form button[type="submit"]').click();

    const alert = page.getByRole('alert');

    await expect(alert).toBeVisible();

    // And here is a real gap, asserted rather than glossed over: the API is
    // not localised, so the *server's* explanation arrives in English even
    // when everything around it is Serbian. The client shows it because a
    // specific English sentence beats a vague translated one — the same
    // trade-off the mobile app makes. Closing it properly means translating
    // the API's problem-details, which is a backend change.
    //
    // The assertion is what is true today. If the API is ever localised this
    // test fails, which is the right moment to delete it.
    await expect(alert).toContainText('Invalid email or password.');
  });
});
