"""Small QA harness for the Construction API: stdlib only."""
import json
import os
import ssl
import time
import urllib.request
import urllib.error

BASE = os.environ.get('QA_BASE', 'http://localhost:5000')
CTX = ssl.create_default_context()

RESULTS = []  # (id, title, status, detail)


def call(method, path, token=None, body=None, headers=None, raw=None, timeout=30):
    url = BASE + path
    data = None
    h = {'Accept': 'application/json'}
    if body is not None:
        data = json.dumps(body).encode()
        h['Content-Type'] = 'application/json'
    if raw is not None:
        data = raw
        h['Content-Type'] = 'application/json'
    if token:
        h['Authorization'] = 'Bearer ' + token
    if headers:
        h.update(headers)
    req = urllib.request.Request(url, data=data, headers=h, method=method)
    try:
        with urllib.request.urlopen(req, timeout=timeout, context=CTX) as r:
            text = r.read().decode('utf-8', 'replace')
            code = r.status
            hdrs = dict(r.headers)
    except urllib.error.HTTPError as e:
        text = e.read().decode('utf-8', 'replace')
        code = e.code
        hdrs = dict(e.headers)
    except Exception as e:  # network / timeout
        return 0, {'error': str(e)}, {}
    try:
        js = json.loads(text) if text else None
    except Exception:
        js = text
    return code, js, hdrs


def login(email, password):
    code, js, _ = call('POST', '/api/auth/login', body={'email': email, 'password': password})
    if code != 200:
        raise RuntimeError(f'login {email} -> {code} {js}')
    return js


def record(tid, title, ok, detail=''):
    RESULTS.append((tid, title, 'PASS' if ok else 'FAIL', detail))
    mark = 'PASS' if ok else 'FAIL'
    print(f'[{mark}] {tid} {title}' + (f'  -- {detail}' if detail and not ok else ''))


def expect(tid, title, actual, allowed, detail=''):
    ok = actual in (allowed if isinstance(allowed, (list, tuple, set)) else [allowed])
    record(tid, title, ok, f'got {actual}, expected {allowed}. {detail}')
    return ok


def fresh_state():
    """Load the fixture ids and sign every role in again (access tokens are short-lived)."""
    st = json.load(open(os.path.join(os.path.dirname(__file__), 'qa_state.json')))
    st['super'] = login('qa.super@construction.local', 'QaPassword123!')['accessToken']
    for name, (email, pw) in st['creds'].items():
        st[name] = login(email, pw)['accessToken']
    return st
