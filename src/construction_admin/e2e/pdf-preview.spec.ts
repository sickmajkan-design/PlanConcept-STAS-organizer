import { expect, OPERATOR, test, unique } from './fixtures';

const API = `http://localhost:${process.env.E2E_API_PORT ?? 5199}`;

/** A real two-page PDF, with an xref table, so a viewer that is strict about structure still opens it. */
function twoPagePdf(): Buffer {
  const objects = [
    '<< /Type /Catalog /Pages 2 0 R >>',
    '<< /Type /Pages /Kids [3 0 R 5 0 R] /Count 2 >>',
    '<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 200] /Contents 4 0 R /Resources << /Font << /F1 7 0 R >> >> >>',
    stream('BT /F1 24 Tf 20 100 Td (Prva stranica) Tj ET'),
    '<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 200] /Contents 6 0 R /Resources << /Font << /F1 7 0 R >> >> >>',
    stream('BT /F1 24 Tf 20 100 Td (Druga stranica) Tj ET'),
    '<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>',
  ];

  let body = '%PDF-1.4\n';
  const offsets: number[] = [];

  objects.forEach((object, index) => {
    offsets.push(Buffer.byteLength(body, 'latin1'));
    body += `${index + 1} 0 obj\n${object}\nendobj\n`;
  });

  const xref = Buffer.byteLength(body, 'latin1');
  body += `xref\n0 ${objects.length + 1}\n0000000000 65535 f \n`;
  offsets.forEach((offset) => {
    body += `${String(offset).padStart(10, '0')} 00000 n \n`;
  });
  body += `trailer\n<< /Size ${objects.length + 1} /Root 1 0 R >>\nstartxref\n${xref}\n%%EOF\n`;

  return Buffer.from(body, 'latin1');

  function stream(content: string): string {
    return `<< /Length ${content.length} >>\nstream\n${content}\nendstream`;
  }
}

/**
 * A PDF is drawn page by page by the panel itself, so it reads the same on a phone as on a desktop —
 * not left to whatever viewer the browser happens to have in a frame.
 */
test('a PDF is drawn page by page inside the preview', async ({ signedIn: page, request }) => {
  test.setTimeout(150_000);

  const login = await request.post(`${API}/api/v1/auth/login`, {
    data: { email: OPERATOR.email, password: OPERATOR.password },
  });
  const admin = (await login.json()).accessToken as string;
  const auth = { Authorization: `Bearer ${admin}` };
  const tag = unique('PDF').replace('-', '');

  const employee = await request.post(`${API}/api/v1/employees`, {
    headers: auth,
    data: {
      employeeNumber: tag,
      position: 'Zidar',
      firstName: 'Pdf',
      lastName: tag,
      employmentDate: '2024-01-15',
    },
  });
  expect(employee.ok(), await employee.text()).toBeTruthy();
  const created = await employee.json();

  const upload = await request.post(`${API}/api/v1/attachments`, {
    headers: auth,
    multipart: {
      ownerType: 'Employee',
      ownerId: created.id,
      category: 'Contract',
      title: 'Ugovor u dvije stranice',
      file: { name: 'ugovor.pdf', mimeType: 'application/pdf', buffer: twoPagePdf() },
    },
  });
  expect(upload.ok(), await upload.text()).toBeTruthy();

  await page.goto(`/employees/${created.id}`);

  const row = page.getByTestId('attachment-drop-target').getByText('Ugovor u dvije stranice');
  await expect(row).toBeVisible({ timeout: 60_000 });
  await row.dblclick();

  const dialog = page.getByRole('dialog');
  const pages = dialog.getByTestId('pdf-pages');

  await expect(pages.locator('canvas')).toHaveCount(2, { timeout: 30_000 });

  // The built-in viewer is not what is showing.
  await expect(dialog.locator('iframe')).toHaveCount(0);

  // And something was actually painted: a canvas of the right proportions with non-blank pixels.
  const painted = await pages.locator('canvas').first().evaluate((canvas) => {
    const element = canvas as HTMLCanvasElement;
    const context = element.getContext('2d')!;
    const data = context.getImageData(0, 0, element.width, element.height).data;

    for (let i = 0; i < data.length; i += 4) {
      // A dark pixel: the page is white and the text is black.
      if (data[i] < 80 && data[i + 1] < 80 && data[i + 2] < 80 && data[i + 3] > 0) return true;
    }

    return false;
  });

  expect(painted).toBe(true);
});
