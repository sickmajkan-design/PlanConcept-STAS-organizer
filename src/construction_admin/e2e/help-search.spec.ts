import { expect, test, unique } from './fixtures';

const API = `http://localhost:${process.env.E2E_API_PORT ?? 5199}`;

async function token(request: import('@playwright/test').APIRequestContext) {
  const login = await request.post(`${API}/api/v1/auth/login`, {
    data: { email: 'e2e@construction.local', password: 'E2ePassword123!' },
  });

  return (await login.json()).accessToken as string;
}

test('Ctrl+K finds a guide, shows its steps and example, and leads to the screen', async ({ signedIn: page }) => {
  await page.goto('/');
  await page.keyboard.press('Control+k');

  const palette = page.getByRole('dialog');
  await palette.getByRole('textbox').fill('expiry reminders');
  await expect(palette.getByText('Expiry dates and reminders')).toBeVisible();
  await palette.getByText('Expiry dates and reminders').click();

  await expect(page).toHaveURL(/\/help\/dokumenti-rokovi$/);
  await expect(page.getByRole('heading', { name: 'Expiry dates and reminders' })).toBeVisible();
  await expect(page.getByText('Step by step')).toBeVisible();
  await expect(page.getByText('Example', { exact: true })).toBeVisible();

  await page.getByRole('link', { name: 'Open the screen' }).click();
  await expect(page).toHaveURL(/\/documents\/expiring/);
});

test('Ctrl+K finds a document by its label and opens it on the documents page', async ({ signedIn: page, request }) => {
  const bearer = { Authorization: `Bearer ${await token(request)}` };
  const label = unique('oznaka').toLowerCase();

  const employee = await (
    await request.post(`${API}/api/v1/employees`, {
      headers: bearer,
      data: { employeeNumber: unique('SRC'), position: 'Zidar', firstName: 'Pera', lastName: 'Pretraga', employmentDate: '2024-01-15' },
    })
  ).json();

  const uploaded = await request.post(`${API}/api/v1/attachments`, {
    headers: bearer,
    multipart: {
      ownerType: 'Employee',
      ownerId: employee.id,
      category: 'Other',
      title: 'Dokument za pretragu',
      tags: label,
      file: { name: 'pretraga.txt', mimeType: 'text/plain', buffer: Buffer.from('tekst') },
    },
  });
  expect(uploaded.ok(), await uploaded.text()).toBeTruthy();

  await page.goto('/');
  await page.keyboard.press('Control+k');

  const palette = page.getByRole('dialog');
  await palette.getByRole('textbox').fill(label);
  await expect(palette.getByText('Dokument za pretragu')).toBeVisible({ timeout: 15_000 });
  await palette.getByText('Dokument za pretragu').click();

  await expect(page).toHaveURL(/\/documents\/expiring\?highlight=/);
});

test('the Upload a document action opens the upload on the documents page', async ({ signedIn: page }) => {
  await page.goto('/');
  await page.keyboard.press('Control+k');

  const palette = page.getByRole('dialog');
  await palette.getByRole('textbox').fill('upload a document');
  await palette.getByText('Upload a document').first().click();

  await expect(page).toHaveURL(/\/documents\/expiring/);
  await expect(page.getByRole('dialog').getByRole('button', { name: /^Upload$/ })).toBeVisible();
});

test('the guides page lists the topics and is searchable', async ({ signedIn: page }) => {
  await page.goto('/help');
  await expect(page.getByRole('heading', { name: 'Guides' })).toBeVisible();
  await expect(page.getByText('Adding documents')).toBeVisible();
  await expect(page.getByText('The book of money: how money flows')).toBeVisible();

  await page.getByPlaceholder('Search the guides…').fill('passport');
  await expect(page.getByText('Requests for documents')).toBeVisible();
  await expect(page.getByText('Adding documents')).toBeHidden();

  await page.getByPlaceholder('Search the guides…').fill('zzzzqq');
  await expect(page.getByText('No guide matches the search.')).toBeVisible();
});
