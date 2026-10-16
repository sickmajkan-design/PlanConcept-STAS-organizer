import { expect, submitButton, test, unique } from './fixtures';

const dropOn = (selector: string) =>
  `(files) => {
    const data = new DataTransfer();
    for (const f of files) data.items.add(new File([f.body], f.name, { type: 'text/plain' }));
    const target = ${selector};
    for (const type of ['dragenter', 'dragover', 'drop']) {
      target.dispatchEvent(new DragEvent(type, { bubbles: true, cancelable: true, dataTransfer: data }));
    }
  }`;

test('a file dropped anywhere on the documents page opens the upload with the file in it', async ({ signedIn: page }) => {
  await page.goto('/documents/expiring');
  // The page's own button, so the page (and its drop listener) is really there.
  await expect(page.getByRole('button', { name: /Upload/ }).first()).toBeVisible();

  const name = `${unique('bilo-gdje')}.txt`;

  // Only the dragenter first: the overlay must show while the file is over the page.
  await page.evaluate(() => {
    const data = new DataTransfer();
    data.items.add(new File(['x'], 'a.txt', { type: 'text/plain' }));
    document.body.dispatchEvent(new DragEvent('dragenter', { bubbles: true, cancelable: true, dataTransfer: data }));
  });
  await expect(page.getByTestId('page-drop-overlay')).toBeVisible();

  await page.evaluate(
    `(${dropOn('document.body')})([{ name: ${JSON.stringify(name)}, body: 'sadrzaj' }])`,
  );

  await expect(page.getByTestId('page-drop-overlay')).toBeHidden();
  await expect(page.getByRole('dialog').getByText(name)).toBeVisible();
});

test('each file of one drop can have its own type', async ({ signedIn: page, request }) => {
  const number = unique('CAT');

  await page.goto('/employees/new');
  await page.getByLabel('Employee number').fill(number);
  await page.getByLabel('Position').fill('Zidar');
  await page.getByLabel('First name').fill('Kate');
  await page.getByLabel('Last name').fill('Gorija');
  await page.getByLabel('Employment date').fill('2024-01-15');
  await submitButton(page).click();
  await expect(page).toHaveURL(/\/employees(\/[0-9a-f-]+)?$/);

  await page.goto('/employees');
  await page.getByRole('textbox').first().fill(number);
  await page.locator('.MuiDataGrid-row', { hasText: number }).first().click();

  const target = page.getByTestId('attachment-drop-target');
  await expect(target).toBeVisible();

  const first = `${unique('ugovor')}.txt`;
  const second = `${unique('potvrda')}.txt`;

  await target.evaluate(
    new Function(
      'element',
      `(${dropOn('element')})([{ name: ${JSON.stringify(first)}, body: 'a' }, { name: ${JSON.stringify(second)}, body: 'b' }])`,
    ) as (e: Element) => void,
  );

  const dialog = page.getByRole('dialog');
  await expect(dialog.getByText(first)).toBeVisible();
  await expect(dialog.getByText(second)).toBeVisible();

  // The general type is Contract; the second file is turned into a Certificate on its own.
  await dialog.getByRole('combobox', { name: new RegExp(`Type: ${second}`) }).click();
  await page.getByRole('option', { name: 'Certificate' }).click();

  await dialog.getByRole('button', { name: /^Upload$/ }).click();
  await expect(dialog).toBeHidden();

  await expect(page.getByText(first)).toBeVisible();
  await expect(page.getByText(second)).toBeVisible();

  // What the server stored, not what the row happens to print.
  const api = `http://localhost:${process.env.E2E_API_PORT ?? 5199}`;
  const login = await request.post(`${api}/api/v1/auth/login`, {
    data: { email: 'e2e@construction.local', password: 'E2ePassword123!' },
  });
  const headers = { Authorization: `Bearer ${(await login.json()).accessToken as string}` };
  const employees = await (await request.get(`${api}/api/v1/employees`, { headers, params: { search: number } })).json();
  const id = (employees.items ?? employees)[0].id as string;
  const stored = await (
    await request.get(`${api}/api/v1/attachments`, { headers, params: { ownerType: 'Employee', ownerId: id } })
  ).json();
  const categoryOf = (file: string) => (stored as { fileName: string; category: string }[]).find((a) => a.fileName === file)?.category;

  expect(categoryOf(first)).toBe('Contract');
  expect(categoryOf(second)).toBe('Certificate');
});

test('the panel asks the server for the upload limits', async ({ signedIn: page }) => {
  const asked = page.waitForResponse((r) => r.url().includes('/api/v1/attachments/limits') && r.ok());

  await page.goto('/documents/expiring');
  await page.getByRole('button', { name: /Upload/ }).first().click();
  await asked;
});

test('the month import of the book takes a dragged spreadsheet through the shared drop area', async ({ signedIn: page }) => {
  await page.goto('/book/import');
  const area = page.getByTestId('drop-area').first();
  await expect(area).toBeVisible();

  await area.evaluate((element) => {
    const data = new DataTransfer();
    data.items.add(new File(['x'], 'mjesec.xlsx'));
    element.dispatchEvent(new DragEvent('dragenter', { bubbles: true, cancelable: true, dataTransfer: data }));
  });

  await expect(area).toHaveAttribute('data-over', 'true');
});
