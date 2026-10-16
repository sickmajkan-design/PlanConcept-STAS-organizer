import { expect, test } from '@playwright/test';

import { OPERATOR, setLanguage, signIn } from './fixtures';

/**
 * Not part of the suite: seeds a believable month through the API and takes a picture of every screen of the
 * book, so the layout can be compared with the mockup by eye. Run on its own.
 */
const API = `http://localhost:${process.env.E2E_API_PORT ?? 5199}`;
const OUT = process.env.VISUAL_OUT ?? 'e2e/_shots';

const pages = [
  ['guide', '/book/guide'],
  ['overview', '/book'],
  ['entries', '/book/entries'],
  ['receivables', '/book/receivables'],
  ['payouts', '/book/payouts'],
  ['settlement', '/book/settlement'],
  ['recurring', '/book/recurring'],
  ['categories', '/book/categories'],
  ['import', '/book/import'],
] as const;

test('book screens', async ({ page, request }) => {
  test.setTimeout(240_000);

  const login = await request.post(`${API}/api/v1/auth/login`, {
    data: { email: OPERATOR.email, password: OPERATOR.password },
  });
  const token = (await login.json()).accessToken as string;
  const headers = { Authorization: `Bearer ${token}` };

  const post = async (url: string, data: unknown) => {
    const response = await request.post(`${API}${url}`, { data, headers });
    if (!response.ok()) throw new Error(`${url} ${response.status()} ${await response.text()}`);
    return response.json();
  };

  const put = async (url: string, data: unknown) => {
    const response = await request.put(`${API}${url}`, { data, headers });
    if (!response.ok()) throw new Error(`${url} ${response.status()} ${await response.text()}`);
  };

  const now = new Date();
  const y = now.getFullYear();
  const m = now.getMonth() + 1;
  const iso = (day: number, monthOffset = 0) => {
    const d = new Date(y, m - 1 + monthOffset, day);
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  };
  const day = Math.min(now.getDate(), 27);

  if (!process.env.SKIP_SEED) {
  const hala = await post('/api/v1/projects', { name: 'Hala B', status: 'Active', billingMode: 'Hourly', clientHourlyRate: 33 });
  const zgrada = await post('/api/v1/projects', { name: 'Zgrada D', status: 'Active', billingMode: 'FlatRate', clientHourlyRate: 31 });

  await post('/api/v1/invoices', { projectId: hala.id, number: `2026-${Date.now() % 1000}1`, issueDate: iso(1), dueDate: iso(15), amount: 18400 });
  await post('/api/v1/invoices', { projectId: zgrada.id, number: `2026-${Date.now() % 1000}2`, issueDate: iso(1, -1), dueDate: iso(5, -1), amount: 12500, payrollYear: y, payrollMonth: m });

  const cats = await (await request.get(`${API}/api/v1/book/categories`, { headers })).json();
  const cat = (key: string) => cats.find((c: { systemKey: string }) => c.systemKey === key).id as string;

  const entry = (kind: string, categoryKey: string | null, description: string, amount: number, extra: object = {}) =>
    post('/api/v1/book/entries', {
      kind,
      amount,
      occurredOn: iso(Math.max(1, day - 2)),
      categoryId: categoryKey ? cat(categoryKey) : null,
      description,
      branchId: null,
      projectId: null,
      ...extra,
    });

  await entry('Cost', 'Fuel', 'Gorivo, DKV izvod sedmica 39', 2140, { counterparty: 'DKV', paidOn: iso(1) });
  await entry('Cost', 'Tools', 'Alat i uniforme', 640, { counterparty: 'Dobavljač 2', dueDate: iso(27) });
  await entry('Cost', 'Housing', 'Kirija stana, Ulica 12', 1200, { projectId: hala.id, counterparty: 'Stanodavac', paidOn: iso(1) });
  await entry('Cost', 'FixedCosts', 'Fiksni trošak, Slovenija', 1100, { counterparty: 'Ponavljajući', paidOn: iso(1) });
  await entry('Income', 'Collection', 'Faktura, satnica', 6000, { projectId: hala.id, counterparty: 'Klijent A', dueDate: iso(27) });

  await post('/api/v1/book/recurring', {
    name: 'Računovodstvo', categoryId: cat('FixedCosts'), amount: 500, frequency: 'Monthly',
    startMonth: iso(1), dayOfMonth: 5,
  });

  const ledger = await post('/api/v1/ledgers', { name: `Obračun ${m}/${y}`, year: y, month: m, template: 'Payroll' });
  const column = (key: string) => ledger.columns.find((c: { systemKey: string }) => c.systemKey === key).id as string;

  for (const [project, people] of [
    [hala, [['Marko Horvat', '40', '40', '38', '40', '20', '33'], ['Ivan Kovač', '40', '36', '40', '40', '21', '33']]],
    [zgrada, [['Ana Marić', '40', '40', '40', '8', '19', '31']]],
  ] as const) {
    const section = await post(`/api/v1/ledgers/${ledger.id}/sections`, { name: project.name, projectId: project.id });
    for (const [label, w1, w2, w3, w4, rate, client] of people) {
      const row = await post(`/api/v1/ledgers/sections/${section.id}/rows`, { label });
      const set = (key: string, value: string) =>
        put('/api/v1/ledgers/cells', { rowId: row.id, columnId: column(key), value });
      await set('week1', w1);
      await set('week2', w2);
      await set('week3', w3);
      await set('week4', w4);
      await set('workerRate', rate);
      await set('clientRate', client);
    }
  }

  await post('/api/v1/payouts', { year: y, month: m, group: 'Workers', kind: 'Advance', payDate: iso(Math.max(1, day - 3)), amount: 1500, method: 'Cash' });

  }

  await page.setViewportSize({ width: 1360, height: 900 });
  await setLanguage(page, 'sr');
  await signIn(page);

  for (const [name, path] of pages) {
    await page.goto(path);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(4500);
    await page.screenshot({ path: `${OUT}/${name}.png`, fullPage: true });
  }

  expect(true).toBe(true);
});
