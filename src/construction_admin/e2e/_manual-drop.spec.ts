import { expect, test, type Page } from '@playwright/test';

/**
 * Temporary, not part of the suite: walks every drag-and-drop screen against the running local servers
 * (API 5199, web 5173, account probaj@construction.local) and takes a picture of each state.
 */
const API = 'http://localhost:5199';
const OUT = 'e2e/_shots';
const CREDS = { email: 'probaj@construction.local', password: 'ProbajPassword123!' };

async function drop(page: Page, testId: string, files: { name: string; type: string; text?: string; size?: number; binary?: number[] }[]) {
  await page.getByTestId(testId).first().evaluate((element, specs) => {
    const data = new DataTransfer();
    for (const s of specs) {
      const body = s.binary
        ? new Uint8Array(s.binary)
        : new Uint8Array(s.size ?? (s.text ?? 'x').length).fill(120);
      const bytes = s.text !== undefined && !s.size ? new TextEncoder().encode(s.text) : body;
      data.items.add(new File([bytes], s.name, { type: s.type, lastModified: 1 }));
    }
    for (const type of ['dragenter', 'dragover', 'drop']) {
      element.dispatchEvent(new DragEvent(type, { bubbles: true, cancelable: true, dataTransfer: data }));
    }
  }, files);
}

const PDF = { name: 'viza.pdf', type: 'application/pdf', text: '%PDF-1.4\nsadrzaj viza' };
const PDF2 = { name: 'ugovor.pdf', type: 'application/pdf', text: '%PDF-1.4\nsadrzaj ugovor' };
const PNG = { name: 'slika.png', type: 'image/png', binary: [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0, 0, 0, 0] };

test.describe.configure({ mode: 'serial' });

let token = '';
let employeeId = '';

test.beforeAll(async ({ request }) => {
  const login = await request.post(`${API}/api/v1/auth/login`, { data: CREDS });
  token = (await login.json()).accessToken as string;

  const created = await request.post(`${API}/api/v1/employees`, {
    headers: { Authorization: `Bearer ${token}` },
    data: {
      employeeNumber: `TRY-${Date.now() % 100000}`,
      position: 'Zidar',
      firstName: 'Proba',
      lastName: 'Povlacenja',
      employmentDate: '2024-01-15',
    },
  });
  expect(created.ok(), await created.text()).toBeTruthy();
  employeeId = (await created.json()).id;
});

async function signIn(page: Page) {
  await page.addInitScript(() => window.localStorage.setItem('construction.locale', 'sr'));
  await page.goto('/login');
  await page.locator('input[type="email"]').fill(CREDS.email);
  await page.locator('input[type="password"]').fill(CREDS.password);
  await page.locator('form button[type="submit"]').click();
  await expect(page.locator('.MuiAppBar-root')).toBeVisible();

  const dialog = page.getByRole('dialog');
  try {
    await dialog.waitFor({ state: 'visible', timeout: 3000 });
    await dialog.getByRole('button').last().click();
    await expect(dialog).toBeHidden();
  } catch {
    /* no release notes */
  }
}

test('1. two files dropped on the record: dialog, queue, details, upload, list', async ({ page }) => {
  await signIn(page);
  await page.goto(`/employees/${employeeId}`);
  await expect(page.getByTestId('attachment-drop-target')).toBeVisible();

  await drop(page, 'attachment-drop-target', [PDF, PNG]);

  const dialog = page.getByRole('dialog');
  await expect(dialog.getByText('viza.pdf')).toBeVisible();
  await expect(dialog.getByText('slika.png')).toBeVisible();
  await page.screenshot({ path: `${OUT}/try-1-dialog.png` });

  // With several files a title is not offered.
  await expect(dialog.getByLabel('Naslov')).toBeDisabled();

  // The dates section is folded until opened.
  await expect(dialog.getByLabel('Važi do')).toBeHidden();
  await dialog.getByRole('button', { name: /Datumi i podsjetnici/ }).click();
  await expect(dialog.getByLabel('Važi do')).toBeVisible();
  await page.screenshot({ path: `${OUT}/try-1-dates.png` });

  const tags = dialog.getByLabel('Oznake');
  await tags.fill('viza');
  await tags.press('Enter');
  await tags.fill('2026');
  await tags.press('Enter');

  await dialog.getByRole('button', { name: 'Otpremi' }).click();
  await expect(dialog).toBeHidden({ timeout: 20_000 });

  await expect(page.getByText('viza.pdf')).toBeVisible();
  await expect(page.getByText('slika.png')).toBeVisible();
  await expect(page.getByText('#viza').first()).toBeVisible();
  await page.screenshot({ path: `${OUT}/try-1-list.png` });
});

test('2. one file with a title shows the title in the list', async ({ page }) => {
  await signIn(page);
  await page.goto(`/employees/${employeeId}`);
  await drop(page, 'attachment-drop-target', [{ ...PDF2, name: 'ugovor-o-radu.pdf' }]);

  const dialog = page.getByRole('dialog');
  const title = dialog.getByLabel('Naslov');
  await expect(title).toBeEnabled();
  await title.fill('Ugovor Proba');
  await dialog.getByRole('button', { name: 'Otpremi' }).click();
  await expect(dialog).toBeHidden({ timeout: 20_000 });

  await expect(page.getByText('Ugovor Proba')).toBeVisible();
  // The file name stays as the second line.
  await expect(page.getByText(/ugovor-o-radu\.pdf/)).toBeVisible();
});

test('3. rejected files say why and the good ones still go in', async ({ page }) => {
  await signIn(page);
  await page.goto(`/employees/${employeeId}`);

  await drop(page, 'attachment-drop-target', [
    { name: 'program.exe', type: 'application/x-msdownload', text: 'MZ' },
    { name: 'velika.pdf', type: 'application/pdf', size: 26 * 1024 * 1024 },
    { name: 'prazna.pdf', type: 'application/pdf', size: 0, text: '' },
    { ...PDF, name: 'dobra.pdf' },
  ]);

  const dialog = page.getByRole('dialog');
  await expect(dialog.getByText('dobra.pdf')).toBeVisible();
  await expect(dialog.getByText(/program\.exe.*nije dozvoljena/)).toBeVisible();
  await expect(dialog.getByText(/velika\.pdf.*veća od 25 MB/)).toBeVisible();
  await expect(dialog.getByText(/prazna\.pdf.*prazna/)).toBeVisible();
  await page.screenshot({ path: `${OUT}/try-3-rejected.png` });

  await dialog.getByRole('button', { name: 'Otkaži' }).click();
  await expect(dialog).toBeHidden();
});

test('4. a file that is not what its name says is refused by the server and can be retried or removed', async ({ page }) => {
  await signIn(page);
  await page.goto(`/employees/${employeeId}`);

  await drop(page, 'attachment-drop-target', [{ name: 'lazni.pdf', type: 'application/pdf', text: 'MZ ovo nije pdf' }, { ...PDF, name: 'ispravan.pdf' }]);

  const dialog = page.getByRole('dialog');
  await dialog.getByRole('button', { name: 'Otpremi' }).click();

  await expect(dialog.getByText(/Nije poslano/)).toBeVisible({ timeout: 20_000 });
  await page.screenshot({ path: `${OUT}/try-4-failed.png` });
  await expect(dialog.getByRole('button', { name: /Ponovi neposlane \(1\)/ })).toBeVisible();

  // The good one went through, so it is already on the record.
  await dialog.getByRole('button', { name: 'Otkaži' }).click();
  await expect(page.getByText('ispravan.pdf')).toBeVisible();
});

test('5. the zone opens the chooser from the keyboard', async ({ page }) => {
  await signIn(page);
  await page.goto(`/employees/${employeeId}`);
  await page.getByRole('button', { name: 'Dodaj dokument' }).click();

  const zone = page.getByTestId('dropzone');
  await expect(zone).toBeVisible();
  await page.screenshot({ path: `${OUT}/try-5-empty.png` });

  await zone.focus();
  const chooser = page.waitForEvent('filechooser');
  await page.keyboard.press('Enter');
  const picked = await chooser;
  await picked.setFiles({ name: 'sa-tastature.pdf', mimeType: 'application/pdf', buffer: Buffer.from('%PDF-1.4\nx') });

  await expect(page.getByRole('dialog').getByText('sa-tastature.pdf')).toBeVisible();
});

test('6. phone: two buttons instead of the zone', async ({ browser }) => {
  const context = await browser.newContext({ viewport: { width: 390, height: 844 }, hasTouch: true, isMobile: true });
  const page = await context.newPage();
  await signIn(page);
  await page.goto(`/employees/${employeeId}`);
  await page.getByRole('button', { name: 'Dodaj dokument' }).click();

  const dialog = page.getByRole('dialog');
  await expect(dialog.getByRole('button', { name: 'Izaberi datoteke' })).toBeVisible();
  await expect(dialog.getByRole('button', { name: 'Slikaj' })).toBeVisible();
  await expect(page.getByTestId('dropzone')).toHaveCount(0);
  await page.screenshot({ path: `${OUT}/try-6-phone.png` });
  await context.close();
});

test('7. all documents hub: owner first, then the files', async ({ page }) => {
  await signIn(page);
  await page.goto('/documents/expiring');
  await page.getByRole('button', { name: 'Otpremi' }).first().click();

  const dialog = page.getByRole('dialog');
  await expect(dialog.getByLabel('Pripada')).toBeVisible();
  await dialog.getByLabel('Koji/koje').click();
  await page.getByRole('option', { name: /Proba Povlacenja/ }).first().click();
  await drop(page, 'dropzone', [{ ...PDF, name: 'iz-huba.pdf' }]);
  await expect(dialog.getByText('iz-huba.pdf')).toBeVisible();
  await page.screenshot({ path: `${OUT}/try-7-hub.png` });

  await dialog.getByRole('button', { name: 'Otpremi' }).click();
  await expect(dialog).toBeHidden({ timeout: 20_000 });
});

test('8. refund request: single-file zone', async ({ page }) => {
  await signIn(page);
  await page.goto('/refunds');
  await page.getByRole('button', { name: /Zatraži refundaciju/ }).first().click();

  const dialog = page.getByRole('dialog');
  await drop(page, 'dropzone', [{ name: 'racun.png', type: 'image/png', binary: PNG.binary }]);
  await expect(dialog.getByText('racun.png')).toBeVisible();
  await page.screenshot({ path: `${OUT}/try-8-refund.png` });
});

test('9. an import page marks the area and takes the file', async ({ page }) => {
  await signIn(page);
  await page.goto('/stock-movements/import');

  const area = page.getByTestId('drop-area').first();
  await area.evaluate((element) => {
    const data = new DataTransfer();
    data.items.add(new File(['x'], 'isporuke.csv', { type: 'text/csv' }));
    element.dispatchEvent(new DragEvent('dragenter', { bubbles: true, cancelable: true, dataTransfer: data }));
  });
  await expect(area).toHaveAttribute('data-over', 'true');
  await page.screenshot({ path: `${OUT}/try-9-import-over.png` });

  await area.evaluate((element) => {
    const data = new DataTransfer();
    const csv = 'Materijal;Kolicina;Cijena\nCement;10;5\n';
    data.items.add(new File([csv], 'isporuke.csv', { type: 'text/csv' }));
    element.dispatchEvent(new DragEvent('drop', { bubbles: true, cancelable: true, dataTransfer: data }));
  });

  // The page reacts the way it does to a chosen file: either a preview or its own message.
  await expect(area).not.toHaveAttribute('data-over', 'true');
  await page.waitForTimeout(1500);
  await page.screenshot({ path: `${OUT}/try-9-import-after.png` });
});
