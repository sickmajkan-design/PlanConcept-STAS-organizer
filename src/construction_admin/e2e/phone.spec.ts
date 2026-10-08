import { expect, test as base } from '@playwright/test';

import { setLanguage, signIn } from './fixtures';

/**
 * The panel on a phone, in the hand of someone standing on a site.
 *
 * Opening the platform in a phone's browser has to be enough to run the business
 * from: see what is waiting, find a page, approve hours, book leave. The layout is
 * responsive by construction; these tests assert the result — that the bottom bar
 * is there and reaches the pages, that the filters above a list fold behind one
 * button, that a dialog is a sheet a thumb can reach, that nothing needs a
 * sideways swipe, and that what a finger is asked to hit is big enough to hit.
 *
 * Run only in the `phone` project, which emulates a touch screen
 * (`pointer: coarse`) — the theme's touch sizes apply to nothing else.
 */
base.describe('on a phone', () => {
  base.beforeEach(async ({ page }) => {
    await setLanguage(page, 'en');
    await signIn(page);
  });

  base('the bottom bar reaches the main pages and the rest', async ({ page }) => {
    const bar = page.getByRole('navigation', { name: 'Main navigation' });
    await expect(bar).toBeVisible();

    await bar.getByRole('link', { name: 'Schedule' }).click();
    await expect(page).toHaveURL(/\/schedule$/);

    // "More" opens the same menu the hamburger does, which still holds every page.
    await bar.getByRole('button', { name: 'More' }).click();
    await expect(page.getByRole('button', { name: 'Directory', exact: true })).toBeVisible();
  });

  base('the filters above a list fold behind one button', async ({ page }) => {
    await page.goto('/employees');

    const filters = page.getByRole('button', { name: /^Filters/ });
    await expect(filters).toBeVisible();
    const status = page.getByRole('combobox', { name: 'Status' });
    await expect(status).toHaveCount(0);

    await filters.click();
    await expect(status).toBeVisible();
  });

  base('a dialog rises from the bottom edge, full width', async ({ page }) => {
    await page.goto('/absences');
    await page.getByRole('button', { name: /Book time off/ }).click();

    const paper = page.locator('.MuiDialog-paper').first();
    await expect(paper).toBeVisible();

    const box = await paper.boundingBox();
    const viewport = page.viewportSize()!;
    expect(box).not.toBeNull();
    expect(Math.round(box!.width)).toBe(viewport.width);
    expect(Math.round(box!.y + box!.height)).toBeGreaterThanOrEqual(viewport.height - 1);

    // Its buttons are as big as a thumb needs, and not clipped under the edge.
    for (const height of await page
      .locator('.MuiDialogActions-root button')
      .evaluateAll((els) => els.map((el) => el.getBoundingClientRect().height))) {
      expect(height).toBeGreaterThanOrEqual(44);
    }
  });

  base('nothing scrolls sideways', async ({ page }) => {
    // Six pages, each compiled on first visit by the dev server.
    base.setTimeout(180_000);
    for (const path of ['/', '/employees', '/projects', '/time-entries', '/schedule', '/absences']) {
      await page.goto(path);
      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
      );
      expect(overflow, `${path} scrolls sideways by ${overflow}px`).toBeLessThanOrEqual(1);
    }
  });

  base('what a finger must hit is big enough to hit', async ({ page }) => {
    await page.goto('/employees');

    // The bar at the top and the bar at the bottom are on every screen, so every
    // control in them is held to the 44-pixel rule.
    const small = await page
      .locator('.MuiAppBar-root button, nav[aria-label="Main navigation"] a, nav[aria-label="Main navigation"] button')
      .evaluateAll((els) =>
        els
          .map((el) => ({ el, box: el.getBoundingClientRect() }))
          .filter(({ box }) => box.width > 0 && (box.height < 43.5 || box.width < 43.5))
          .map(({ el, box }) => `${el.getAttribute('aria-label') ?? el.textContent?.trim()} ${Math.round(box.width)}x${Math.round(box.height)}`),
      );

    expect(small, `too small to hit: ${small.join(', ')}`).toEqual([]);
  });
});
