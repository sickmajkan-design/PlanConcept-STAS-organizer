// QA only (not part of the product): every static route of the panel plus a detail page of each kind, as a signed-in user,
// at a phone or a desktop width, one screenshot each, and the same alignment checks as phone_align.mjs.
// Run: node qa/survey_all.mjs <email> <password> <outdir> <phone|desktop>
import { chromium } from '@playwright/test';
import fs from 'node:fs';

const [, , email, password, outDir, mode = 'phone'] = process.argv;
const BASE = 'http://localhost:5173';
const API = 'http://localhost:5000';
const viewport = mode === 'phone' ? { width: 390, height: 844 } : { width: 1366, height: 800 };

const pathsSource = fs.readFileSync(new URL('../src/routes/paths.ts', import.meta.url), 'utf8');
const skip = /login|forgot|reset|invite|twoStep|customerPortal|inviteRoute|changePassword/;
const statics = [...pathsSource.matchAll(/^\s+(\w+): '(\/[^':?]*)',/gm)].filter(([, k]) => !skip.test(k)).map(([, , p]) => p);

const login = await fetch(`${API}/api/auth/login`, { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ email, password }) });
const token = (await login.json()).accessToken;
const get = async (p) => { try { const r = await fetch(API + p, { headers: { authorization: `Bearer ${token}` } }); return r.ok ? await r.json() : null; } catch { return null; } };
const first = (page) => (page?.items ?? page ?? [])[0]?.id;
const dyn = [];
for (const [list, route] of [['employees', '/employees'], ['projects', '/projects'], ['vehicles', '/vehicles'], ['tools', '/tools'], ['materials', '/materials'], ['accommodations', '/accommodations']]) {
  const id = first(await get(`/api/${list}?pageSize=1`));
  if (id) dyn.push(`${route}/${id}`, `${route}/${id}/edit`);
}
const routes = [...new Set([...statics, ...dyn])];

fs.mkdirSync(outDir, { recursive: true });
const browser = await chromium.launch();
const ctx = await browser.newContext({ viewport, locale: 'en-US', ...(mode === 'phone' ? { hasTouch: true, isMobile: true, deviceScaleFactor: 2 } : {}) });
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
const names = [];
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
    const heading = (document.querySelector('main h1, main h2, main h5') || {}).innerText || '';
    if (/something went wrong|unexpected error|došlo je do greške/i.test((document.querySelector('main') || document.body).innerText)) out.push({ kind: 'error-page', text: heading });
    for (const el of document.querySelectorAll('main *')) {
      const r = el.getBoundingClientRect();
      if (r.width > 0 && r.right > vw + 2 && getComputedStyle(el).position !== 'fixed') {
        let p = el.parentElement; let clipped = false;
        while (p && p !== document.body) { const o = getComputedStyle(p).overflowX; if (o === 'auto' || o === 'scroll' || o === 'hidden') { clipped = true; break; } p = p.parentElement; }
        if (!clipped) { out.push({ kind: 'overflow', text: `${el.tagName.toLowerCase()} right=${Math.round(r.right)}` }); break; }
      }
    }
    return out;
  });
  for (const f of res) findings.push({ route, ...f });
  const name = (route.replace(/[0-9a-f]{8}-[0-9a-f-]{27}/g, 'ID').replace(/\W+/g, '_') || 'home').replace(/^_/, '') || 'home';
  names.push(name);
  await page.screenshot({ path: `${outDir}/${name}.png`, fullPage: false });
}
await browser.close();
fs.writeFileSync(`${outDir}/names.json`, JSON.stringify(names));
fs.writeFileSync(`${outDir}/findings.json`, JSON.stringify(findings, null, 1));
console.log(routes.length + ' routes, ' + findings.length + ' findings');
for (const f of findings) console.log(f.route, f.kind, f.text);
