// QA only: puts the four server widgets on the Super Admin's board of the local scratch API and screenshots the home page
// on a desktop and on a phone. Run: node qa/server_widgets_shot.mjs <email> <password> <outdir>
import { chromium } from '@playwright/test';
import fs from 'node:fs';

const [, , email, password, outDir] = process.argv;
fs.mkdirSync(outDir, { recursive: true });
const API = 'http://localhost:5000';
const login = await (await fetch(`${API}/api/auth/login`, { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ email, password }) })).json();
const widgets = ['ServerHealth', 'ServerResources', 'ServerBackups', 'ServerSignIns'].map((type, i) => ({
  id: crypto.randomUUID(), type, x: (i % 2) * 6, y: Math.floor(i / 2) * 10, w: 6, h: 10,
}));
const res = await fetch(`${API}/api/dashboard-layout`, { method: 'PUT', headers: { 'content-type': 'application/json', authorization: `Bearer ${login.accessToken}` }, body: JSON.stringify({ widgets }) });
console.log('save layout', res.status);

const browser = await chromium.launch();
for (const [label, viewport, extra] of [['desktop', { width: 1366, height: 900 }, {}], ['phone', { width: 390, height: 844 }, { hasTouch: true, isMobile: true, deviceScaleFactor: 2 }]]) {
  const ctx = await browser.newContext({ viewport, locale: 'en-US', ...extra });
  const page = await ctx.newPage();
  await page.goto('http://localhost:5173/login');
  await page.locator('input[type="email"]').fill(email);
  await page.locator('input[type="password"]').fill(password);
  await page.locator('form button[type="submit"]').click();
  await page.waitForSelector('.MuiAppBar-root', { timeout: 30000 });
  try {
    const dialog = page.getByRole('dialog');
    await dialog.waitFor({ state: 'visible', timeout: 3000 });
    await dialog.getByRole('button').last().click();
  } catch { /* none */ }
  await page.goto('http://localhost:5173/');
  await page.waitForLoadState('networkidle', { timeout: 15000 }).catch(() => {});
  await page.waitForTimeout(2500);
  await page.screenshot({ path: `${outDir}/${label}.png`, fullPage: label === 'phone' });
  await ctx.close();
}
await browser.close();
