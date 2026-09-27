"""New-feature flow: leave rules, refunds/absences decision rights after today's
customer-driven access changes, the finance grant's edges, and invoices with
client companies.

Run after qa_setup.py, against a fresh local QA database, same convention as
the other flow scripts.
"""
import datetime as dt
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from qa_lib import *  # noqa: F401,F403

st = fresh_state()
SA = st['super']
AD = st['admin']
PM = st['pm']
FO = st['foreman']
W = st['worker']
W2 = st['worker2']
E = st['emp']
P1 = st['p1']

print('=== LEAVE: who decides absences ===')
d0 = dt.date.today() + dt.timedelta(days=200)
c, ab, _ = call('POST', '/api/absences', W, {
    'employeeId': E['worker'], 'type': 'AnnualLeave',
    'startDate': (d0 + dt.timedelta(days=100)).isoformat(),
    'endDate': (d0 + dt.timedelta(days=101)).isoformat(),
})
expect('LV-01', 'worker requests leave', c, 201)
aid = ab.get('id') if isinstance(ab, dict) else None

if aid:
    c, js, _ = call('POST', f'/api/absences/{aid}/review', FO, {'approve': True})
    expect('LV-02', 'a foreman may NOT decide leave (customer rule: Admin/SuperAdmin only)', c, [403])
    c, js, _ = call('POST', f'/api/absences/{aid}/review', PM, {'approve': True})
    expect('LV-03', 'a project manager may NOT decide leave either', c, [403])
    c, js, _ = call('POST', f'/api/absences/{aid}/review', AD, {'approve': True})
    expect('LV-04', 'admin may decide leave', c, [200, 204])

print('\n=== LEAVE: manual corrections ===')
c, js, _ = call('POST', '/api/absences/adjustments', FO, {
    'employeeId': E['worker'], 'year': 2026, 'days': 3, 'reason': 'QA proba',
})
expect('LV-05', 'a foreman may NOT write a leave correction', c, [403])

c, js, _ = call('POST', '/api/absences/adjustments', AD, {
    'employeeId': E['worker'], 'year': 2026, 'days': 3, 'reason': 'Prenos iz stare evidencije',
})
expect('LV-06', 'admin writes a leave correction', c, [200, 201], f'GOT {c} {str(js)[:150]}')

c, js, _ = call('POST', '/api/absences/adjustments', AD, {
    'employeeId': E['worker'], 'year': 2026, 'days': 0, 'reason': 'Nista',
})
expect('LV-07', 'a correction of zero days is refused', c, [400])

c, js, _ = call('GET', f'/api/absences/balance?employeeId={E["worker"]}&year=2026', W)
expect('LV-08', 'worker reads own balance with the new breakdown', c, 200)
if c == 200 and isinstance(js, dict):
    record('LV-09', 'balance carries entitlement/carriedOver/adjustment fields',
           all(k in js for k in ('entitlementDays', 'carriedOverDays', 'adjustmentDays', 'carryOverExpiresOn')),
           str(js)[:200])

print('\n=== LEAVE SETTINGS: money, finance grant only ===')
c, js, _ = call('GET', '/api/leave-settings', AD)
expect('LV-10', 'admin WITHOUT the finance grant cannot read leave settings (it is money)', c, [403])
c, js, _ = call('GET', '/api/leave-settings', SA)
expect('LV-11', 'super admin always reads leave settings', c, 200)

# Leave settings need the company profile row to exist.
call('PUT', '/api/company-settings', SA, {'name': 'QA Firma'})
c, js, _ = call('PUT', '/api/leave-settings', SA, {'annualLeaveDailyRate': 32, 'holidayCountryCode': 'DE'})
expect('LV-12', 'super admin sets the daily leave rate and holiday country', c, 200, str(js)[:150])

print('\n=== REFUNDS: who decides after the customer rule ===')
c, refund, _ = call('POST', '/api/v1/refunds', W, {
    'amount': 12.5, 'currency': 'EUR', 'expenseDate': dt.date.today().isoformat(), 'description': 'QA rukavice',
})
expect('RF-01', 'worker asks for a refund', c, 201)
rid = refund.get('id') if isinstance(refund, dict) else None
if rid:
    c, js, _ = call('POST', f'/api/v1/refunds/{rid}/review', PM, {'status': 'Approved'})
    expect('RF-02', 'a project manager may NOT decide a refund (customer rule: Admin/SuperAdmin only)', c, [403])
    c, js, _ = call('POST', f'/api/v1/refunds/{rid}/review', AD, {'status': 'Approved', 'payrollYear': 2026, 'payrollMonth': 12})
    expect('RF-03', 'admin decides a refund', c, [200], str(js)[:150])

print('\n=== FINANCE GRANT: cannot reach a worker or customer, takes effect at once ===')
me_worker = call('GET', '/api/auth/me', W)[1] or {}
worker_uid = me_worker.get('id')
me_admin = call('GET', '/api/auth/me', AD)[1] or {}
admin_uid = me_admin.get('id')

if worker_uid:
    c, js, _ = call('PUT', f'/api/users/{worker_uid}', SA, {
        'email': me_worker.get('email'), 'role': 'Worker', 'employeeId': E['worker'], 'financeAccess': 'Full',
    })
    expect('FG-01', 'the finance grant cannot be given to a Worker account', c, [409], str(js)[:150])
else:
    record('FG-01', 'the finance grant cannot be given to a Worker account', False, 'could not resolve worker user id')

c, js, _ = call('GET', '/api/material-movements', AD)
expect('FG-02', 'admin without the grant is refused amounts before any grant is given', c, [403])

if admin_uid:
    c, js, _ = call('PUT', f'/api/users/{admin_uid}', SA, {
        'email': me_admin.get('email'), 'role': 'Admin', 'financeAccess': 'Full',
    })
    expect('FG-03', 'super admin grants the admin the finance right', c, [200], str(js)[:150])
    c, js, _ = call('GET', '/api/material-movements', AD)
    expect('FG-04', 'granted admin reads amounts immediately, same token', c, [200], str(js)[:150])

print('\n=== CUSTOMER COMPANIES AND INVOICES ===')
# st['customer'] is the customer login's own access token (used as CU in the other flow
# scripts), not the customer record id, so the id is read off the project instead.
_p1_detail = call('GET', f'/api/projects/{P1}', SA)[1] or {}
cust_id = _p1_detail.get('customerId')

c, js, _ = call('GET', f'/api/customers/{cust_id}/companies', AD)
expect('IN-01', 'no companies to start: the client is invoiced as a whole', c, 200)
starts_empty = isinstance(js, list) and len(js) == 0
record('IN-02', 'company list starts empty for this fixture client', starts_empty, str(js)[:150])

c, co1, _ = call('POST', f'/api/customers/{cust_id}/companies', AD, {'name': 'QA Firma A'})
expect('IN-03', 'admin adds a company to the client', c, [200, 201], str(co1)[:150])
c, co2, _ = call('POST', f'/api/customers/{cust_id}/companies', AD, {'name': 'QA Firma B'})
expect('IN-04', 'admin adds a second company', c, [200, 201], str(co2)[:150])

c, js, _ = call('POST', f'/api/customers/{cust_id}/companies', FO, {'name': 'Ne smije'})
expect('IN-05', 'a foreman may NOT add a company', c, [403])

if isinstance(co1, dict) and isinstance(co2, dict):
    co1id, co2id = co1['id'], co2['id']

    c, js, _ = call('POST', '/api/invoices', AD, {
        'projectId': P1, 'number': f'QA-INV-{int(time.time())}', 'issueDate': dt.date.today().isoformat(),
        'amount': 100, 'companyIds': [co1id, co2id],
    })
    expect('IN-06', 'invoice split evenly among two companies adds up to the cent', c, [200, 201], str(js)[:200])
    if c in (200, 201) and isinstance(js, dict):
        shares = js.get('shares', [])
        total = sum(s['amount'] for s in shares)
        record('IN-07', 'the two parts add up exactly to 100', abs(total - 100) < 0.001, str(shares))
        invoice_number = js.get('number')

        c, js2, _ = call('POST', '/api/invoices', AD, {
            'projectId': P1, 'number': invoice_number, 'issueDate': dt.date.today().isoformat(), 'amount': 50,
        })
        expect('IN-08', 'the same invoice number cannot be issued twice', c, [409], str(js2)[:150])

    c, js, _ = call('POST', '/api/invoices', AD, {
        'projectId': P1, 'number': f'QA-INV-B-{int(time.time())}', 'issueDate': dt.date.today().isoformat(),
        'amount': 1000,
        'shares': [{'customerCompanyId': co1id, 'amount': 400}, {'customerCompanyId': co2id, 'amount': 500}],
    })
    expect('IN-09', 'typed parts that do not add up to the whole are refused', c, [409], str(js)[:200])

c, js, _ = call('POST', '/api/invoices', FO, {
    'projectId': P1, 'number': f'QA-INV-C-{int(time.time())}', 'issueDate': dt.date.today().isoformat(), 'amount': 10,
})
expect('IN-10', 'a foreman may NOT record an invoice', c, [403])

c, js, _ = call('GET', '/api/invoices', AD)
expect('IN-11', 'admin (now granted) lists invoices', c, 200)

print(f"\nSUMMARY: {sum(1 for r in RESULTS if r[2] == 'PASS')} pass, "
      f"{sum(1 for r in RESULTS if r[2] == 'FAIL')} fail")
