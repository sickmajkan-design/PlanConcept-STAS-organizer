import { expect, test, type Page } from '@playwright/test';

/**
 * Temporary: opens the preview of every kind of document through a front door that sets the production
 * Content-Security-Policy, on port 5181 (old policy) or 5182 (new policy). Run with BASE=...
 */
const API = 'http://localhost:5199';
const BASE = process.env.CSP_BASE ?? 'http://localhost:5181';
const CREDS = { email: 'probaj@construction.local', password: 'ProbajPassword123!' };

const kinds: { name: string; type: string; body: () => Buffer | Promise<Buffer> }[] = [
  { name: 'dokument.pdf', type: 'application/pdf', body: () => Buffer.from(minimalPdf()) },
  { name: 'slika.png', type: 'image/png', body: () => Buffer.from(
      'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==', 'base64') },
  { name: 'bilješke.txt', type: 'text/plain', body: () => Buffer.from('Obican tekst za pregled') },
  { name: 'tabela.csv', type: 'text/csv', body: () => Buffer.from('Naziv;Kolicina\nCement;10\nPijesak;5\n') },
  { name: 'tabela.xlsx', type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', body: async () => {
      const XLSX = await import('xlsx');
      const wb = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet([['Naziv', 'Kolicina'], ['Cement', 10]]), 'List1');
      return Buffer.from(XLSX.write(wb, { type: 'buffer', bookType: 'xlsx' }));
    } },
];

function minimalPdf(): string {
  return '%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n' +
    '3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 200 100]/Contents 4 0 R/Resources<</Font<</F1 5 0 R>>>>>>endobj\n' +
    '4 0 obj<</Length 44>>stream\nBT /F1 18 Tf 20 50 Td (Pregled PDF) Tj ET\nendstream endobj\n' +
    '5 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF';
}

async function signIn(page: Page) {
  await page.addInitScript(() => window.localStorage.setItem('construction.locale', 'sr'));
  await page.goto(`${BASE}/login`);
  await page.locator('input[type="email"]').fill(CREDS.email);
  await page.locator('input[type="password"]').fill(CREDS.password);
  await page.locator('form button[type="submit"]').click();
  await expect(page.locator('.MuiAppBar-root')).toBeVisible();
  const dialog = page.getByRole('dialog');
  try {
    await dialog.waitFor({ state: 'visible', timeout: 3000 });
    await dialog.getByRole('button').last().click();
  } catch { /* none */ }
}

test('preview of every document kind under the production policy', async ({ page, request }) => {
  test.setTimeout(240_000);
  const login = await request.post(`${API}/api/v1/auth/login`, { data: CREDS });
  const headers = { Authorization: `Bearer ${(await login.json()).accessToken as string}` };

  const created = await request.post(`${API}/api/v1/employees`, {
    headers,
    data: { employeeNumber: `CSP-${Date.now() % 100000}`, position: 'Zidar', firstName: 'Csp', lastName: 'Pregled', employmentDate: '2024-01-15' },
  });
  const employeeId = (await created.json()).id as string;

  for (const kind of kinds) {
    const res = await request.post(`${API}/api/v1/attachments`, {
      headers,
      multipart: {
        ownerType: 'Employee', ownerId: employeeId, category: 'Other',
        file: { name: kind.name, mimeType: kind.type, buffer: await kind.body() },
      },
    });
    expect(res.ok(), `${kind.name}: ${await res.text()}`).toBeTruthy();
  }

  const violations: string[] = [];
  page.on('console', (m) => {
    if (/Content Security Policy|Refused to/i.test(m.text())) violations.push(m.text().slice(0, 160));
  });

  await signIn(page);
  await page.goto(`${BASE}/employees/${employeeId}`);

  const results: string[] = [];
  for (const kind of kinds) {
    await page.getByText(kind.name, { exact: false }).first().dblclick();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await page.waitForTimeout(1800);

    const iframe = dialog.locator('iframe');
    const img = dialog.locator('img');
    const text = (await dialog.innerText()).replace(/\s+/g, ' ');
    const note = /nije moguć pregled|No preview/i.test(text) ? 'NO PREVIEW MESSAGE' : /ne može|error|greška/i.test(text) ? 'ERROR' : 'shown';
    const detail = (await iframe.count()) ? 'iframe' : (await img.count()) ? 'img' : 'html/text';
    results.push(`${kind.name}: ${note} (${detail})`);
    await page.screenshot({ path: `e2e/_shots/csp-${BASE.endsWith('5181') ? 'old' : 'new'}-${kind.name}.png` });
    await dialog.getByRole('button', { name: /Zatvori|Close/ }).click();
    await expect(dialog).toBeHidden();
  }

  console.log('RESULTS\n' + results.join('\n') + '\nVIOLATIONS\n' + violations.join('\n'));
});
