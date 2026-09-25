import json, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from qa_lib import *

st = fresh_state()
sw = call('GET', '/swagger/v1/swagger.json')[1]  # served in Development only
roles = ['anon', 'customer', 'worker', 'foreman', 'pm', 'admin', 'super']

gets = sorted(p for p, ops in sw['paths'].items()
              if p.startswith('/api/') and not p.startswith('/api/v1') and '{' not in p and 'get' in ops)
print(len(gets), 'parameterless GET endpoints')

from concurrent.futures import ThreadPoolExecutor
matrix = {}
five = []

def probe(p):
    row = {}
    q = ''
    if any(x in p.lower() for x in ('costs', 'finance', 'exports', 'summary', 'series', 'breakdown', 'realization')):
        q = '?from=2026-09-01&to=2026-09-25'
    for r in roles:
        tok = None if r == 'anon' else st[r]
        code, js, _ = call('GET', p + q, tok, timeout=60)
        row[r] = code
        if code >= 500 or code == 0:
            five.append((p + q, r, code, str(js)[:160]))
    return p, row

with ThreadPoolExecutor(8) as ex:
    for p, row in ex.map(probe, gets):
        matrix[p] = row

json.dump(matrix, open(os.path.join(os.path.dirname(__file__), 'qa_matrix.json'), 'w'), indent=1)  # gitignored output
print('\n=== 5xx / transport errors ===')
for f in five:
    print(f)

print('\n=== anonymous access that returned 200 (should be only public endpoints) ===')
for p, row in matrix.items():
    if row['anon'] == 200:
        print(p)

print('\n=== Worker/Customer got 200 (review whether intended) ===')
for p, row in matrix.items():
    if row['customer'] == 200:
        print('CUSTOMER 200:', p)
for p, row in matrix.items():
    if row['worker'] == 200 and row['customer'] != 200:
        print('worker 200:', p)
