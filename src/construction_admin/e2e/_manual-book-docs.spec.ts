import { expect, test } from '@playwright/test';

const API = 'http://localhost:5199';
const CREDS = { email: 'probaj@construction.local', password: 'ProbajPassword123!' };

test('a document is filed on an entry of the book and counted in the Dok. column', async ({ page, request }) => {
  test.setTimeout(150_000);
  const login = await request.post(`${API}/api/v1/auth/login`, { data: CREDS });
  const headers = { Authorization: `Bearer ${(await login.json()).accessToken as string}` };

  const cats = await (await request.get(`${API}/api/v1/book/categories`, { headers })).json();
  const category = cats.find((c: { systemKey: string }) => c.systemKey === 'Tools').id as string;
  const today = new Date().toISOString().slice(0, 10);
  const description = `Dok proba ${Date.now() % 100000}`;

  const created = await request.post(`${API}/api/v1/book/entries`, {
    headers,
    data: { kind: 'Cost', amount: 77, occurredOn: today, categoryId: category, description, branchId: null, projectId: null },
  });
  expect(created.ok(), await created.text()).toBeTruthy();

  await page.addInitScript(() => window.localStorage.setItem('construction.locale', 'sr'));
  await page.goto('/login');
  await page.locator('input[type="email"]').fill(CREDS.email);
  await page.locator('input[type="password"]').fill(CREDS.password);
  await page.locator('form button[type="submit"]').click();
  await expect(page.locator('.MuiAppBar-root')).toBeVisible();
  const notes = page.getByRole('dialog');
  try {
    await notes.waitFor({ state: 'visible', timeout: 3000 });
    await notes.getByRole('button').last().click();
  } catch { /* none */ }

  await page.goto('/book/entries');
  const row = page.locator('tr', { hasText: description });
  await expect(row).toBeVisible({ timeout: 20_000 });
  await page.screenshot({ path: 'e2e/_shots/try-book-docs-1.png' });

  await row.getByRole('button', { name: /Dokumenti uz ovaj zapis: 0/ }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog.getByTestId('attachment-drop-target')).toBeVisible();

  await dialog.getByTestId('attachment-drop-target').evaluate((element) => {
    const data = new DataTransfer();
    data.items.add(new File(['%PDF-1.4\nracun'], 'racun-alat.pdf', { type: 'application/pdf' }));
    for (const type of ['dragenter', 'dragover', 'drop']) {
      element.dispatchEvent(new DragEvent(type, { bubbles: true, cancelable: true, dataTransfer: data }));
    }
  });

  const upload = page.getByRole('dialog').last();
  await expect(upload.getByText('racun-alat.pdf')).toBeVisible();
  await upload.getByRole('button', { name: 'Otpremi' }).click();
  await expect(page.getByText('racun-alat.pdf').first()).toBeVisible({ timeout: 20_000 });
  await page.screenshot({ path: 'e2e/_shots/try-book-docs-2.png' });

  await page.getByRole('dialog').getByRole('button', { name: 'Zatvori' }).click();
  await expect(row.getByRole('button', { name: /Dokumenti uz ovaj zapis: 1/ })).toBeVisible({ timeout: 20_000 });
  await page.screenshot({ path: 'e2e/_shots/try-book-docs-3.png' });
});
