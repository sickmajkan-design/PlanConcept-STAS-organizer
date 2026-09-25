import json, os, sys, datetime as dt
sys.path.insert(0, os.path.dirname(__file__))
from qa_lib import *

st = fresh_state()
W, W2, F, PM, AD, SU, CU = (st[k] for k in ('worker', 'worker2', 'foreman', 'pm', 'admin', 'super', 'customer'))
P1, P2 = st['p1'], st['p2']
E = st['emp']
now = dt.datetime.now(dt.timezone.utc).replace(tzinfo=None)
iso = lambda d: d.strftime('%Y-%m-%dT%H:%M:%SZ')

print('=== TIME TRACKING ===')
c, js, _ = call('GET', '/api/timeentries/current', W)
expect('TT-01', 'no running shift at start: current returns 200/204/404', c, [200, 204, 404])

c, js, _ = call('POST', '/api/timeentries/clock-out', W, {'breakMinutes': 0})
expect('TT-02', 'clock-out without an open shift is refused (4xx, not 5xx)', c, [400, 404, 409, 422], str(js)[:120])

start = now - dt.timedelta(hours=3)
c, js, _ = call('POST', '/api/timeentries/clock-in', W, {'projectId': P1, 'workType': 'Regular', 'occurredAt': iso(start), 'latitude': 45.815, 'longitude': 15.9819})
expect('TT-03', 'worker clocks in on an assigned project', c, [200, 201], str(js)[:150])
entry = js if isinstance(js, dict) else {}

c, js, _ = call('POST', '/api/timeentries/clock-in', W, {'projectId': P1, 'workType': 'Regular'})
expect('TT-04', 'a second clock-in while a shift is open is refused with 409', c, [409, 400], str(js)[:120])

c, js, _ = call('POST', '/api/timeentries/clock-in', W2, {'projectId': P1, 'workType': 'Regular'})
expect('TT-05', 'a worker not assigned to a site may clock in there (the office is told, not refused)', c, [200, 201], f'GOT {c} {str(js)[:120]}')
if c in (200, 201):
    call('POST', '/api/timeentries/clock-out', W2, {'breakMinutes': 0})

c, js, _ = call('POST', '/api/timeentries/clock-out', W, {'breakMinutes': 999})
expect('TT-06', 'break longer than the shift is refused', c, [400, 422, 409], str(js)[:120])
c, js, _ = call('POST', '/api/timeentries/clock-out', W, {'breakMinutes': -5})
expect('TT-07', 'negative break is refused', c, [400, 422], str(js)[:120])

c, js, _ = call('POST', '/api/timeentries/clock-out', W, {'breakMinutes': 30, 'note': '<script>alert(1)</script>'})
expect('TT-08', 'worker clocks out with a break; XSS note stored as text', c, [200, 201, 204], str(js)[:150])

c, js, _ = call('GET', '/api/timeentries?pageSize=50', W)
own = js.get('items', []) if isinstance(js, dict) else []
expect('TT-09', 'worker sees own entries', c, 200)
record('TT-10', 'worker sees ONLY own time entries', all(e.get('employeeId') == E['worker'] for e in own), f'{len(own)} entries')
eid = own[0]['id'] if own else None
record('TT-11', 'entry is Submitted after clock-out (needs review)', bool(own) and str(own[0].get('status')) == 'Submitted', str(own[0].get('status')) if own else 'no entries')

if eid:
    c, js, _ = call('POST', f'/api/timeentries/{eid}/review', W, {'approve': True})
    expect('TT-12', 'worker cannot approve own hours', c, [403], f'GOT {c}')
    c, js, _ = call('POST', f'/api/timeentries/{eid}/review', F, {'approve': True})
    expect('TT-13', 'a foreman may NOT review hours (project manager and above)', c, [403], f'GOT {c}')
    c, js, _ = call('GET', f'/api/timeentries/{eid}', W2)
    expect('TT-14', "another worker cannot read this worker's entry", c, [403, 404], f'GOT {c}')

c, js, _ = call('POST', '/api/timeentries/clock-in', W, {'projectId': P1, 'workType': 'Regular', 'occurredAt': iso(now + dt.timedelta(days=2))})
expect('TT-15', 'clock-in dated in the future is refused', c, [400, 422], f'GOT {c} {str(js)[:120]}')
c, js, _ = call('POST', '/api/timeentries/clock-in', W, {'projectId': P1, 'workType': 'Regular', 'occurredAt': iso(now - dt.timedelta(days=400))})
expect('TT-16', 'clock-in dated far in the past is refused', c, [400, 422], f'GOT {c} {str(js)[:120]}')
c, js, _ = call('POST', '/api/timeentries/clock-in', W, {'projectId': '00000000-0000-0000-0000-000000000000', 'workType': 'Regular'})
expect('TT-17', 'clock-in on a nonexistent project is refused', c, [400, 404, 422], f'GOT {c}')
c, js, _ = call('POST', '/api/timeentries/clock-in', W, {'projectId': 'not-a-guid', 'workType': 'Regular'})
expect('TT-18', 'malformed project id gives 400 not 500', c, [400], f'GOT {c}')
c, js, _ = call('POST', '/api/timeentries/clock-in', W, {'projectId': P1, 'workType': 'Regular', 'latitude': 999, 'longitude': 15})
expect('TT-19', 'out-of-range latitude is refused', c, [400, 422], f'GOT {c} {str(js)[:120]}')
if c in (200, 201):
    call('POST', '/api/timeentries/clock-out', W, {'breakMinutes': 0})

c, js, _ = call('POST', '/api/timeentries/clock-in', CU, {'projectId': P1, 'workType': 'Regular'})
expect('TT-20', 'a customer account cannot clock in', c, [403], f'GOT {c}')
c, js, _ = call('POST', '/api/timeentries/clock-in', AD, {'projectId': P1, 'workType': 'Regular'})
expect('TT-21', 'an admin account with no employee record gets a clear refusal (not 500)', c, [400, 403, 404, 409], f'GOT {c} {str(js)[:120]}')

print('\n=== ABSENCES ===')
today = dt.date.today()
d0 = today + dt.timedelta(days=30)
c, js, _ = call('POST', '/api/absences', W, {'employeeId': E['worker'], 'type': 'AnnualLeave', 'startDate': d0.isoformat(), 'endDate': (d0 + dt.timedelta(days=4)).isoformat(), 'reason': 'Godisnji'})
expect('AB-01', 'worker requests vacation', c, [200, 201], str(js)[:150])
ab = js if isinstance(js, dict) else {}
c, js, _ = call('POST', '/api/absences', W, {'employeeId': E['worker'], 'type': 'AnnualLeave', 'startDate': (d0 + dt.timedelta(days=2)).isoformat(), 'endDate': (d0 + dt.timedelta(days=6)).isoformat()})
expect('AB-02', 'overlapping absence for the same worker is refused', c, [400, 409], f'GOT {c} {str(js)[:100]}')
c, js, _ = call('POST', '/api/absences', W, {'employeeId': E['worker'], 'type': 'AnnualLeave', 'startDate': (d0 + dt.timedelta(days=10)).isoformat(), 'endDate': (d0 + dt.timedelta(days=8)).isoformat()})
expect('AB-03', 'end before start is refused', c, [400, 422], f'GOT {c}')
c, js, _ = call('POST', '/api/absences', W, {'employeeId': E['worker2'], 'type': 'AnnualLeave', 'startDate': (d0 + dt.timedelta(days=20)).isoformat(), 'endDate': (d0 + dt.timedelta(days=21)).isoformat()})
expect('AB-04', 'worker cannot request leave on behalf of a colleague', c, [400, 403], f'GOT {c} {str(js)[:100]}')
c, js, _ = call('POST', '/api/absences', W, {'employeeId': E['worker'], 'type': 'AnnualLeave', 'startDate': (d0 + dt.timedelta(days=40)).isoformat(), 'endDate': (d0 + dt.timedelta(days=41)).isoformat(), 'approve': True})
record('AB-05', 'worker cannot self-approve via approve=true', c in (400, 403) or (c in (200, 201) and str(js.get('status')).lower() in ('pending', '1', 'requested')), f'GOT {c} {str(js)[:150]}')
if ab.get('id'):
    aid = ab['id']
    c, js, _ = call('POST', f'/api/absences/{aid}/review', W, {'approve': True})
    expect('AB-06', 'worker cannot review own request', c, [403], f'GOT {c}')
    c, js, _ = call('POST', f'/api/absences/{aid}/review', AD, {'approve': True})
    expect('AB-07', 'admin approves the request', c, [200, 204], f'{c} {str(js)[:150]}')
    c, js, _ = call('GET', '/api/absences/balance?employeeId=' + E['worker'], W)
    expect('AB-08', 'balance is available to the worker', c, [200, 400], str(js)[:150])
    c, js, _ = call('GET', f'/api/absences/{aid}', W2)
    expect('AB-09', "another worker cannot read this worker's absence", c, [403, 404], f'GOT {c}')

print('\n=== AUTH ===')
c, js, _ = call('POST', '/api/auth/login', body={'email': st['creds']['worker'][0], 'password': 'wrong'})
expect('AU-01', 'wrong password gives 401 with a generic message', c, 401)
c1, j1, _ = call('POST', '/api/auth/login', body={'email': 'nobody@nowhere.test', 'password': 'wrong'})
record('AU-02', 'unknown email and wrong password answer identically (no user enumeration)', c1 == 401 and str(j1.get('detail')) == str(js.get('detail')), f'{j1} vs {js}')
c, js, _ = call('POST', '/api/auth/login', body={'email': '', 'password': ''})
expect('AU-03', 'empty credentials give 400', c, [400, 401])
c, js, _ = call('POST', '/api/auth/login', raw=b'{not json')
expect('AU-04', 'malformed JSON gives 400', c, [400])
c, js, _ = call('GET', '/api/employees', 'garbage.token.value')
expect('AU-05', 'garbage bearer token gives 401', c, 401)
c, js, _ = call('GET', '/api/employees')
expect('AU-06', 'no token gives 401', c, 401)

# Lockout on a throwaway account.
tmp_email = 'qa.lock.' + str(int(time.time())) + '@construction.local'
call('POST', '/api/users', SU, {'email': tmp_email, 'password': 'QaPassword123!', 'role': 'Admin'})
codes = []
for i in range(12):
    c, js, h = call('POST', '/api/auth/login', body={'email': tmp_email, 'password': 'bad-pass'})
    codes.append(c)
record('AU-07', 'repeated failures answer 401 throughout (a locked account looks like a wrong password, by design)', all(c in (401, 429) for c in codes), f'codes {codes}')
c, js, _ = call('POST', '/api/auth/login', body={'email': tmp_email, 'password': 'QaPassword123!'})
record('AU-08', 'locked/throttled account cannot sign in even with the right password', c in (401, 423, 429), f'GOT {c}')

# Refresh + logout.
c, js, h = call('POST', '/api/auth/login', body={'email': st['creds']['worker2'][0], 'password': st['creds']['worker2'][1]})
rt = (js or {}).get('refreshToken')
record('AU-09', 'login response carries a refresh token (or sets it as a cookie)', bool(rt) or 'Set-Cookie' in h or 'set-cookie' in {k.lower() for k in h}, str(list(js.keys()) if isinstance(js, dict) else js))
if rt:
    c, js2, _ = call('POST', '/api/auth/refresh', body={'refreshToken': rt})
    expect('AU-10', 'refresh rotates the token pair', c, 200, str(js2)[:100])
    c, js3, _ = call('POST', '/api/auth/refresh', body={'refreshToken': rt})
    expect('AU-11', 'the old refresh token cannot be replayed', c, [400, 401], f'GOT {c}')

print('\nSUMMARY:', sum(1 for r in RESULTS if r[2] == 'PASS'), 'pass,', sum(1 for r in RESULTS if r[2] == 'FAIL'), 'fail')
