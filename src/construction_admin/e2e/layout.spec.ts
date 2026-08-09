import { expect, test as base } from '@playwright/test';

import { navLink, signIn, submitButton, setLanguage } from './fixtures';

/**
 * The panel on the screen it is actually used on.
 *
 * Half the time this is a tablet in a portakabin, not a 27-inch monitor. The
 * layout is responsive by construction — MUI breakpoints, a drawer that
 * collapses — but "responsive" is a property of the code and this is a
 * property of the result: nothing important off the bottom, nothing
 * overlapping, and the drawer still reachable once it has been hidden.
 *
 * Run only in the `tablet` project; see playwright.config.ts.
 */
base.describe('on a tablet', () => {
  base('the navigation is reachable once the drawer is hidden', async ({ page }) => {
    await setLanguage(page, 'en');
    await signIn(page);

    // At this width the permanent drawer is gone and the only way through is
    // the menu button. A layout change that hid both would strand the
    // operator on whatever page they landed on, and no unit test can see it.
    await page.locator('.MuiAppBar-root button').first().click();

    await expect(navLink(page, 'Employees')).toBeVisible();
    await navLink(page, 'Employees').click();

    await expect(page).toHaveURL(/\/employees$/);
  });

  base('the form fits: nothing important is off the bottom', async ({ page }) => {
    await setLanguage(page, 'en');
    await signIn(page);

    await page.goto('/employees/new');

    const submit = submitButton(page);

    // Scrolled to, not merely present. If the button cannot be brought into
    // view the form cannot be submitted, which is the failure this exists for.
    await submit.scrollIntoViewIfNeeded();
    await expect(submit).toBeInViewport();

    const box = await submit.boundingBox();
    const viewport = page.viewportSize()!;

    expect(box).not.toBeNull();
    expect(box!.x).toBeGreaterThanOrEqual(0);
    expect(box!.x + box!.width).toBeLessThanOrEqual(viewport.width);
  });

  base('the page never scrolls sideways', async ({ page }) => {
    await setLanguage(page, 'en');
    await signIn(page);

    for (const path of ['/', '/employees', '/employees/new']) {
      await page.goto(path);

      // Horizontal overflow is the classic responsive failure: it looks fine
      // until somebody swipes and finds half a table under the edge.
      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
      );

      expect(overflow, `${path} scrolls sideways by ${overflow}px`).toBeLessThanOrEqual(1);
    }
  });
});
