#!/usr/bin/env python3
"""A local S3 that checks the signature, for testing the off-site copy.

Not a mock. It implements the *verifying* half of AWS Signature Version 4
independently of ``s3-sigv4.sh`` — same published algorithm, different
language, hashlib and hmac rather than the openssl command line — and refuses
any request whose signature it cannot reproduce. That independence is the
point: a fake that accepted anything would let a signer emitting gibberish
pass every test in ``test-offsite.sh``.

It is deliberately not a complete S3. No listing, no multipart, no ACLs, no
versioning — PUT, GET, HEAD and nothing else, because that is all the backup
path uses.

Nothing here belongs in a deployment. It has no TLS and its credentials are
arguments.
"""

from __future__ import annotations

import argparse
import hashlib
import hmac
import os
import re
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

EMPTY_SHA256 = hashlib.sha256(b"").hexdigest()

AUTHORIZATION = re.compile(
    r"AWS4-HMAC-SHA256 "
    r"Credential=(?P<access_key>[^/]+)/(?P<date>\d{8})/(?P<region>[^/]+)/"
    r"(?P<service>[^/]+)/aws4_request, "
    r"SignedHeaders=(?P<signed_headers>[^,]+), "
    r"Signature=(?P<signature>[0-9a-f]+)"
)


def signing_key(secret: str, date: str, region: str, service: str) -> bytes:
    """The four-step key derivation, straight from the AWS documentation."""
    key = f"AWS4{secret}".encode()

    for part in (date, region, service, "aws4_request"):
        key = hmac.new(key, part.encode(), hashlib.sha256).digest()

    return key


class Handler(BaseHTTPRequestHandler):
    # Silences the per-request line; the test prints its own results.
    def log_message(self, *_args):  # noqa: D102
        pass

    # ---- signature ------------------------------------------------------

    def _expected_signature(self, method: str, payload_hash: str) -> str | None:
        header = self.headers.get("Authorization", "")
        match = AUTHORIZATION.match(header)

        if not match:
            return None

        if match["access_key"] != self.server.access_key:
            return None

        signed_headers = match["signed_headers"].split(";")

        canonical_headers = ""
        for name in signed_headers:
            value = self.headers.get(name, "")
            canonical_headers += f"{name}:{value.strip()}\n"

        path = self.path.split("?", 1)[0]
        query = self.path.split("?", 1)[1] if "?" in self.path else ""

        canonical_request = "\n".join([
            method,
            path,
            query,
            canonical_headers,
            ";".join(signed_headers),
            payload_hash,
        ])

        amz_date = self.headers.get("x-amz-date", "")
        scope = f"{match['date']}/{match['region']}/{match['service']}/aws4_request"

        string_to_sign = "\n".join([
            "AWS4-HMAC-SHA256",
            amz_date,
            scope,
            hashlib.sha256(canonical_request.encode()).hexdigest(),
        ])

        key = signing_key(
            self.server.secret_key,
            match["date"],
            match["region"],
            match["service"],
        )

        expected = hmac.new(key, string_to_sign.encode(), hashlib.sha256).hexdigest()

        return expected if hmac.compare_digest(expected, match["signature"]) else None

    def _authorized(self, method: str, payload_hash: str) -> bool:
        if self._expected_signature(method, payload_hash) is not None:
            return True

        self.send_response(403)
        self.end_headers()
        self.wfile.write(b"<Error><Code>SignatureDoesNotMatch</Code></Error>")
        return False

    # ---- storage --------------------------------------------------------

    def _object_path(self) -> Path:
        # Path-style only, which is what the test configures. The key is taken
        # apart rather than joined blindly so a `..` in it cannot escape.
        relative = self.path.split("?", 1)[0].lstrip("/")
        parts = [p for p in relative.split("/") if p not in ("", ".", "..")]

        return Path(self.server.root, *parts)

    def do_PUT(self):  # noqa: N802
        length = int(self.headers.get("Content-Length", "0"))
        body = self.rfile.read(length)

        declared = self.headers.get("x-amz-content-sha256", "")

        if not self._authorized("PUT", declared):
            return

        # The signature binds the request to this hash; checking the body
        # against it is what makes that binding mean anything.
        if hashlib.sha256(body).hexdigest() != declared:
            self.send_response(400)
            self.end_headers()
            self.wfile.write(b"<Error><Code>XAmzContentSHA256Mismatch</Code></Error>")
            return

        path = self._object_path()
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(body)

        self.send_response(200)
        self.send_header("ETag", '"' + hashlib.md5(body).hexdigest() + '"')
        self.send_header("Content-Length", "0")
        self.end_headers()

    def do_GET(self):  # noqa: N802
        if self.path == "/healthz":
            self.send_response(200)
            self.send_header("Content-Length", "2")
            self.end_headers()
            self.wfile.write(b"ok")
            return

        if not self._authorized("GET", EMPTY_SHA256):
            return

        path = self._object_path()

        if not path.is_file():
            self.send_response(404)
            self.end_headers()
            return

        body = path.read_bytes()

        self.send_response(200)
        self.send_header("Content-Length", str(len(body)))
        self.send_header("ETag", '"' + hashlib.md5(body).hexdigest() + '"')
        self.end_headers()
        self.wfile.write(body)

    def do_HEAD(self):  # noqa: N802
        if not self._authorized("HEAD", EMPTY_SHA256):
            return

        path = self._object_path()

        if not path.is_file():
            self.send_response(404)
            self.end_headers()
            return

        body = path.read_bytes()

        self.send_response(200)
        self.send_header("Content-Length", str(len(body)))
        self.send_header("ETag", '"' + hashlib.md5(body).hexdigest() + '"')
        self.end_headers()


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, default=18099)
    parser.add_argument("--root", required=True)
    parser.add_argument("--access-key", required=True)
    parser.add_argument("--secret-key", required=True)
    parser.add_argument("--region", default="us-east-1")

    args = parser.parse_args()

    os.makedirs(args.root, exist_ok=True)

    server = ThreadingHTTPServer(("127.0.0.1", args.port), Handler)
    server.root = args.root
    server.access_key = args.access_key
    server.secret_key = args.secret_key
    server.region = args.region

    server.serve_forever()


if __name__ == "__main__":
    main()
