import { chromium } from '@playwright/test';
const b=await chromium.launch({executablePath:'/opt/pw-browsers/chromium-1194/chrome-linux/chrome'});
const c=await b.newContext();
await c.addInitScript((v)=>window.localStorage.setItem('construction.locale',v),'en');
const p=await c.newPage();
await p.goto('http://localhost:5173/login');
try{
  await p.getByLabel('Email').fill('e2e@construction.local');
  console.log('email fill ok');
}catch(e){ console.log('email fill FAILED:', String(e).split('\n')[0]); }
try{
  await p.getByLabel('Password',{exact:true}).fill('E2ePassword123!');
  console.log('password fill ok');
}catch(e){ console.log('password fill FAILED:', String(e).split('\n').slice(0,3).join(' | ')); }
try{
  await p.getByRole('button',{name:'Sign in'}).click();
  console.log('click ok');
}catch(e){ console.log('click FAILED:', String(e).split('\n')[0]); }
await p.waitForTimeout(2500);
console.log('url:', p.url());
const n = await p.getByRole('link',{name:'Employees'}).count();
console.log('Employees links:', n);
await b.close();
