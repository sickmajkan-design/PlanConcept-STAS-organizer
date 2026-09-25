import json, os, sys, datetime as dt
sys.path.insert(0, os.path.dirname(__file__))
from qa_lib import *

st = fresh_state()
W, W2, F, PM, AD, SU, CU, CU2 = (st[k] for k in ('worker', 'worker2', 'foreman', 'pm', 'admin', 'super', 'customer', 'customer2'))
P1, P2, E = st['p1'], st['p2'], st['emp']
today = dt.date.today()
d = lambda n: (today + dt.timedelta(days=n)).isoformat()

print('=== ABSENCES (continued) ===')
c, js, _ = call('GET', '/api/absences?pageSize=100', W)
mine = (js or {}).get('items', [])
record('AB-13', 'worker lists only own absences', c == 200 and all(a.get('employeeId') == E['worker'] for a in mine), f'{len(mine)} items')
a_ids = [a['id'] for a in mine]
c, js, _ = call('GET', '/api/absences?pageSize=100', W2)
record('AB-14', 'other worker sees none of them', c == 200 and not (set(a['id'] for a in (js or {}).get('items', [])) & set(a_ids)), '')
c, js, _ = call('GET', f'/api/absences/balance?employeeId={E["worker"]}', W)
expect('AB-08', 'worker reads own leave balance', c, 200, str(js)[:150])
c, js, _ = call('GET', f'/api/absences/balance?employeeId={E["worker2"]}', W)
record('AB-15', "asking for a colleague's balance returns the worker's OWN (narrowed), never the colleague's", c == 200 and js.get('employeeId') == E['worker'], f'GOT {c} {str(js)[:120]}')
# overlap resolved at approval: two overlapping pending, approve both -> second must conflict
c, a1, _ = call('POST', '/api/absences', W2, {'employeeId': E['worker2'], 'type': 'AnnualLeave', 'startDate': d(60), 'endDate': d(64)})
c, a2, _ = call('POST', '/api/absences', W2, {'employeeId': E['worker2'], 'type': 'SickLeave', 'startDate': d(62), 'endDate': d(66)})
c1, _, _ = call('POST', f'/api/absences/{a1["id"]}/review', AD, {'approve': True})
c2, j2, _ = call('POST', f'/api/absences/{a2["id"]}/review', AD, {'approve': True})
record('AB-02b', 'two overlapping absences cannot both be approved', c1 in (200, 204) and c2 in (400, 409), f'first {c1}, second {c2} {str(j2)[:100]}')

print('\n=== FINANCE RIGHTS: grant, use, revoke ===')
u = None
for pg in range(1, 40):
    js = call('GET', f'/api/users?pageSize=100&pageNumber={pg}', SU)[1] or {}
    u = next((x for x in js.get('items', []) if x['email'] == st['creds']['admin'][0]), None)
    if u or not js.get('hasNextPage'):
        break
assert u, 'admin user not found in the list'
def set_access(level):
    body = {'email': u['email'], 'role': u['role'], 'employeeId': u.get('employeeId'), 'customerId': u.get('customerId'),
            'documentExpiryReminderDays': u.get('documentExpiryReminderDays'), 'canViewCustomerTaxDetails': u.get('canViewCustomerTaxDetails', False),
            'financeAccess': level}
    return call('PUT', f'/api/users/{u["id"]}', SU, body)
c, js, _ = set_access('Full')
expect('FI-10', 'super admin grants Full finance access to an admin', c, [200, 204], f'{c} {str(js)[:150]}')
adm = login(*st['creds']['admin'])['accessToken']
c, js, _ = call('GET', f'/api/costs/company?from={d(-30)}&to={d(0)}', adm)
expect('FI-11', 'the granted admin can read company costs immediately (no re-login)', c, 200, f'GOT {c}')
c, js, _ = call('POST', '/api/general-expenses', adm, {'category': 'Bookkeeping', 'amount': 250.50, 'occurredOn': d(-1), 'projectId': P1, 'supplier': 'QA dobavljac'})
expect('CO-01', 'admin with the right records an expense on a project', c, [200, 201], f'{c} {str(js)[:150]}')
exp_id = (js or {}).get('id') if isinstance(js, dict) else None
c, js, _ = call('POST', '/api/general-expenses', adm, {'category': 'Bookkeeping', 'amount': -5, 'occurredOn': d(-1)})
expect('CO-02', 'negative expense amount is refused', c, [400, 422])
c, js, _ = call('POST', '/api/general-expenses', adm, {'category': 'Bookkeeping', 'amount': 10, 'occurredOn': d(1000)})
record('CO-04', 'expense dated far in the future is refused', c in (400, 422), f'GOT {c}')
c, js, _ = call('POST', '/api/general-expenses', adm, {'category': 'Bookkeeping', 'amount': 12.345, 'occurredOn': d(-1)})
record('CO-06', '3-decimal amount refused or stored rounded', c in (200, 201, 400, 422), f'GOT {c} amount={(js or {}).get("amount") if isinstance(js, dict) else js}')
c, js, _ = call('POST', '/api/general-expenses', adm, {'category': 'Bookkeeping', 'amount': 0, 'occurredOn': d(-1)})
record('CO-10', 'zero-amount expense refused or accepted consistently (no 5xx)', c < 500, f'GOT {c}')
c, js, _ = call('POST', '/api/general-expenses', adm, {'category': 'NotACategory', 'amount': 10, 'occurredOn': d(-1)})
expect('CO-11', 'unknown category gives 400', c, [400])
c, js, _ = call('POST', '/api/general-expenses', adm, {'category': 'Bookkeeping', 'amount': 10, 'occurredOn': d(-1), 'supplier': 'x' * 5000})
record('CO-12', 'overlong supplier refused (no 5xx, no silent truncation)', c in (400, 422), f'GOT {c}')

c, js, _ = call('GET', f'/api/costs/company?from={d(-30)}&to={d(0)}', SU)
expect('FI-12', 'super admin reads company costs', c, 200)
if isinstance(js, dict) and 'total' in js:
    parts = sum(js[k] for k in ('labour', 'manualPay', 'material', 'generalExpenses', 'accommodation', 'vehicles', 'tools'))
    record('FI-13', 'company total equals the sum of its parts', abs(js['total'] - parts) < 0.011, f"{js['total']} vs {parts}")
c, js, _ = call('GET', f'/api/costs/projects?from={d(-30)}&to={d(0)}', SU)
row = next((r for r in (js or {}).get('rows', []) if r['projectId'] == P1), None)
record('FI-14', 'project cost report shows the expense on its project', row is not None and row['generalExpenseCost'] >= 250.5, str(row))

c, js, _ = set_access('StatisticsOnly')
expect('FI-15', 'downgrade to statistics-only', c, [200, 204])
c, js, _ = call('GET', f'/api/costs/company?from={d(-30)}&to={d(0)}', adm)
expect('FI-16', 'statistics-only admin can no longer read amounts (takes effect at once)', c, [403], f'GOT {c}')
c, js, _ = call('GET', f'/api/finance/statistics?from={d(-30)}&to={d(0)}', adm)
expect('FI-17', 'statistics-only admin reads percentages', c, 200, f'{c} {str(js)[:100]}')
txt = json.dumps(js)
record('FI-18', 'statistics response holds no money amounts (no "amount"/"total" fields)', not any(k in txt.lower() for k in ('"expense"', '"revenue"', '"profit"', '"amount"', '"total"')), txt[:200])
c, js, _ = set_access('None')
c, js, _ = call('GET', f'/api/finance/statistics?from={d(-30)}&to={d(0)}', adm)
expect('FI-19', 'revoked admin cannot read statistics', c, [403])

print('\n=== CUSTOMER PORTAL ISOLATION ===')
c, js, _ = call('GET', '/api/customer-portal/projects', CU)
c2, js2, _ = call('GET', '/api/customer-portal/projects', CU2)
print('  portal projects A:', c, str(js)[:200]); print('  portal projects B:', c2, str(js2)[:200])
if c == 200 and c2 == 200:
    ids1 = {p.get('id') or p.get('projectId') for p in (js if isinstance(js, list) else js.get('items', []))}
    ids2 = {p.get('id') or p.get('projectId') for p in (js2 if isinstance(js2, list) else js2.get('items', []))}
    record('CP-01', 'customer A sees own project only', P1 in ids1 and P2 not in ids1, f'{ids1}')
    record('CP-02', 'customer B sees own project only', P2 in ids2 and P1 not in ids2, f'{ids2}')
    record('CP-03', 'the two customers see disjoint projects', not (ids1 & ids2), '')
for path in ('/api/employees', '/api/projects', '/api/costs/company', '/api/users', '/api/locations/current', '/api/timeentries', '/api/customers'):
    c, js, _ = call('GET', path, CU)
    record('CP-04', f'customer cannot reach staff endpoint {path}', c in (401, 403), f'GOT {c}')
c, js, _ = call('GET', f'/api/projects/{P1}', CU)
record('CP-05', 'customer cannot read the staff project detail even for their own project', c in (401, 403, 404), f'GOT {c}')
c, js, _ = call('GET', f'/api/projects/{P2}', CU)
record('CP-06', "customer cannot read another customer's project", c in (401, 403, 404), f'GOT {c}')

print('\n=== LOCATIONS / CREW SCOPE ===')
c, js, _ = call('POST', '/api/locations', W, {'pings': [{'latitude': 45.815, 'longitude': 15.98, 'accuracy': 5, 'timestamp': dt.datetime.now(dt.timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ')}]})
expect('LO-01', 'worker reports a ping (202: accepted for processing)', c, [200, 201, 202, 204], f'{c} {str(js)[:120]}')
c, js, _ = call('POST', '/api/locations', W, {'pings': []})
record('LO-02', 'empty batch refused or no-op', c in (200, 204, 400), f'GOT {c}')
big = [{'latitude': 45.8, 'longitude': 15.9, 'timestamp': dt.datetime.now(dt.timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ')}] * 121
c, js, _ = call('POST', '/api/locations', W, {'pings': big})
expect('LO-03', 'batch over 120 pings refused', c, [400, 413, 422])
c, js, _ = call('POST', '/api/locations', W, {'pings': [{'latitude': 91, 'longitude': 15.9, 'timestamp': dt.datetime.now(dt.timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ')}]})
expect('LO-04', 'latitude 91 refused', c, [400, 422])
c, js, _ = call('POST', '/api/locations', W, {'pings': [{'latitude': 45.8, 'longitude': 15.9, 'timestamp': '2001-01-01T00:00:00Z'}]})
record('LO-05', 'ancient timestamp refused or dropped, not 5xx', c < 500, f'GOT {c}')
c, js, _ = call('POST', '/api/locations', W, {'pings': [{'latitude': 45.8, 'longitude': 15.9, 'timestamp': '2099-01-01T00:00:00Z'}]})
record('LO-06', 'far-future timestamp refused or dropped, not 5xx', c < 500, f'GOT {c}')
c, js, _ = call('POST', '/api/locations', AD, {'pings': [{'latitude': 45.8, 'longitude': 15.9, 'timestamp': dt.datetime.now(dt.timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ')}]})
record('LO-07', 'an account without an employee cannot report locations', c in (400, 403, 404), f'GOT {c}')
c, js, _ = call('GET', '/api/locations/current', F)
crew = js if isinstance(js, list) else (js or {}).get('items', [])
print('  foreman sees', len(crew), 'positions')
c, js, _ = call('GET', f'/api/locations/employees/{E["worker2"]}/last', F)
record('LO-08', "foreman cannot read the position of someone outside their crew (404, not 403/200)", c == 404, f'GOT {c}')
c, js, _ = call('GET', f'/api/locations/employees/{E["worker"]}/last', F)
record('LO-09', 'foreman can read own crew member', c in (200, 404), f'GOT {c}')
c, js, _ = call('GET', f'/api/locations/employees/{E["worker"]}/last', W)
record('LO-10', 'a worker cannot read positions at all', c in (403,), f'GOT {c}')
c, js, _ = call('GET', '/api/locations/current', W)
record('LO-11', 'a worker cannot read the live map', c in (403,), f'GOT {c}')

print('\nSUMMARY:', sum(1 for r in RESULTS if r[2] == 'PASS'), 'pass,', sum(1 for r in RESULTS if r[2] == 'FAIL'), 'fail')
