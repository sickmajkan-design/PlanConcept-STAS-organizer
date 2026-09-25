// QA smoke crawl of the admin panel (not part of the product): every route as a signed-in
// user, recording console errors, failed API calls, empty pages and horizontal overflow
// at desktop and phone widths. Run: node qa_ui_crawl.mjs <email> <password> [outfile]
import { chromium } from '@playwright/test';
import fs from 'node:fs';

const [, , email, password, out = 'qa_ui_crawl.json'] = process.argv;
const BASE = 'http://localhost:5173';
const routes = [
  '/', '/bulletin', '/employees', '/employees/new', '/hierarchy', '/projects', '/projects/new', '/projects/annual-realization',
  '/customers', '/customers/new', '/vehicles', '/vehicles/new', '/tools', '/tools/new', '/materials', '/materials/new',
  '/time-entries', '/time-entries/new', '/time-entries/summary', '/work-items', '/work-items/new', '/schedule', '/absences',
  '/assignment-board', '/costs', '/cost-records', '/billing-settings', '/stock-movements', '/stock-movements/import',
  '/vehicle-expenses', '/vehicle-expenses/fuel-import', '/tool-expenses', '/rates', '/public-holidays', '/finance-entries',
  '/general-expenses', '/company-revenues', '/accommodation-costs', '/accommodations', '/accommodations/new',
  '/accommodations/import', '/ledgers', '/company-settings', '/documents/expiring', '/audit', '/scheduled-reports',
  '/weekly-reports', '/notifications', '/notification-groups', '/notification-groups/new', '/users', '/users/new', '/map',
  '/change-password',
];

const browser = await chromium.launch();
const report = [];

for (const [label, viewport] of [['desktop', { width: 1366, height: 800 }], ['phone', { width: 390, height: 800 }]]) {
  const ctx = await browser.newContext({ viewport, locale: 'en-US' });
  const page = await ctx.newPage();
  let current = '';
  const errors = [];
  page.on('console', (m) => { if (m.type() === 'error') errors.push({ route: current, kind: 'console', text: m.text().slice(0, 240) }); });
  page.on('pageerror', (e) => errors.push({ route: current, kind: 'pageerror', text: String(e).slice(0, 240) }));
  page.on('response', (r) => {
    const s = r.status();
    if (s >= 400 && r.url().includes('localhost:5000')) errors.push({ route: current, kind: `http ${s}`, text: `${r.request().method()} ${r.url().replace('http://localhost:5000', '')}`.slice(0, 200) });
  });

  await page.goto(BASE + '/login');
  await page.locator('input[type="email"]').fill(email);
  await page.locator('input[type="password"]').fill(password);
  await page.locator('form button[type="submit"]').click();
  await page.waitForSelector('.MuiAppBar-root', { timeout: 20000 });
  // Dismiss the "what's new" dialog if it shows.
  try {
    const dialog = page.getByRole('dialog');
    await dialog.waitFor({ state: 'visible', timeout: 3000 });
    await dialog.getByRole('button').last().click();
    await dialog.waitFor({ state: 'hidden', timeout: 3000 });
  } catch { /* none */ }

  for (const route of routes) {
    current = route;
    const before = errors.length;
    let status = 'ok';
    try {
      await page.goto(BASE + route, { waitUntil: 'domcontentloaded' });
      await page.waitForLoadState('networkidle', { timeout: 15000 }).catch(() => {});
      await page.waitForTimeout(600);
    } catch (e) { status = 'nav-error: ' + String(e).slice(0, 100); }
    const info = await page.evaluate(() => {
      const body = document.body;
      const main = document.querySelector('main') ?? body;
      const text = (main.innerText || '').trim();
      const overflow = document.documentElement.scrollWidth - window.innerWidth;
      const h = (document.querySelector('h1,h2,[role="heading"]') || {}).innerText || '';
      return { textLen: text.length, overflow, heading: h.slice(0, 60), url: location.pathname, error: /something went wrong|unexpected error|došlo je do greške/i.test(text) };
    });
    report.push({ viewport: label, route, ...info, status, errors: errors.slice(before) });
    if (label === 'phone' && (info.overflow > 4 || info.error)) {
      await page.screenshot({ path: `qa_shot_${label}_${route.replace(/\W+/g, '_')}.png` });
    }
  }
  await ctx.close();
}
await browser.close();
fs.writeFileSync(out, JSON.stringify(report, null, 1));

const bad = report.filter((r) => r.errors.length || r.status !== 'ok' || r.error || r.textLen < 20 || (r.viewport === 'phone' && r.overflow > 4) || r.url !== r.route);
console.log(`${report.length} page loads, ${bad.length} with findings`);
for (const r of bad) {
  const why = [];
  if (r.status !== 'ok') why.push(r.status);
  if (r.error) why.push('error screen');
  if (r.textLen < 20) why.push('near-empty page');
  if (r.viewport === 'phone' && r.overflow > 4) why.push(`horizontal overflow ${r.overflow}px`);
  if (r.url !== r.route) why.push(`redirected to ${r.url}`);
  console.log(`[${r.viewport}] ${r.route}: ${why.join(', ')}`);
  for (const e of r.errors.slice(0, 4)) console.log(`      ${e.kind}: ${e.text}`);
}
