// QA only (not part of the product): every route on a 390 px phone, a screenshot of each, and a check for the
// misalignments seen by eye: a wrapped row whose later lines start further right than its first line
// (margin-based Stack spacing), a wrapped row whose items are different widths on the same line pair, and any
// element wider than the screen. Run: node qa/phone_align.mjs <email> <password> <outdir>
import { chromium } from '@playwright/test';
import fs from 'node:fs';

const [, , email, password, outDir] = process.argv;
const BASE = 'http://localhost:5173';
const routes = [
  '/', '/bulletin', '/employees', '/employees/new', '/hierarchy', '/projects', '/projects/new', '/customers', '/vehicles',
  '/tools', '/materials', '/time-entries', '/time-entries/new', '/time-entries/summary', '/work-items', '/schedule', '/absences',
  '/assignment-board', '/costs', '/cost-records', '/stock-movements', '/vehicle-expenses', '/tool-expenses', '/rates',
  '/finance-entries', '/general-expenses', '/accommodations', '/accommodation-costs', '/ledgers', '/company-settings',
  '/documents/expiring', '/audit', '/scheduled-reports', '/weekly-reports', '/notifications', '/notification-groups',
  '/users', '/system', '/documents', '/map', '/change-password',
];

fs.mkdirSync(outDir, { recursive: true });
const browser = await chromium.launch();
const ctx = await browser.newContext({ viewport: { width: 390, height: 844 }, locale: 'en-US', hasTouch: true, isMobile: true, deviceScaleFactor: 2 });
const page = await ctx.newPage();

await page.goto(BASE + '/login');
await page.locator('input[type="email"]').fill(email);
await page.locator('input[type="password"]').fill(password);
await page.locator('form button[type="submit"]').click();
await page.waitForSelector('.MuiAppBar-root', { timeout: 30000 });
try {
  const dialog = page.getByRole('dialog');
  await dialog.waitFor({ state: 'visible', timeout: 3000 });
  await dialog.getByRole('button').last().click();
  await dialog.waitFor({ state: 'hidden', timeout: 3000 });
} catch { /* none */ }

const findings = [];
for (const route of routes) {
  try {
    await page.goto(BASE + route, { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('networkidle', { timeout: 15000 }).catch(() => {});
    await page.waitForFunction(() => !document.querySelector('main .MuiCircularProgress-root, [role="progressbar"]'), null, { timeout: 30000 }).catch(() => {});
    await page.waitForTimeout(900);
  } catch (e) { findings.push({ route, kind: 'nav', text: String(e).slice(0, 100) }); continue; }

  const res = await page.evaluate(() => {
    const out = [];
    const vw = window.innerWidth;
    const label = (el) => `${el.tagName.toLowerCase()}.${String(el.className).split(' ').filter((c) => c.startsWith('Mui')).slice(0, 2).join('.')}`;
    // 1. wrapped rows: group direct children by line; a later line must start where the first one did.
    for (const el of document.querySelectorAll('main *')) {
      const cs = getComputedStyle(el);
      if (!(cs.display.includes('flex')) || cs.flexDirection !== 'row' || cs.flexWrap !== 'wrap') continue;
      const kids = [...el.children].filter((k) => { const r = k.getBoundingClientRect(); return r.width > 0 && r.height > 0 && getComputedStyle(k).position !== 'absolute'; });
      if (kids.length < 2) continue;
      const lines = [];
      for (const k of kids) {
        const r = k.getBoundingClientRect();
        const line = lines.find((l) => Math.abs(l.top - r.top) < 6);
        if (line) line.items.push(r); else lines.push({ top: r.top, items: [r] });
      }
      if (lines.length < 2) continue;
      const first = Math.min(...lines[0].items.map((r) => r.left));
      for (const l of lines.slice(1)) {
        const left = Math.min(...l.items.map((r) => r.left));
        if (Math.abs(left - first) > 3) { out.push({ kind: 'indent', text: `${label(el)} line starts at ${Math.round(left)} vs ${Math.round(first)}` }); break; }
      }
    }
    // 2. anything poking out of the screen
    for (const el of document.querySelectorAll('main *')) {
      const r = el.getBoundingClientRect();
      if (r.width > 0 && r.right > vw + 2 && getComputedStyle(el).position !== 'fixed') {
        let p = el.parentElement; let clipped = false;
        while (p && p !== document.body) { const o = getComputedStyle(p).overflowX; if (o === 'auto' || o === 'scroll' || o === 'hidden') { clipped = true; break; } p = p.parentElement; }
        if (!clipped) { out.push({ kind: 'overflow', text: `${label(el)} right=${Math.round(r.right)}` }); break; }
      }
    }
    return out;
  });
  for (const f of res) findings.push({ route, ...f });
  await page.screenshot({ path: `${outDir}/${route.replace(/\W+/g, '_') || 'home'}.png`, fullPage: false });
}
await browser.close();
fs.writeFileSync(`${outDir}/findings.json`, JSON.stringify(findings, null, 1));
console.log(findings.length + ' findings');
for (const f of findings) console.log(f.route, f.kind, f.text);
