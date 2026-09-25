import json, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from qa_lib import *

SUFFIX = str(int(time.time()))[-6:]
state = {}

sa = login('qa.super@construction.local', 'QaPassword123!')['accessToken']
state['super'] = sa


def must(code_js, what):
    code, js, _ = code_js
    if code not in (200, 201, 204):
        raise RuntimeError(f'{what} failed: {code} {js}')
    return js


cust = must(call('POST', '/api/customers', sa, {'name': f'QA Klijent {SUFFIX}', 'email': f'klijent{SUFFIX}@example.com'}), 'customer')
cust2 = must(call('POST', '/api/customers', sa, {'name': f'QA Klijent B {SUFFIX}'}), 'customer2')
state['customer'] = cust['id']
state['customer2'] = cust2['id']

p1 = must(call('POST', '/api/projects', sa, {'name': f'QA Gradiliste A {SUFFIX}', 'status': 'Active', 'latitude': 45.815, 'longitude': 15.9819,
                                               'countryCode': 'HR', 'customerId': cust['id'], 'contractValue': 100000, 'budget': 80000}), 'project1')
p2 = must(call('POST', '/api/projects', sa, {'name': f'QA Gradiliste B {SUFFIX}', 'status': 'Active', 'latitude': 46.05, 'longitude': 14.5,
                                               'countryCode': 'SI', 'customerId': cust2['id']}), 'project2')
state['p1'] = p1['id']
state['p2'] = p2['id']


def emp(n, last, etype='Employee'):
    e = must(call('POST', '/api/employees', sa, {'employeeNumber': f'QA-{SUFFIX}-{n}', 'firstName': f'Radnik{n}', 'lastName': last,
                                                   'position': 'Zidar', 'employmentDate': '2026-01-01', 'status': 'Active', 'type': etype}), f'employee {n}')
    return e['id']


emps = {'worker': emp(1, 'Prvi'), 'worker2': emp(2, 'Drugi'), 'foreman': emp(3, 'Predradnik'), 'sub': emp(4, 'Kooperant', 'Subcontractor')}
state['emp'] = emps

users = {
    'admin': ('Admin', None, None),
    'pm': ('ProjectManager', None, None),
    'foreman': ('Foreman', emps['foreman'], None),
    'worker': ('Worker', emps['worker'], None),
    'worker2': ('Worker', emps['worker2'], None),
    'customer': ('Customer', None, cust['id']),
    'customer2': ('Customer', None, cust2['id']),
}
state['creds'] = {}
for name, (role, eid, cid) in users.items():
    email = f'qa.{name}.{SUFFIX}@construction.local'
    pw = 'QaPassword123!'
    body = {'email': email, 'password': pw, 'role': role}
    if eid: body['employeeId'] = eid
    if cid: body['customerId'] = cid
    must(call('POST', '/api/users', sa, body), f'user {name}')
    state['creds'][name] = (email, pw)
    state[name] = login(email, pw)['accessToken']

# Assign the crew to project 1; worker2 to project 2 only.
for k in ('worker', 'foreman'):
    call('POST', f'/api/employees/{emps[k]}/projects/{p1["id"]}', sa)
call('POST', f'/api/employees/{emps["worker2"]}/projects/{p2["id"]}', sa)
call('POST', f'/api/employees/{emps["sub"]}/projects/{p1["id"]}', sa)

json.dump(state, open(os.path.join(os.path.dirname(__file__), 'qa_state.json'), 'w'), indent=1)
print('setup ok', SUFFIX)
