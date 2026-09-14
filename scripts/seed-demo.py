#!/usr/bin/env python3
"""
Fills an installation with a believable working day, for demonstrating.

Goes through the public API rather than the database, on purpose. A demo
database built with INSERTs holds states the application itself cannot
produce — overlapping postings, two open shifts for one person, hours outside
the backdating window — and the afternoon is then spent chasing a bug that is
not in the code. Everything here passes the same validation, the same overlap
constraints and the same audit trail as a person typing it in.

Everything it writes is dated from the moment it runs: shifts started this
morning and still running, a task due today, leave that covers today. Run it
again tomorrow and tomorrow is the live day.

Usage:

    export API_BASE_URL=https://your-server
    export DEMO_EMAIL=admin@example.com DEMO_PASSWORD=...   # or use .env
    scripts/seed-demo.py --yes-seed-demo-data

    scripts/seed-demo.py --dry-run                  # print, connect to nothing
    scripts/seed-demo.py --yes-seed-demo-data --undo

Refuses to touch an installation that already holds records it did not create.
See --help.

Requires nothing but Python 3.9+. No pip install, no jq.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import urllib.error
import urllib.parse
import urllib.request
from datetime import date, datetime, timedelta, timezone

# Everything this script creates carries one of these, and nothing else in the
# system does. They are how --undo knows what is its to remove, and how a
# person reading the database in three months can tell demo from real.
# Two shapes, because they go in two kinds of field: an em-dash prefix reads
# as part of a name a person sees, a hyphen one as part of a code.
NAME_PREFIX = "DEMO — "
CODE_PREFIX = "DEMO-"

PROJECT_PREFIX = NAME_PREFIX
MATERIAL_PREFIX = NAME_PREFIX
EMPLOYEE_PREFIX = CODE_PREFIX
ASSET_PREFIX = CODE_PREFIX

# Roughly 07:00 in Belgrade during the summer half of the year. Only used as a
# starting hour for shifts that have already finished; anything touching "now"
# is computed backwards from the clock so the script is correct whenever it is
# run, including on a server in another zone.
WORKDAY_START_UTC = 5

TIMEOUT_SECONDS = 30


# --------------------------------------------------------------------------
# Transport
# --------------------------------------------------------------------------


class ApiError(RuntimeError):
    def __init__(self, method: str, path: str, status: int, body: str):
        self.status = status
        self.body = body
        super().__init__(f"{method} {path} → {status}\n{body}")


class Api:
    """The API, or a transcript of what would have been sent to it."""

    def __init__(self, base_url: str, dry_run: bool = False):
        self.base_url = base_url.rstrip("/")
        self.dry_run = dry_run
        self.token: str | None = None
        self._fake_ids = 0

    def _next_fake_id(self) -> str:
        # Dry runs still have to thread ids from one call into the next, so
        # they are minted here rather than left as None.
        self._fake_ids += 1
        return f"00000000-0000-4000-8000-{self._fake_ids:012d}"

    def request(self, method: str, path: str, body=None, params=None, quiet=False):
        url = self.base_url + path

        if params:
            cleaned = {k: v for k, v in params.items() if v is not None}
            url += "?" + urllib.parse.urlencode(cleaned)

        if self.dry_run:
            if not quiet:
                rendered = json.dumps(body, ensure_ascii=False) if body is not None else ""
                print(f"  {method} {path} {rendered}")

            if method == "GET":
                return {"items": [], "totalCount": 0}

            return {"id": self._next_fake_id()}

        data = None
        headers = {"Accept": "application/json"}

        if body is not None:
            data = json.dumps(body).encode("utf-8")
            headers["Content-Type"] = "application/json"

        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"

        request = urllib.request.Request(url, data=data, headers=headers, method=method)

        try:
            with urllib.request.urlopen(request, timeout=TIMEOUT_SECONDS) as response:
                raw = response.read().decode("utf-8")
                return json.loads(raw) if raw.strip() else None
        except urllib.error.HTTPError as error:
            raise ApiError(method, path, error.code, error.read().decode("utf-8", "replace")) from None
        except urllib.error.URLError as error:
            raise RuntimeError(f"Cannot reach {self.base_url}: {error.reason}") from None

    def get(self, path, params=None, quiet=False):
        return self.request("GET", path, params=params, quiet=quiet)

    def post(self, path, body=None):
        return self.request("POST", path, body=body)

    def delete(self, path):
        return self.request("DELETE", path)

    def sign_in(self, email: str, password: str) -> None:
        if self.dry_run:
            print(f"  POST /api/v1/auth/login {{\"email\": \"{email}\", \"password\": \"…\"}}")
            self.token = "dry-run"
            return

        response = self.request(
            "POST", "/api/v1/auth/login", {"email": email, "password": password}
        )
        self.token = response["accessToken"]


# --------------------------------------------------------------------------
# Credentials
# --------------------------------------------------------------------------


def read_dotenv(path: str = ".env") -> dict:
    """The values compose already uses, if a .env is sitting there."""
    values = {}

    if not os.path.exists(path):
        return values

    with open(path, encoding="utf-8") as handle:
        for line in handle:
            line = line.strip()

            if not line or line.startswith("#") or "=" not in line:
                continue

            key, _, value = line.partition("=")
            values[key.strip()] = value.strip().strip("'\"")

    return values


def credentials() -> tuple:
    """
    Never from the command line.

    An argument lands in the shell's history file and is visible in `ps` to
    every other user on the machine, and this is the account that can do
    anything in the installation.
    """
    env = read_dotenv()

    email = (
        os.environ.get("DEMO_EMAIL")
        or os.environ.get("SUPERADMIN_EMAIL")
        or env.get("SUPERADMIN_EMAIL")
    )
    password = (
        os.environ.get("DEMO_PASSWORD")
        or os.environ.get("SUPERADMIN_PASSWORD")
        or env.get("SUPERADMIN_PASSWORD")
    )

    if not email or not password:
        die(
            "No credentials. Set DEMO_EMAIL and DEMO_PASSWORD, or leave a .env "
            "with SUPERADMIN_EMAIL and SUPERADMIN_PASSWORD in this directory.\n"
            "They are read from the environment on purpose — a password passed "
            "as an argument is kept in your shell history and shown by `ps`."
        )

    return email, password


# --------------------------------------------------------------------------
# The cast
# --------------------------------------------------------------------------

CUSTOMERS = [
    {
        "key": "gradnja",
        "name": NAME_PREFIX + "Gradnja Invest d.o.o.",
        "contactPerson": "Vesna Simić",
        "phone": "+381 11 2345 678",
    },
    {
        "key": "putevi",
        "name": NAME_PREFIX + "JP Putevi",
        "contactPerson": "Bojan Radić",
        "phone": "+381 11 3456 789",
    },
]

SITES = [
    {
        "key": "vidikovac",
        "name": PROJECT_PREFIX + "Vidikovac, stambena zgrada",
        "description": "Stambeni objekat P+4, faza grube gradnje.",
        "customer": "gradnja",
        "address": "Vidikovački venac 74, Beograd",
        "latitude": 44.7534,
        "longitude": 20.4187,
        "contractValue": 48500000,
    },
    {
        "key": "obilaznica",
        "name": PROJECT_PREFIX + "Obilaznica, sektor 3",
        "description": "Potporni zidovi i odvodnja na deonici 3.",
        "customer": "putevi",
        "address": "Obilaznica, deonica 3, Beograd",
        "latitude": 44.8010,
        "longitude": 20.3402,
        "contractValue": 22750000,
    },
]

CREW = [
    ("001", "Marko", "Marković", "Poslovođa", "vidikovac"),
    ("002", "Nenad", "Jovanović", "Zidar", "vidikovac"),
    ("003", "Dragan", "Petrović", "Zidar", "vidikovac"),
    ("004", "Miloš", "Ilić", "Tesar", "vidikovac"),
    ("005", "Ana", "Stanković", "Armirač", "vidikovac"),
    ("006", "Zoran", "Nikolić", "Vozač", "obilaznica"),
    ("007", "Jelena", "Đorđević", "Električar", "obilaznica"),
    ("008", "Stefan", "Lazić", "Pomoćni radnik", "obilaznica"),
]

MATERIALS = [
    (MATERIAL_PREFIX + "Cement PC 42,5", "vreća", 340, "Glavni magacin", 940),
    (MATERIAL_PREFIX + "Armaturna mreža Q188", "tabla", 96, "Glavni magacin", 3150),
    (MATERIAL_PREFIX + "Šljunak 0–4", "m³", 58, "Vidikovac", 2400),
    (MATERIAL_PREFIX + "Blok 20 cm", "kom", 2400, "Vidikovac", 118),
]

VEHICLES = [
    ("Volkswagen", "Crafter", ASSET_PREFIX + "BG-001-AA", "Diesel"),
    ("Iveco", "Daily kiper", ASSET_PREFIX + "BG-002-AA", "Diesel"),
]

TOOLS = [
    (NAME_PREFIX + "Vibrator za beton", "Betonski radovi", CODE_PREFIX + "SN-4401"),
    (NAME_PREFIX + "Ugaona brusilica 230", "Ručni alat", CODE_PREFIX + "SN-4402"),
    (NAME_PREFIX + "Nivelir sa stativom", "Geodezija", CODE_PREFIX + "SN-4403"),
]


# --------------------------------------------------------------------------
# Dates
# --------------------------------------------------------------------------


class Calendar:
    """
    Every date the script writes, derived from the moment it runs.

    Nothing is hard-coded: the whole point is that the day it produces is
    today's, so the same script is useful again tomorrow.
    """

    def __init__(self, now: datetime):
        self.now = now
        self.today = now.date()
        self.monday = self.today - timedelta(days=self.today.weekday())

    @property
    def past_workdays(self) -> list:
        """
        The five most recent working days before today.

        Counted backwards rather than taken from this Monday, because this
        Monday is nothing at all when the script runs on a Monday — and an
        empty history empties every hour total, every cost report and both
        review examples without saying so. At most seven calendar days back,
        comfortably inside the API's 31-day backdating limit.
        """
        days = []
        cursor = self.today - timedelta(days=1)

        while len(days) < 5:
            if cursor.weekday() < 5:
                days.append(cursor)

            cursor -= timedelta(days=1)

        return list(reversed(days))

    def full_shift(self, day: date) -> tuple:
        """An eight-hour day with a half-hour break."""
        start = datetime(day.year, day.month, day.day, WORKDAY_START_UTC, 0, tzinfo=timezone.utc)

        return start, start + timedelta(hours=8, minutes=30)

    def started_this_morning(self) -> datetime:
        """
        The start of a shift that is still running.

        Clamped to two hours ago: the API refuses a start more than a few
        minutes in the future, and a script run before dawn would otherwise
        post a shift that has not begun.
        """
        morning = datetime(
            self.today.year, self.today.month, self.today.day,
            WORKDAY_START_UTC, 0, tzinfo=timezone.utc,
        )

        return min(morning, self.now - timedelta(hours=2))

    def finished_today(self) -> tuple:
        """A shift that ran earlier today and is already closed."""
        end = self.now - timedelta(hours=1)
        start = max(self.started_this_morning(), end - timedelta(hours=8))

        return start, end


def iso(moment: datetime) -> str:
    return moment.astimezone(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def day(value: date) -> str:
    return value.isoformat()


# --------------------------------------------------------------------------
# Guards
# --------------------------------------------------------------------------


def die(message: str) -> "NoReturn":  # noqa: F821
    print(f"\nRefused: {message}\n", file=sys.stderr)
    sys.exit(1)


def survey(api: Api) -> dict:
    """What is already here, split into ours and everyone else's."""
    projects = api.get("/api/v1/projects", {"pageSize": 100}, quiet=True)
    employees = api.get("/api/v1/employees", {"pageSize": 100}, quiet=True)
    customers = api.get("/api/v1/customers", {"pageSize": 100}, quiet=True)

    project_items = projects.get("items", []) if projects else []
    employee_items = employees.get("items", []) if employees else []
    customer_items = customers.get("items", []) if customers else []

    return {
        "demo_customers": [c for c in customer_items if c["name"].startswith(NAME_PREFIX)],
        "demo_projects": [p for p in project_items if p["name"].startswith(PROJECT_PREFIX)],
        "other_projects": [p for p in project_items if not p["name"].startswith(PROJECT_PREFIX)],
        "demo_employees": [
            e for e in employee_items if e["employeeNumber"].startswith(EMPLOYEE_PREFIX)
        ],
        "other_employees": [
            e for e in employee_items if not e["employeeNumber"].startswith(EMPLOYEE_PREFIX)
        ],
    }


def refuse_if_occupied(found: dict, force: bool) -> None:
    """
    Stop if this installation holds records the script did not create.

    Checked by behaviour rather than by hostname. There is no reliable way to
    tell production from staging by its name, and somebody else's data is the
    thing actually worth not writing into — a machine with no records is safe
    to seed whatever it is called.
    """
    strangers = len(found["other_projects"]) + len(found["other_employees"])

    if strangers and not force:
        die(
            f"this installation already holds {len(found['other_projects'])} project(s) and "
            f"{len(found['other_employees'])} employee(s) that this script did not create.\n"
            "That looks like somebody's real data, and demo records do not come "
            "back out of a database cleanly.\n"
            "If you are certain, re-run with --force."
        )


# --------------------------------------------------------------------------
# Seeding
# --------------------------------------------------------------------------


def ensure_customers(api: Api, existing: list) -> dict:
    """
    The projects board groups by customer, so a site with none sits in a
    nameless pile and the screen reads as broken rather than empty.
    """
    by_name = {c["name"]: c for c in existing}
    customers = {}

    for customer in CUSTOMERS:
        found = by_name.get(customer["name"])

        if found:
            customers[customer["key"]] = found["id"]
            continue

        created = api.post("/api/v1/customers", {
            "name": customer["name"],
            "contactPerson": customer["contactPerson"],
            "phone": customer["phone"],
        })

        customers[customer["key"]] = created["id"]

    return customers


def ensure_sites(api: Api, existing: list, customers: dict, calendar: Calendar) -> dict:
    by_name = {p["name"]: p for p in existing}
    sites = {}

    for site in SITES:
        found = by_name.get(site["name"])

        if found:
            sites[site["key"]] = found["id"]
            continue

        created = api.post("/api/v1/projects", {
            "name": site["name"],
            "description": site["description"],
            "customerId": customers[site["customer"]],
            "address": site["address"],
            "latitude": site["latitude"],
            "longitude": site["longitude"],
            "startDate": day(calendar.monday - timedelta(days=60)),
            "status": "Active",
            "contractValue": site["contractValue"],
        })

        sites[site["key"]] = created["id"]

    return sites


def ensure_crew(api: Api, existing: list, sites: dict, calendar: Calendar) -> list:
    by_number = {e["employeeNumber"]: e for e in existing}
    crew = []

    for number, first, last, position, site_key in CREW:
        employee_number = EMPLOYEE_PREFIX + number
        found = by_number.get(employee_number)

        if found:
            crew.append({
                "id": found["id"],
                "name": f"{first} {last}",
                "position": position,
                "site": sites[site_key],
                "new": False,
            })
            continue

        created = api.post("/api/v1/employees", {
            "employeeNumber": employee_number,
            "firstName": first,
            "lastName": last,
            "position": position,
            "employmentDate": day(calendar.monday - timedelta(days=365)),
            "status": "Active",
            "type": "Employee",
        })

        crew.append({
            "id": created["id"],
            "name": f"{first} {last}",
            "position": position,
            "site": sites[site_key],
            "new": True,
        })

    return crew


def post_crew_to_sites(api: Api, crew: list, calendar: Calendar) -> None:
    for person in crew:
        if not person["new"]:
            # A posting already exists and is open-ended; assigning again
            # collides with the database's own overlap constraint.
            continue

        api.post(
            f"/api/v1/employees/{person['id']}/projects/{person['site']}",
            {
                "employeeId": person["id"],
                "projectId": person["site"],
                "startDate": day(calendar.monday),
            },
        )


def already_has_a_shift_today(api: Api, employee_id: str, calendar: Calendar) -> bool:
    """So a second run on the same day adds nothing rather than doubling it."""
    entries = api.get("/api/v1/timeentries", {
        "employeeId": employee_id,
        "from": iso(datetime(
            calendar.today.year, calendar.today.month, calendar.today.day, tzinfo=timezone.utc
        )),
        "pageSize": 1,
    }, quiet=True)

    return bool(entries and entries.get("totalCount", 0) > 0)


def seed_history(api: Api, crew: list, calendar: Calendar) -> None:
    """
    Monday to yesterday, mostly signed off.

    The mixture matters more than the volume. All-approved leaves the review
    screen empty and it reads as broken; nothing approved leaves every cost
    report at zero, because only approved hours are a cost.
    """
    workdays = calendar.past_workdays

    if not workdays:
        print("  (it is Monday — no history to write yet)")
        return

    for index, person in enumerate(crew):
        for offset, workday in enumerate(workdays):
            start, end = calendar.full_shift(workday)

            entry = api.post("/api/v1/timeentries", {
                "employeeId": person["id"],
                "projectId": person["site"],
                "startedAt": iso(start),
                "endedAt": iso(end),
                "breakMinutes": 30,
                "workType": "Regular",
            })

            # One person's newest day is left waiting, and one is sent back,
            # so both halves of the review screen have something in them.
            last_day = offset == len(workdays) - 1

            if last_day and index == 0:
                continue

            if last_day and index == 1:
                api.post(f"/api/v1/timeentries/{entry['id']}/review", {
                    "approve": False,
                    "note": "Pauza nije evidentirana — dopuniti i poslati ponovo.",
                })
                continue

            api.post(f"/api/v1/timeentries/{entry['id']}/review", {"approve": True})


def seed_today(api: Api, crew: list, calendar: Calendar) -> tuple:
    """
    Three people still on site, two already finished.

    Returns what it actually wrote rather than what it intended to, so the
    closing summary describes the installation instead of the plan.
    """
    running = 0
    finished = 0

    for person in crew[:3]:
        if already_has_a_shift_today(api, person["id"], calendar):
            print(f"  {person['name']} already has a shift today — left alone")
            continue

        api.post("/api/v1/timeentries", {
            "employeeId": person["id"],
            "projectId": person["site"],
            "startedAt": iso(calendar.started_this_morning()),
            "breakMinutes": 0,
            "workType": "Regular",
        })
        running += 1

    start, end = calendar.finished_today()

    if (end - start) < timedelta(hours=4):
        # Run early in the morning, "already finished today" would be a
        # forty-minute shift with a half-hour break in it. Better absent than
        # absurd — the running shifts above still carry the day.
        print("  (too early in the day for a finished shift — skipped)")
        return running, finished

    for person in crew[3:5]:
        if already_has_a_shift_today(api, person["id"], calendar):
            print(f"  {person['name']} already has a shift today — left alone")
            continue

        api.post("/api/v1/timeentries", {
            "employeeId": person["id"],
            "projectId": person["site"],
            "startedAt": iso(start),
            "endedAt": iso(end),
            "breakMinutes": 30,
            "workType": "Regular",
        })
        finished += 1

    return running, finished


def seed_work_items(api: Api, crew: list, sites: dict, calendar: Calendar) -> None:
    items = [
        ("Task", "Zatvoriti oplatu na trećoj ploči", "vidikovac", 1, "High", calendar.today),
        ("Task", "Naručiti armaturu za sledeću fazu", "vidikovac", 0, "Urgent",
         calendar.today - timedelta(days=2)),
        ("Defect", "Prslina na potpornom zidu, zapadna strana", "obilaznica", 5, "High", None),
        ("Task", "Postaviti privremenu rasvetu u podrumu", "vidikovac", 6, "Normal",
         calendar.today + timedelta(days=3)),
        ("Task", "Očistiti pristupni put", "obilaznica", 7, "Low",
         calendar.today + timedelta(days=1)),
    ]

    for kind, title, site_key, assignee, priority, due in items:
        api.post("/api/v1/workitems", {
            "kind": kind,
            "title": title,
            "projectId": sites[site_key],
            "assignedEmployeeId": crew[assignee]["id"],
            "priority": priority,
            "dueDate": day(due) if due else None,
            "requiresAcknowledgment": False,
        })


def seed_absence(api: Api, crew: list, calendar: Calendar) -> None:
    """One approved day off covering today, so the board is not all present."""
    api.post("/api/v1/absences", {
        "employeeId": crew[7]["id"],
        "type": "AnnualLeave",
        "startDate": day(calendar.today),
        "endDate": day(calendar.today + timedelta(days=2)),
        "reason": "Godišnji odmor",
        "approve": True,
    })


def seed_stock(api: Api, sites: dict) -> None:
    for name, unit, quantity, warehouse, unit_price in MATERIALS:
        material = api.post("/api/v1/materials", {
            "name": name,
            "unit": unit,
            "quantity": quantity,
            "warehouse": warehouse,
            "unitPrice": unit_price,
        })

        if name.endswith("Cement PC 42,5"):
            # One movement today, or the stock screen shows a balance with no
            # history behind it — which is the screen's whole point.
            api.post(f"/api/v1/materials/{material['id']}/adjust", {
                "change": -40,
                "reason": "Izdato na gradilište Vidikovac",
            })


def seed_assets(api: Api) -> None:
    for brand, model, registration, fuel in VEHICLES:
        api.post("/api/v1/vehicles", {
            "brand": brand,
            "model": model,
            "registrationNumber": registration,
            "fuelType": fuel,
            "status": "Available",
            "ownershipType": "Owned",
        })

    for name, category, serial in TOOLS:
        api.post("/api/v1/tools", {
            "name": name,
            "category": category,
            "serialNumber": serial,
            "status": "Available",
            "ownershipType": "Owned",
        })


# --------------------------------------------------------------------------
# Undo
# --------------------------------------------------------------------------


def undo(api: Api) -> None:
    """
    Removes what carries the marker, and nothing else.

    Children before parents: a person's shifts and tasks go before the person,
    so nothing is left pointing at a record that is gone.
    """
    found = survey(api)

    for person in found["demo_employees"]:
        entries = api.get(
            "/api/v1/timeentries", {"employeeId": person["id"], "pageSize": 100}, quiet=True
        )

        for entry in (entries or {}).get("items", []):
            try:
                api.delete(f"/api/v1/timeentries/{entry['id']}")
            except ApiError as error:
                # An approved entry is locked, by design — it is somebody's
                # pay. Say so and carry on rather than stopping half-done.
                if error.status == 409:
                    print(f"  approved shift {entry['id']} left in place (locked)")
                else:
                    raise

        absences = api.get(
            "/api/v1/absences", {"employeeId": person["id"], "pageSize": 100}, quiet=True
        )

        for absence in (absences or {}).get("items", []):
            api.delete(f"/api/v1/absences/{absence['id']}")

    for project in found["demo_projects"]:
        items = api.get(
            "/api/v1/workitems", {"projectId": project["id"], "pageSize": 100}, quiet=True
        )

        for item in (items or {}).get("items", []):
            api.delete(f"/api/v1/workitems/{item['id']}")

    for person in found["demo_employees"]:
        api.delete(f"/api/v1/employees/{person['id']}")

    for project in found["demo_projects"]:
        api.delete(f"/api/v1/projects/{project['id']}")

    # After the projects, which point at them.
    for customer in found["demo_customers"]:
        api.delete(f"/api/v1/customers/{customer['id']}")

    for resource, prefix, field in (
        ("materials", MATERIAL_PREFIX, "name"),
        ("vehicles", CODE_PREFIX, "registrationNumber"),
        ("tools", NAME_PREFIX, "name"),
    ):
        listing = api.get(f"/api/v1/{resource}", {"pageSize": 100}, quiet=True)

        for row in (listing or {}).get("items", []):
            if str(row.get(field, "")).startswith(prefix):
                api.delete(f"/api/v1/{resource}/{row['id']}")

    print("\nDemo records removed.")


# --------------------------------------------------------------------------
# Entry point
# --------------------------------------------------------------------------


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Fill an installation with a believable working day, dated today.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--api-base-url",
        default=os.environ.get("API_BASE_URL"),
        help="e.g. https://your-server. No default: a default is how the wrong machine gets seeded.",
    )
    parser.add_argument(
        "--yes-seed-demo-data",
        action="store_true",
        help="Required. Writes fabricated records into whatever this points at.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print every request that would be sent. Opens no connection.",
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Proceed even though the installation holds records this script did not create.",
    )
    parser.add_argument("--undo", action="store_true", help="Remove everything it created.")

    args = parser.parse_args()

    if not args.api_base_url:
        die("no target. Set API_BASE_URL or pass --api-base-url.")

    if not args.yes_seed_demo_data and not args.dry_run:
        die(
            "--yes-seed-demo-data was not given.\n"
            f"This would write fabricated employees, shifts and hours into {args.api_base_url}."
        )

    api = Api(args.api_base_url, dry_run=args.dry_run)
    calendar = Calendar(datetime.now(timezone.utc))

    print(f"Target:  {args.api_base_url}")
    print(f"Day:     {calendar.today.isoformat()}" + (" (dry run)" if args.dry_run else ""))

    email, password = ("dry-run@local", "") if args.dry_run else credentials()
    print(f"Account: {email}\n")

    api.sign_in(email, password)

    if args.undo:
        undo(api)
        return

    found = survey(api)
    refuse_if_occupied(found, args.force)

    print("Customers, sites and crew")
    customers = ensure_customers(api, found["demo_customers"])
    sites = ensure_sites(api, found["demo_projects"], customers, calendar)
    crew = ensure_crew(api, found["demo_employees"], sites, calendar)
    post_crew_to_sites(api, crew, calendar)

    fresh = any(person["new"] for person in crew)

    print("Hours — the last five working days")
    if fresh:
        seed_history(api, crew, calendar)
    else:
        print("  (crew already existed — history left as it is)")

    print("Today")
    running, finished = seed_today(api, crew, calendar)

    if fresh:
        print("Tasks, leave, stock and equipment")
        seed_work_items(api, crew, sites, calendar)
        seed_absence(api, crew, calendar)
        seed_stock(api, sites)
        seed_assets(api)

    # Reported from what was written, not from what the script set out to do.
    # A summary that always claims the same thing is worth nothing on the run
    # where half of it was skipped.
    print(f"\nDone. {running} shift(s) running now, {finished} finished earlier today.")

    if fresh:
        print(
            "Five working days of history are in, mostly approved — one entry "
            "is waiting for review and one was sent back with a reason."
        )
    else:
        print("The crew was already here, so history and the rest were left untouched.")

    print(
        "\nThe live map stays empty. Positions can only be reported by the "
        "person they belong to, so filling it would mean creating a sign-in for "
        "every demo worker — see the note in the plan."
    )


if __name__ == "__main__":
    try:
        main()
    except ApiError as error:
        die(str(error))
    except RuntimeError as error:
        die(str(error))
    except KeyboardInterrupt:
        sys.exit(130)
