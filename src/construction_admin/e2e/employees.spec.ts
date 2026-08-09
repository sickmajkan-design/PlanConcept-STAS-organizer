import { expect, submitButton, test, unique } from './fixtures';

/**
 * The directory, in a real browser against the real API.
 *
 * The Vitest suite already covers this screen's logic against a fake network.
 * What it cannot cover is here: a grid that virtualises, a dialog that has to
 * be clickable rather than merely mounted, and — the one no fake can give —
 * whether the client and the server still agree about the shape of a request.
 */
test.describe('the employee directory', () => {
  test('a record created here is on the server, and comes back', async ({ signedIn: page }) => {
    // The seam neither other suite reaches. Vitest asserts the payload against
    // what the test author believed the API accepts; the backend tests assert
    // the handler against a command built in C#. Nothing until now put the two
    // halves in the same sentence, so a renamed field would pass both suites
    // and fail in front of an operator.
    const number = unique('E2E');

    await page.goto('/employees/new');

    await page.getByLabel('Employee number').fill(number);
    await page.getByLabel('Position').fill('Zidar');
    await page.getByLabel('First name').fill('Ivan');
    await page.getByLabel('Last name').fill('Horvat');
    await page.getByLabel('Employment date').fill('2024-01-15');

    await submitButton(page).click();

    // The app navigates on success, so arriving anywhere else means the save
    // was refused.
    await expect(page).toHaveURL(/\/employees(\/[0-9a-f-]+)?$/);

    // And it is really there: a fresh page load, a search that goes to the
    // server, and the row comes back.
    await page.goto('/employees');
    await page.getByRole('textbox').first().fill(number);

    await expect(page.getByText(number)).toBeVisible();
  });

  test('the grid paints rows that are actually visible', async ({ signedIn: page }) => {
    // In jsdom every element has zero height, so "the row is in the DOM" is
    // the strongest thing that suite can say. Here it has to be on screen and
    // have a size, which is what catches a grid whose container collapsed —
    // rows present, page apparently empty.
    await page.goto('/employees');

    const firstCell = page.locator('.MuiDataGrid-row').first();

    await expect(firstCell).toBeVisible();

    const box = await firstCell.boundingBox();

    expect(box).not.toBeNull();
    expect(box!.height).toBeGreaterThan(10);
    expect(box!.width).toBeGreaterThan(200);
  });

  test('Delete opens the dialog and stays on the list', async ({ signedIn: page }) => {
    // The bug the Vitest suite found on five screens, pinned where it
    // originally happened: the row has an onRowClick that navigates, the
    // Delete button sits inside the row, and without stopPropagation the
    // dialog was mounted and unmounted in the same tick. In jsdom that is a
    // missing element; in a browser it is a screen that flickers and does
    // nothing, which is what an operator reported.
    const number = unique('DEL');

    await page.goto('/employees/new');
    await page.getByLabel('Employee number').fill(number);
    await page.getByLabel('Position').fill('Zidar');
    await page.getByLabel('First name').fill('Ana');
    await page.getByLabel('Last name').fill('Marić');
    await page.getByLabel('Employment date').fill('2024-02-01');
    await submitButton(page).click();

    await page.goto('/employees');
    await page.getByRole('textbox').first().fill(number);
    await expect(page.getByText(number)).toBeVisible();

    const row = page.locator('.MuiDataGrid-row', { hasText: number });

    await row.getByRole('button', { name: 'Delete' }).click();

    const dialog = page.getByRole('dialog');

    // Still on the list, with a dialog that stayed open long enough to be
    // read. The URL assertion is the actual regression check.
    await expect(dialog).toBeVisible();
    await expect(page).toHaveURL(/\/employees$/);

    await dialog.getByRole('button', { name: 'Delete' }).click();

    await expect(dialog).toBeHidden();
    await expect(page.getByText(number)).toBeHidden();
  });

  test('a duplicate employee number is refused by the server and shown', async ({
    signedIn: page,
  }) => {
    // A 409 raised by a real unique index rather than by a fake adapter
    // returning what the test asked for. This is the path where the API's
    // problem-details shape and the client's error handling have to agree.
    const number = unique('DUP');

    for (const first of ['Marko', 'Petar']) {
      await page.goto('/employees/new');
      await page.getByLabel('Employee number').fill(number);
      await page.getByLabel('Position').fill('Zidar');
      await page.getByLabel('First name').fill(first);
      await page.getByLabel('Last name').fill('Petrović');
      await page.getByLabel('Employment date').fill('2024-03-01');
      await submitButton(page).click();

      if (first === 'Marko') {
        await expect(page).toHaveURL(/\/employees(\/[0-9a-f-]+)?$/);
      }
    }

    // The server's own words, not a generic fallback.
    await expect(page.getByText(/already in use/i)).toBeVisible();
    await expect(page).toHaveURL(/\/employees\/new$/);
  });
});
