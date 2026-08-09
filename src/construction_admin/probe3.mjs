import { chromium } from '@playwright/test';
const b=await chromium.launch({executablePath:'/opt/pw-browsers/chromium-1194/chrome-linux/chrome'});
const c=await b.newContext();
await c.addInitScript(()=>window.localStorage.setItem('construction.locale','sr'));
const p=await c.newPage();
await p.goto('http://localhost:5173/login'); await p.waitForTimeout(1000);
console.log('SR labels:', await p.evaluate(()=>[...document.querySelectorAll('label')].map(l=>l.textContent)));
console.log('SR buttons:', await p.evaluate(()=>[...document.querySelectorAll('button')].map(x=>(x.getAttribute('aria-label')||x.textContent||'').trim())));
console.log('html lang:', await p.evaluate(()=>document.documentElement.lang));
await p.locator('input[type="email"]').fill('e2e@construction.local');
await p.locator('input[type="password"]').fill('E2ePassword123!');
await p.locator('form button[type="submit"]').click();
await p.waitForTimeout(2500);
console.log('SR nav sample:', await p.evaluate(()=>[...document.querySelectorAll('a')].map(a=>a.textContent.trim()).slice(0,5)));
const vis = await p.getByRole('link',{name:'Radnici'}).filter({visible:true}).count();
console.log('visible Radnici links:', vis);
// employee form labels (EN)
await p.evaluate(()=>window.localStorage.setItem('construction.locale','en'));
await p.goto('http://localhost:5173/employees/new'); await p.waitForTimeout(1500);
console.log('form labels:', await p.evaluate(()=>[...document.querySelectorAll('label')].map(l=>l.textContent)));
console.log('form submit:', await p.evaluate(()=>[...document.querySelectorAll('form button[type=submit]')].map(x=>x.textContent.trim())));
// language menu
await p.getByRole('button',{name:'Language'}).click(); await p.waitForTimeout(600);
console.log('lang menu:', await p.evaluate(()=>[...document.querySelectorAll('[role=menuitem]')].map(x=>x.textContent.trim())));
await b.close();
