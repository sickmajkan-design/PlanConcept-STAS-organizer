import re
import json, os, sys, random, uuid
sys.path.insert(0, os.path.dirname(__file__))
from qa_lib import *
from concurrent.futures import ThreadPoolExecutor

st = fresh_state()
SU = st['super']
sw = call('GET', '/swagger/v1/swagger.json')[1]  # served in Development only
schemas = sw['components']['schemas']

SKIP = ('logout', 'change-password', '/privacy', 'maintenance', '/setup', 'forgot-password', 'reset-password', 'import', 'erase', 'assistant')
STRINGS = ['', ' ', 'x' * 20000, "'; DROP TABLE users;--", '<script>alert(1)</script>', '"><img src=x onerror=alert(1)>',
           '../../etc/passwd', '\u0000', '😀' * 500, 'Ćirilica Ђ Љ Њ', '{{7*7}}', '${jndi:ldap://x}', '\n\r\t']
NUMS = [0, -1, 1e15, -1e15, 0.000001, 2147483648, 3.14159265358979]


def sch(ref):
    return schemas[ref.split('/')[-1]]


def gen(s, mode, depth=0):
    if depth > 4:
        return None
    if '$ref' in s:
        return gen(sch(s['$ref']), mode, depth + 1)
    if 'allOf' in s:
        return gen(s['allOf'][0], mode, depth + 1)
    t = s.get('type')
    if 'enum' in s:
        return random.choice(s['enum'] + ['NoSuchValue', 999])
    if t == 'object' or 'properties' in s:
        return {k: gen(v, mode, depth + 1) for k, v in s.get('properties', {}).items()}
    if t == 'array':
        return [gen(s.get('items', {}), mode, depth + 1) for _ in range(3 if mode == 'attack' else 1)]
    if t == 'string':
        f = s.get('format')
        if f in ('uuid',):
            return random.choice([str(uuid.uuid4()), 'not-a-guid', '00000000-0000-0000-0000-000000000000'])
        if f in ('date', 'date-time'):
            return random.choice(['2026-09-25', '0001-01-01', '9999-12-31', 'garbage', '2026-02-30', '2026-09-25T25:61:61Z'])
        return random.choice(STRINGS)
    if t in ('integer', 'number'):
        return random.choice(NUMS)
    if t == 'boolean':
        return random.choice([True, False, 'maybe'])
    return random.choice([None, 'x', 1])


targets = []
for p, ops in sw['paths'].items():
    if not p.startswith('/api/') or p.startswith('/api/v1'):
        continue
    if any(x in p.lower() for x in SKIP):
        continue
    for m in ('post', 'put'):
        if m in ops:
            rb = ops[m].get('requestBody', {}).get('content', {}).get('application/json', {}).get('schema')
            targets.append((m.upper(), p, rb))
print(len(targets), 'write endpoints to fuzz')

five = []
stats = {}


def fuzz(t):
    m, p, rb = t
    path = p
    for seg in ('{id}', '{employeeId}', '{projectId}'):
        path = path.replace(seg, str(uuid.uuid4()))
    import re
    path = re.sub(r'\{[^}]+\}', str(uuid.uuid4()), path)
    out = []
    bodies = [{}, None]
    if rb:
        for _ in range(6):
            bodies.append(gen(rb, 'attack'))
    for b in bodies:
        code, js, _ = call(m, path, SU, body=b if b is not None else None, raw=(b'null' if b is None else None), timeout=60)
        out.append(code)
        if code >= 500 or code == 0:
            five.append((m, path, code, json.dumps(b)[:300], str(js)[:200]))
    return p, out


with ThreadPoolExecutor(6) as ex:
    for p, out in ex.map(fuzz, targets):
        stats[p] = out

print('\n=== 5xx from fuzzed input ===')
seen = set()
for f in five:
    key = (f[0], re.sub(r'[0-9a-f-]{36}', '{id}', f[1]) if False else f[1][:60])
    print(f)
print(len(five), 'server errors across', len(targets), 'endpoints')
json.dump(stats, open(os.path.join(os.path.dirname(__file__), 'qa_fuzz.json'), 'w'))  # gitignored output
