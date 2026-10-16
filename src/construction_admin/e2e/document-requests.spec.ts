import type { APIRequestContext } from '@playwright/test';

import { expect, OPERATOR, test, unique } from './fixtures';

const API = `http://localhost:${process.env.E2E_API_PORT ?? 5199}`;

async function token(request: APIRequestContext, email: string, password: string): Promise<string> {
  const login = await request.post(`${API}/api/v1/auth/login`, { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();

  return (await login.json()).accessToken as string;
}

/** A person with a phone account, as the office would have set up. */
async function seedWorker(request: APIRequestContext) {
  const admin = await token(request, OPERATOR.email, OPERATOR.password);
  const auth = { Authorization: `Bearer ${admin}` };
  const tag = unique('REQ').replace('-', '');

  const employee = await request.post(`${API}/api/v1/employees`, {
    headers: auth,
    data: {
      employeeNumber: tag,
      position: 'Vozac',
      firstName: 'Zahtjev',
      lastName: tag,
      employmentDate: '2024-01-15',
    },
  });
  expect(employee.ok(), await employee.text()).toBeTruthy();
  const created = await employee.json();

  const email = `${tag.toLowerCase()}@construction.local`;
  const user = await request.post(`${API}/api/v1/users`, {
    headers: auth,
    data: { email, password: 'WorkerPassword123!', role: 'Worker', employeeId: created.id },
  });
  expect(user.ok(), await user.text()).toBeTruthy();

  return { employeeId: created.id as string, fullName: `Zahtjev ${tag}`, email, admin };
}

async function sendAsWorker(request: APIRequestContext, email: string, requestId: string) {
  const worker = await token(request, email, 'WorkerPassword123!');

  const sent = await request.post(`${API}/api/v1/documentrequests/${requestId}/submit`, {
    headers: { Authorization: `Bearer ${worker}` },
    multipart: {
      files: {
        name: 'dozvola.pdf',
        mimeType: 'application/pdf',
        buffer: Buffer.from('%PDF-1.4\n1 0 obj<</Type/Catalog>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF'),
      },
    },
  });
  expect(sent.ok(), await sent.text()).toBeTruthy();
}

async function myRequests(request: APIRequestContext, email: string) {
  const worker = await token(request, email, 'WorkerPassword123!');
  const list = await request.get(`${API}/api/v1/documentrequests/mine`, {
    headers: { Authorization: `Bearer ${worker}` },
  });

  return (await list.json()) as { id: string; title: string; status: string; returnReason: string | null }[];
}

test('the office asks for a document, checks the answer, sends it back with a reason, then accepts', async ({
  signedIn: page,
  request,
}) => {
  // Five screens and two phone answers in one flow: more than the suite's default minute.
  test.setTimeout(180_000);

  const worker = await seedWorker(request);

  // 1. Ask: from the person's own page, which carries them into the dialog.
  await page.goto(`/documents/requests?new=1&employeeId=${worker.employeeId}`);

  // The page is a lazily loaded chunk: the first visit pays for the dev server compiling it.
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible({ timeout: 60_000 });
  await expect(dialog.getByText(worker.fullName)).toBeVisible();

  await dialog.getByLabel('Driving licence').check();
  await dialog.getByRole('button', { name: 'Send request' }).click();

  await expect(dialog.getByText(/1 requests? sent/)).toBeVisible();
  await dialog.getByRole('button', { name: 'Close' }).click();

  // 2. The person answers from the phone.
  const [asked] = await myRequests(request, worker.email);
  expect(asked.status).toBe('Open');
  await sendAsWorker(request, worker.email, asked.id);

  // 3. It waits to be checked; the list agrees without a manual reload.
  await page.goto('/documents/requests');
  const row = page.locator('.MuiDataGrid-row', { hasText: worker.fullName });
  await expect(row).toBeVisible();
  await expect(row.getByText('To check')).toBeVisible();

  await row.getByRole('button', { name: 'Check' }).click();

  const review = page.getByRole('dialog');
  await expect(review.getByText('dozvola.pdf')).toBeVisible();

  // A licence lapses, so it cannot be accepted without the date it is valid until.
  await expect(review.getByRole('button', { name: 'Accept', exact: true })).toBeDisabled();

  // 4. Send it back: a reason is required, and the person is told what to fix.
  await review.getByRole('button', { name: 'Send back…' }).click();
  const sendBack = review.getByRole('button', { name: 'Send back', exact: true });
  await expect(sendBack).toBeDisabled();

  await review.getByRole('textbox').fill('The photo is blurred, please take it again');
  await sendBack.click();

  await expect(review.getByText(/Sent back with the reason/)).toBeVisible();
  await review.getByRole('button', { name: 'Close' }).click();

  const afterReturn = await myRequests(request, worker.email);
  expect(afterReturn[0].status).toBe('Returned');
  expect(afterReturn[0].returnReason).toBe('The photo is blurred, please take it again');

  // 5. Sent again, accepted with its date.
  await sendAsWorker(request, worker.email, asked.id);

  await page.goto('/documents/requests');
  await page.locator('.MuiDataGrid-row', { hasText: worker.fullName }).getByRole('button', { name: 'Check' }).click();

  const second = page.getByRole('dialog');
  await expect(second.getByText('Try 2')).toBeVisible();

  await second.getByLabel('Valid until').fill('2031-06-30');
  await second.getByRole('button', { name: 'Accept', exact: true }).click();

  await expect(second.getByText(/Accepted\. The document is on the record/)).toBeVisible();

  // It is now on the person's record and shared with them.
  const worker2 = await token(request, worker.email, 'WorkerPassword123!');
  const mine = await request.get(`${API}/api/v1/attachments/mine`, {
    headers: { Authorization: `Bearer ${worker2}` },
  });
  const documents = (await mine.json()).documents as { fileName: string; expiresAt: string | null }[];

  expect(documents.map((d) => d.fileName)).toContain('dozvola.pdf');
  expect(documents.find((d) => d.fileName === 'dozvola.pdf')?.expiresAt).toBe('2031-06-30');
});

test('an employee page shows what is shared with the person and lets the office change it', async ({
  signedIn: page,
  request,
}) => {
  const worker = await seedWorker(request);

  const upload = await request.post(`${API}/api/v1/attachments`, {
    headers: { Authorization: `Bearer ${worker.admin}` },
    multipart: {
      ownerType: 'Employee',
      ownerId: worker.employeeId,
      category: 'Contract',
      title: 'Ugovor o radu',
      file: {
        name: 'ugovor.pdf',
        mimeType: 'application/pdf',
        buffer: Buffer.from('%PDF-1.4\n1 0 obj<</Type/Catalog>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF'),
      },
    },
  });
  expect(upload.ok(), await upload.text()).toBeTruthy();

  await page.goto(`/employees/${worker.employeeId}`);

  const documents = page.getByTestId('attachment-drop-target');
  await expect(documents.getByText('Ugovor o radu')).toBeVisible();
  await expect(documents.getByText('Office only')).toBeVisible();

  // Nothing is visible to the person until it is shared.
  const person = await token(request, worker.email, 'WorkerPassword123!');
  const before = await request.get(`${API}/api/v1/attachments/mine`, { headers: { Authorization: `Bearer ${person}` } });
  expect((await before.json()).documents).toHaveLength(0);

  await documents.getByRole('button', { name: 'Share with the person' }).click();
  await expect(documents.getByText('Visible to the person')).toBeVisible();

  const after = await request.get(`${API}/api/v1/attachments/mine`, { headers: { Authorization: `Bearer ${person}` } });
  expect((await after.json()).documents).toHaveLength(1);
});

test('handing a vehicle to someone with no driving licence on record warns, and still assigns it', async ({
  signedIn: page,
  request,
}) => {
  test.setTimeout(150_000);

  const admin = await token(request, OPERATOR.email, OPERATOR.password);
  const auth = { Authorization: `Bearer ${admin}` };
  const tag = unique('LIC').replace('-', '');

  // Sorts first by surname, so the vehicle's employee picker (the first page of people) lists them.
  const employee = await request.post(`${API}/api/v1/employees`, {
    headers: auth,
    data: {
      employeeNumber: tag,
      position: 'Vozac',
      firstName: 'Vozac',
      lastName: `Aaa${tag}`,
      employmentDate: '2024-01-15',
    },
  });
  expect(employee.ok(), await employee.text()).toBeTruthy();
  const person = await employee.json();

  const vehicle = await request.post(`${API}/api/v1/vehicles`, {
    headers: auth,
    data: {
      brand: 'Ford',
      model: 'Transit',
      registrationNumber: `E2E${tag}`.slice(0, 12).toUpperCase(),
      tdNumber: `TD${tag}`.slice(0, 14).toUpperCase(),
      fuelType: 'Diesel',
      status: 'Available',
      ownershipType: 'Owned',
    },
  });
  expect(vehicle.ok(), await vehicle.text()).toBeTruthy();
  const created = await vehicle.json();

  await page.goto(`/vehicles/${created.id}`);

  await page.getByRole('combobox').first().click({ timeout: 60_000 });
  await page.getByRole('option', { name: `Vozac Aaa${tag}` }).click();
  await page.getByRole('button', { name: 'Assign', exact: true }).click();

  await expect(page.getByText(/no driving licence on record/i)).toBeVisible();

  // Assigned all the same.
  const read = await request.get(`${API}/api/v1/vehicles/${created.id}`, { headers: auth });
  expect((await read.json()).assignedEmployeeId).toBe(person.id);
});

test('a withdrawn request is deleted for good from the list, after a confirmation', async ({ signedIn: page, request }) => {
  test.setTimeout(150_000);

  const worker = await seedWorker(request);
  const auth = { Authorization: `Bearer ${worker.admin}` };

  const created = await request.post(`${API}/api/v1/documentrequests`, {
    headers: auth,
    data: { employeeIds: [worker.employeeId], items: [{ category: 'Diploma', title: 'Diploma za brisanje' }] },
  });
  expect(created.ok(), await created.text()).toBeTruthy();
  const id = (await created.json()).created[0].id as string;

  const cancelled = await request.post(`${API}/api/v1/documentrequests/${id}/cancel`, { headers: auth });
  expect(cancelled.ok(), await cancelled.text()).toBeTruthy();

  await page.goto(`/documents/requests?employeeId=${worker.employeeId}`);
  await page.getByRole('tab', { name: 'Withdrawn' }).click({ timeout: 60_000 });

  const row = page.locator('.MuiDataGrid-row', { hasText: 'Diploma za brisanje' });
  await expect(row).toBeVisible();
  await row.getByRole('button', { name: 'Open' }).click();

  await page.getByRole('dialog').getByRole('button', { name: 'Delete request' }).click();

  // Asked first; nothing is gone yet.
  const confirm = page.getByRole('dialog').last();
  await expect(confirm.getByText('Delete this request?')).toBeVisible();
  await confirm.getByRole('button', { name: 'Delete', exact: true }).click();

  await expect(page.locator('.MuiDataGrid-row', { hasText: 'Diploma za brisanje' })).toHaveCount(0);

  const gone = await request.get(`${API}/api/v1/documentrequests/${id}`, { headers: auth });
  expect(gone.status()).toBe(404);
});
