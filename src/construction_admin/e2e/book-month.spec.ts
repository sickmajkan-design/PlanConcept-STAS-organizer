import { expect, test, unique } from './fixtures';

/**
 * A whole month of the book, from the first line to the accountant: a cost typed in, an invoice paid, advances
 * on the payouts screen, the settlement checked and the month closed, a closed month refusing a new line, the
 * package for the accountant, and the month opened again with a reason.
 *
 * Run on a month back from today that has no sheet yet, so that locking it cannot get in the way of the other specs, which add
 * lines to the current month. Numbers are compared as differences, because the database is shared.
 */
const API = `http://localhost:${process.env.E2E_API_PORT ?? 5199}`;

const now = new Date();

/** The first month back from today that has no sheet yet, so that a run is not confused by an earlier one. */
async function freeMonth(list: { year: number; month: number }[]) {
  for (let back = 1; back < 60; back += 1) {
    const d = new Date(now.getFullYear(), now.getMonth() - back, 1);

    if (!list.some((l) => l.year === d.getFullYear() && l.month === d.getMonth() + 1)) {
      return { back, year: d.getFullYear(), month: d.getMonth() + 1, label: new Intl.DateTimeFormat('en', { month: 'long', year: 'numeric' }).format(d) };
    }
  }

  throw new Error('No free month in five years.');
}

test('a month of the book from the first line to the accountant', async ({ signedIn: page, request }) => {
  test.setTimeout(420_000);

  const login = await request.post(`${API}/api/v1/auth/login`, {
    data: { email: 'e2e@construction.local', password: 'E2ePassword123!' },
  });
  const headers = { Authorization: `Bearer ${(await login.json()).accessToken as string}` };

  const post = async (url: string, data: unknown) => {
    const response = await request.post(`${API}${url}`, { data, headers });
    expect(response.ok(), `${url}: ${await response.text()}`).toBeTruthy();

    return response.json();
  };

  const existing = await request.get(`${API}/api/v1/ledgers`, { headers, params: { pageNumber: 1, pageSize: 100 } });
  const existingBody = await existing.json();
  expect(existing.ok(), JSON.stringify(existingBody)).toBeTruthy();
  const target = await freeMonth(existingBody.items ?? []);
  const YEAR = target.year;
  const MONTH = target.month;
  const MONTH_LABEL = target.label;
  const iso = (day: number) => `${YEAR}-${String(MONTH).padStart(2, '0')}-${String(day).padStart(2, '0')}`;

  /** The month picker steps back one at a time. */
  const goBack = async () => {
    for (let i = 0; i < target.back; i += 1) await page.getByRole('button', { name: 'Earlier' }).first().click();
    await expect(page.getByText(MONTH_LABEL).first()).toBeVisible();
  };

  const stamp = unique('mj');
  const projectName = `Hala ${stamp}`;
  const invoiceNumber = `E2E-${stamp}`;

  // ---- the month's ground: a site, an invoice, a payroll sheet with hours, an advance ----
  const project = await post('/api/v1/projects', { name: projectName, status: 'Active', billingMode: 'Hourly', clientHourlyRate: 33 });
  await post('/api/v1/invoices', { projectId: project.id, number: invoiceNumber, issueDate: iso(2), dueDate: iso(20), amount: 4200 });

  const ledger = await post('/api/v1/ledgers', { name: `Obracun ${stamp}`, year: YEAR, month: MONTH, template: 'Payroll' });
  const column = (key: string) => ledger.columns.find((c: { systemKey: string }) => c.systemKey === key).id as string;
  const section = await post(`/api/v1/ledgers/${ledger.id}/sections`, { name: projectName, projectId: project.id });
  const row = await post(`/api/v1/ledgers/sections/${section.id}/rows`, { label: 'Marko Horvat' });

  for (const [key, value] of [['week1', '40'], ['week2', '40'], ['week3', '38'], ['week4', '40'], ['workerRate', '19'], ['clientRate', '33']]) {
    const response = await request.put(`${API}/api/v1/ledgers/cells`, {
      data: { rowId: row.id, columnId: column(key!), value },
      headers,
    });
    expect(response.ok(), await response.text()).toBeTruthy();
  }

  await post('/api/v1/payouts', { year: YEAR, month: MONTH, group: 'Workers', kind: 'Advance', payDate: iso(10), amount: 1500, method: 'Cash' });

  let locked = false;

  try {
    // ---- 1. a cost typed into the quick entry of that month ----
    await page.goto('/book/entries');
    await expect(page.getByPlaceholder('e.g. Fuel, week 41')).toBeVisible();
    await goBack();

    const cost = `Gorivo ${stamp}`;
    await page.getByPlaceholder('e.g. Fuel, week 41').fill(cost);
    // The quick entry dates the line today unless told otherwise; this one belongs to the month being closed.
    await page.getByLabel('Date', { exact: true }).fill(iso(5));
    await page.getByLabel('Amount (€)').fill('1.234,50');
    await page.getByRole('button', { name: 'Add', exact: true }).click();

    await expect(page.getByText(/The line was added/)).toBeVisible();
    await expect(page.getByRole('row').filter({ hasText: cost })).toBeVisible();

    // ---- 2. the invoice is paid from "who owes us" ----
    await page.goto('/book/receivables');
    const invoiceRow = page.getByRole('row').filter({ hasText: invoiceNumber });
    await expect(invoiceRow).toBeVisible();
    await invoiceRow.getByRole('button', { name: 'Mark paid' }).click();

    const payment = page.getByRole('dialog');
    await expect(payment.getByText(/Payment against/)).toBeVisible();
    await payment.getByRole('button', { name: 'Save' }).click();
    await expect(payment).toBeHidden();
    await expect(page.getByRole('row').filter({ hasText: invoiceNumber })).toBeHidden();

    // ---- 3. the advance is on the payouts of that month ----
    await page.goto('/book/payouts');
    await goBack();
    await expect(page.getByText(/1,500/).first()).toBeVisible();

    // ---- 4. the settlement: checks dealt with, confirmations ticked, month closed ----
    await page.goto('/book/settlement');
    await goBack();
    // The sheet of the month with a site on it. (Earlier runs leave sheets of this month behind, so the name of
    // this run's site is not what to look for.)
    await expect(page.getByText('By site (in €)')).toBeVisible();
    await expect(page.getByRole('row').filter({ hasText: 'Hourly' }).first()).toBeVisible();

    // Every item to check is marked as looked at, and the three confirmations are ticked. (The last box, "all
    // items dealt with", is disabled: it ticks itself once the list is empty.)
    const boxes = page.getByRole('checkbox');

    for (let pass = 0; pass < 2; pass += 1) {
      for (let i = 0; i < (await boxes.count()); i += 1) {
        const box = boxes.nth(i);

        if ((await box.isEnabled()) && !(await box.isChecked())) await box.check();
      }
    }

    await expect(page.getByText('Everything is ticked: the month can be closed.')).toBeVisible();
    await page.getByRole('button', { name: 'Close the month' }).click();
    await page.getByRole('dialog').getByRole('button', { name: 'Close the month' }).click();
    locked = true;

    await expect(page.getByText(/This month is closed/)).toBeVisible();
    await expect(page.getByRole('button', { name: 'Reopen the month' })).toBeVisible();

    // ---- 5. a closed month keeps its figures: a line added afterwards is in the book, not in the closed sheet ----
    // The table of the sheet is what closing freezes; the lists under it (costs, warnings) stay live.
    const sheet = () => page.getByText('By site (in €)').locator('xpath=ancestor::div[contains(@class,"MuiPaper-root")][1]');
    const closedFigures = await sheet().innerText();

    await page.goto('/book/entries');
    await goBack();
    const late = `Kasno ${stamp}`;
    await page.getByPlaceholder('e.g. Fuel, week 41').fill(late);
    await page.getByLabel('Date', { exact: true }).fill(iso(6));
    await page.getByLabel('Amount (€)').fill('777');
    await page.getByRole('button', { name: 'Add', exact: true }).click();
    await expect(page.getByText(/The line was added/)).toBeVisible();
    await expect(page.getByRole('row').filter({ hasText: late })).toBeVisible();

    await page.goto('/book/settlement');
    await goBack();
    await expect(page.getByText(/This month is closed/)).toBeVisible();
    expect(await sheet().innerText()).toBe(closedFigures);

    // ---- 6. the package for the accountant: a ZIP with the table, for exactly this month ----
    const archive = await request.get(`${API}/api/v1/book/entries/package`, {
      headers,
      params: { from: iso(1), to: iso(28) },
    });
    expect(archive.ok(), await archive.text()).toBeTruthy();
    const bytes = await archive.body();
    expect(bytes.subarray(0, 2).toString('latin1')).toBe('PK');
    expect(bytes.length).toBeGreaterThan(200);

    // ---- 7. opened again, with a reason that is kept ----
    await page.goto('/book/settlement');
    await goBack();
    await page.getByRole('button', { name: 'Reopen the month' }).click();

    const reopen = page.getByRole('dialog');
    await reopen.getByLabel('Reason').fill('Ispravka sati prije slanja knjigovođi');
    await reopen.getByRole('button', { name: 'Reopen' }).click();
    await expect(reopen).toBeHidden();
    locked = false;

    await expect(page.getByText(/This month is closed/)).toBeHidden();
    await expect(page.getByRole('button', { name: 'Close the month' })).toBeVisible();
  } finally {
    // A run that failed half-way must not leave the month closed for the next one.
    if (locked) {
      await request.post(`${API}/api/v1/ledgers/${ledger.id}/unlock`, { headers, data: { reason: 'Test cleanup' } });
    }
  }
});
