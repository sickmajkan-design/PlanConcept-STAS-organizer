import { expect, test } from '@playwright/test';

import { OPERATOR, dismissReleaseNotes, openRailGroup, setLanguage, submitButton, unique } from './fixtures';

/**
 * The book of money (Evidencija) is closed to everybody except the Super Admin until one switches it on
 * for an account. The finance grant, which opens the older money pages, must not open it by itself.
 */
const API = `http://localhost:${process.env.E2E_API_PORT ?? 5199}`;

test('the book is closed to an admin until the Super Admin grants it', async ({ page, request }) => {
  test.setTimeout(120_000);

  const login = await request.post(`${API}/api/v1/auth/login`, {
    data: { email: OPERATOR.email, password: OPERATOR.password },
  });
  const headers = { Authorization: `Bearer ${(await login.json()).accessToken as string}` };

  const email = `${unique('admin')}@example.test`;
  const password = 'AdminPassword123!';

  const created = await request.post(`${API}/api/v1/users`, {
    headers,
    data: { email, password, role: 'Admin' },
  });
  expect(created.ok(), await created.text()).toBeTruthy();
  const user = await created.json();

  // The older finance grant alone, as in production before this change.
  const update = (extra: object) =>
    request.put(`${API}/api/v1/users/${user.id}`, {
      headers,
      data: { email, role: 'Admin', financeAccess: 'Full', ...extra },
    });

  expect((await update({ bookAccess: false })).ok()).toBeTruthy();

  await setLanguage(page, 'sr');
  await page.goto('/login');
  await page.locator('input[type="email"]').fill(email);
  await page.locator('input[type="password"]').fill(password);
  await submitButton(page).click();
  await expect(page.locator('.MuiAppBar-root')).toBeVisible();
  await dismissReleaseNotes(page);

  // No group in the menu, and the address sends the admin home.
  await expect(page.getByRole('button', { name: 'Evidencija', exact: true }).filter({ visible: true })).toHaveCount(0);
  await page.goto('/book');
  await expect(page).not.toHaveURL(/\/book/);

  // The API refuses as well, whatever the panel shows.
  const token = await page.evaluate(() =>
    Object.values(window.localStorage).find((v) => typeof v === 'string' && v.includes('accessToken')),
  );
  if (token) {
    const parsed = JSON.parse(token as string) as { accessToken?: string };
    const refused = await request.get(`${API}/api/v1/book/entries`, {
      headers: { Authorization: `Bearer ${parsed.accessToken}` },
    });
    expect(refused.status()).toBe(403);
  }

  // The Super Admin switches it on; the next load of the panel shows it.
  expect((await update({ bookAccess: true })).ok()).toBeTruthy();
  await page.reload();
  await expect(page.locator('.MuiAppBar-root')).toBeVisible();
  await dismissReleaseNotes(page);

  await openRailGroup(page, 'Evidencija');
  await page.goto('/book');
  await expect(page).toHaveURL(/\/book/);
  await expect(page.getByRole('heading', { name: /Pregled|Novac/i }).first()).toBeVisible();

  // Taking it away again closes it at the next request.
  expect((await update({ bookAccess: false })).ok()).toBeTruthy();
  await page.reload();
  await expect(page).not.toHaveURL(/\/book/);
});
