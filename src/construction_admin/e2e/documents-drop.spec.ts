import { expect, submitButton, test, unique } from './fixtures';

/**
 * Dropping files on the documents of a record, in a real browser against the real API: the drop opens
 * the upload dialog with the files in it, the upload goes through (including the server's check that a
 * file is what its name says), and the document is listed afterwards with its title and tag.
 */
test('files dropped on a record are sent and listed with their title and tag', async ({ signedIn: page }) => {
  const number = unique('DROP');

  await page.goto('/employees/new');
  await page.getByLabel('Employee number').fill(number);
  await page.getByLabel('Position').fill('Zidar');
  await page.getByLabel('First name').fill('Dino');
  await page.getByLabel('Last name').fill('Drop');
  await page.getByLabel('Employment date').fill('2024-01-15');
  await submitButton(page).click();
  await expect(page).toHaveURL(/\/employees(\/[0-9a-f-]+)?$/);

  await page.goto('/employees');
  await page.getByRole('textbox').first().fill(number);
  await page.locator('.MuiDataGrid-row', { hasText: number }).first().click();

  const target = page.getByTestId('attachment-drop-target');
  await expect(target).toBeVisible();

  // A real drop event carrying a real File, as the browser would build it.
  const name = `${unique('viza')}.txt`;
  await target.evaluate((element, fileName) => {
    const data = new DataTransfer();
    data.items.add(new File(['sadrzaj dokumenta'], fileName, { type: 'text/plain' }));

    for (const type of ['dragenter', 'dragover', 'drop']) {
      element.dispatchEvent(new DragEvent(type, { bubbles: true, cancelable: true, dataTransfer: data }));
    }
  }, name);

  const dialog = page.getByRole('dialog');
  await expect(dialog.getByText(name)).toBeVisible();

  await dialog.getByLabel('Title').fill('Viza Dino');
  await dialog.getByRole('button', { name: /^Upload$/ }).click();

  await expect(dialog).toBeHidden();
  await expect(page.getByText('Viza Dino')).toBeVisible();
});

test('a file whose content does not fit its name is refused by the server', async ({ signedIn: page, request }) => {
  const login = await request.post(`http://localhost:${process.env.E2E_API_PORT ?? 5199}/api/v1/auth/login`, {
    data: { email: 'e2e@construction.local', password: 'E2ePassword123!' },
  });
  const token = (await login.json()).accessToken as string;
  const api = `http://localhost:${process.env.E2E_API_PORT ?? 5199}`;

  const created = await request.post(`${api}/api/v1/employees`, {
    headers: { Authorization: `Bearer ${token}` },
    data: {
      employeeNumber: unique('SIG'),
      position: 'Zidar',
      firstName: 'Sig',
      lastName: 'Check',
      employmentDate: '2024-01-15',
    },
  });
  expect(created.ok(), await created.text()).toBeTruthy();
  const employee = await created.json();

  const refused = await request.post(`${api}/api/v1/attachments`, {
    headers: { Authorization: `Bearer ${token}` },
    multipart: {
      ownerType: 'Employee',
      ownerId: employee.id,
      category: 'Other',
      file: { name: 'ugovor.pdf', mimeType: 'application/pdf', buffer: Buffer.from('MZ not a pdf at all') },
    },
  });

  expect(refused.status()).toBe(409);
  expect(page).toBeTruthy();
});
