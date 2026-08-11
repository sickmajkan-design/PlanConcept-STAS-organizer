# Privatnost i lični podaci / Privacy and personal data

**SR** — Ovaj dokument opisuje **šta sistem zaista čuva**, izvedeno iz šeme
baze, a ne iz šablona. Sadrži i ono što mora da odluči vlasnik sistema uz
pravnika — ti delovi su jasno označeni. Ovo **nije pravni savet**.

**EN** — This document describes **what the system actually holds**, derived
from the database schema rather than from a template. It also marks what the
owner must decide with a lawyer — those parts are labelled. This is **not legal
advice**.

> **SR** — Odeljci označeni sa **[ZA VLASNIKA]** ne mogu biti popunjeni iz koda.
> Traže odluku o pravnom osnovu i tekst koji ide radnicima.
> **EN** — Sections marked **[FOR THE OWNER]** cannot be filled in from code.
> They need a decision on lawful basis and text that goes to the workforce.

---

## 1. Šta se čuva o ljudima / What is held about people

Izvedeno iz šeme (21 tabela). Navedeno je samo ono što se odnosi na osobu. /
Derived from the schema (21 tables). Only person-related data is listed.

| Tabela / Table | Lični podaci / Personal data | Zašto / Why | Koliko / For how long |
|---|---|---|---|
| `employees` | Ime, prezime, telefon, e-pošta, adresa, datum rođenja, radno mesto, datum zaposlenja | Kadrovska evidencija / Employment record | Trajno, do brisanja (§4) / Indefinitely, until erasure |
| `users` | E-pošta, hash lozinke, uloga, poslednja prijava, neuspeli pokušaji | Pristup sistemu / System access | Dok nalog postoji / While the account exists |
| `location_records` | **Geografska širina/dužina, tačnost, vreme — svakog minuta** | Mapa uživo, dokaz prisustva / Live map, presence | **180 dana** (podesivo) / **180 days** (configurable) |
| `time_entries` | Sati, projekat, **koordinate prijave i odjave smene** | Obračun zarade / Payroll | Sati trajno; koordinate uz smenu, osim ako se ne podesi rok (§3.1) / Hours indefinitely; coordinates with the shift unless bounded |
| `absences` | Tip odsustva, datumi, **razlog (slobodan tekst)**, napomena odobravaoca | Evidencija odsustva / Absence record | Trajno / Indefinitely |
| `employee_rates` | Satnica, period važenja | Obračun troška / Costing | Trajno / Indefinitely |
| `audit_entries` | E-pošta i uloga onoga ko je menjao, **IP adresa**, stare i nove vrednosti polja | Dokazivanje usklađenosti / Demonstrating compliance | Trajno (podrazumevano) / Indefinitely (default) |
| `refresh_tokens` | **IP adresa** pri izdavanju i opozivu | Otkrivanje krađe sesije / Session-theft detection | 30 dana posle isteka / 30 days past expiry |
| `device_tokens` | Token uređaja | Push obaveštenja / Push | Dok se uređaj ne odjavi / Until the device is unregistered |
| `notifications` | Naslov i telo poruke po korisniku | Obaveštenja / Notifications | Trajno / Indefinitely |
| `attachments` | Dokumenti vezani za radnika (ugovori, lekarska uverenja, dozvole) | Kadrovska dokumentacija / HR documents | Trajno / Indefinitely |
| `employee_projects`, `work_items` | Gde je ko radio i šta je radio / Who worked where and on what | Organizacija posla / Work organisation | Trajno / Indefinitely |

### 1.1 Dve stavke koje traže posebnu pažnju / Two items needing particular care

**SR** — Obe su nađene čitanjem šeme, ne pretpostavkom:

**EN** — Both were found by reading the schema, not assumed:

1. **`absences.Reason` je slobodan tekst na zapisu koji može biti bolovanje.**
   U praksi tu završi dijagnoza. To je podatak o zdravlju — posebna kategorija
   po GDPR članu 9, sa strožim uslovima od ostatka ove tabele. Sistem ga ne
   traži i ne mora da postoji; razmotrite da se polje ne popunjava, ili da se
   ograniči pristup. Brisanje (§4) ga uklanja. /
   **`absences.Reason` is free text on a record that may be sick leave.** In
   practice a diagnosis ends up there. That is health data — special category
   under GDPR Article 9, with a higher bar than the rest of this table. The
   system does not require it; consider leaving it empty or restricting who
   reads it. Erasure (§4) removes it.

2. **Koordinate prijave i odjave smene nadživljavaju GPS rok.** To je bila
   namerna odluka — odobren radni list je dokaz za zaradu, a gde je smena
   počela je deo tog dokaza. Ali to znači da tvrdnja „lokacije se čuvaju 180
   dana" nije potpuna: dve koordinate po smeni ostaju uz smenu. Podesivo od
   sada — vidi §3.1. /
   **Clock-in and clock-out coordinates outlive the GPS window.** That was a
   deliberate decision — an approved timesheet is payroll evidence and where the
   shift started is part of it. But it means "location is kept for 180 days" is
   incomplete: two coordinates per shift stay with the shift. Configurable as of
   now — see §3.1.

---

### 1.2 Kopija na telefonu / The copy on the phone

**SR** — Mobilna aplikacija od sada čuva poslednji uspešan odgovor na svaki
ekran, da bi radila i tamo gde nema signala. To znači da lični podaci — imena,
pozicije, rasporedi, poslednje poznate lokacije — postoje i na uređaju, ne samo
na serveru. Pravila su:

**EN** — The mobile app now keeps the last successful answer to each screen so
that it works where there is no signal. That means personal data — names,
positions, schedules, last known locations — exists on the handset as well as on
the server. The rules are:

| Pitanje / Question | Odgovor / Answer |
|---|---|
| Gde / Where | Privatni direktorijum aplikacije (`ApplicationSupport/api_cache`). Nedostupan drugim aplikacijama na neizmenjenom uređaju. / The app's private directory. Not readable by other apps on an unmodified device. |
| Koliko dugo / For how long | Najviše 7 dana; starije se briše pri prvom čitanju, ne prikazuje se. / At most 7 days; older entries are deleted on read rather than shown. |
| Koliko / How much | Najviše 200 odgovora, najviše 512 KB po odgovoru. / At most 200 answers, at most 512 KB each. |
| Kad se briše / When it is cleared | Pri svakoj promeni korisnika: odjava, istek sesije, prijava drugog naloga. / On every change of user: sign-out, session expiry, a different account signing in. |
| Rezervne kopije / Device backup | `android:allowBackup="false"` — Android ne kopira ovo na Google nalog. / Android does not copy this into the user's Google account. |
| Šta se ne čuva / What is never kept | Tokeni i `/auth/*` odgovori, sadržaj priloga (fotografije), i sve što nije GET. / Tokens and `/auth/*` responses, attachment content (photographs), and anything that is not a GET. |

**SR** — Korisniku se uvek kaže kada gleda kopiju, i od kada je: traka na vrhu
ekrana piše vreme kada je odgovor sačuvan. Kopija se koristi **samo** kad
zahtev ne stigne do servera. Odbijanje sa servera (403, 404, 500) se nikad ne
zaobilazi kopijom — oduzeta dozvola ostaje oduzeta. /
**EN** — The user is always told when they are looking at a copy and how old it
is: a strip at the top of the screen carries the time it was saved. A copy is
served **only** when the request never reached the server. A refusal from the
server (403, 404, 500) is never worked around from cache — a permission that has
been withdrawn stays withdrawn.

**SR** — Brisanje po §4 briše podatke na serveru. Kopija na telefonu nestaje
sama, najkasnije za 7 dana, a odmah po odjavi tog korisnika. Ako je potrebno
brisanje istog trenutka, odjavite uređaj. /
**EN** — Erasure under §4 removes the data on the server. The copy on a handset
expires by itself within 7 days, and immediately when that user signs out. If
immediate removal is required, sign the device out.

### 1.3 Prijave grešaka iz aplikacija / Crash reports from the clients

**SR** — Kad se admin panel ili mobilna aplikacija sruše, šalju izveštaj na
`POST /api/v1/client-errors`: poruka greške, tip, stack trace, ekran na kome se
desilo, verzija aplikacije i opis uređaja. Izveštaj **ne sadrži** ime, e-mail
ni identifikator korisnika — šalje se bez prijave, jer je najvažniji izveštaj
onaj sa ekrana za prijavu koji neće da se učita.

Ipak, ovo jesu lični podaci u jednom smislu koji treba reći naglas: putanja
ekrana može sadržati identifikator zaposlenog (`/employees/<id>`), a stack
trace može uhvatiti vrednost koja je bila u obradi. Izveštaji idu u isti log
kao i sve ostalo — na vaš server, pod vašim rokom čuvanja — i **ne šalju se
nijednom trećem servisu.** To je bila namerna odluka: gde odlaze podaci o
kretanju i radu vaših zaposlenih nije stvar podrazumevane vrednosti neke
biblioteke.

Ako log ide u agregator, on nasleđuje ovo isto — pa rok čuvanja logova treba
da bude odluka, a ne slučajnost. /

**EN** — When the admin panel or the mobile app crashes, it posts a report to
`POST /api/v1/client-errors`: the error message, its type, the stack trace, the
screen it happened on, the app version and a device description. The report
carries **no** name, e-mail or user id — it is sent unauthenticated, because
the report worth having most is from a sign-in screen that will not load.

It is still personal data in one respect worth saying plainly: a route can
contain an employee id (`/employees/<id>`), and a stack trace can capture a
value that was being handled. Reports go into the same log as everything else —
on your server, under your retention — and are **sent to no third-party
service.** That was a deliberate decision: where data about your employees'
movements and work ends up is not a library's default to make.

If the log is shipped to an aggregator, this goes with it — so log retention
should be a decision rather than an accident.

---

## 2. Ko šta vidi / Who sees what

| Uloga / Role | Lokacije / Location |
|---|---|
| Radnik / Worker | Samo sebe / Only themselves |
| Poslovođa / Foreman | **Samo ekipe na svojim aktuelnim gradilištima** / Only the crews on their own current projects |
| Šef projekta i naviše / Project manager and above | Sve / Everyone |

**SR** — Ograničenje za poslovođu je uvedeno kao stavka H11 i pokriveno je
testovima. Radnik van opsega vraća 404, ne 403 — „postoji ali nije tvoj"
potvrđuje da osoba postoji. /
**EN** — The foreman restriction was audit item H11 and is covered by tests. An
out-of-scope employee answers 404 rather than 403: "exists but not yours"
confirms the person exists.

Satnice vidi samo šef projekta i naviše (`CostRules`). / Pay rates are visible
to project manager and above only (`CostRules`).

---

## 3. Rokovi čuvanja / Retention

Podešava se promenljivama okruženja. / Set by environment variable.

| Promenljiva / Variable | Podrazumevano / Default | Šta radi / Effect |
|---|---|---|
| `Retention__LocationRecordDays` | `180` | GPS fiksevi stariji od toga se brišu. `0` = čuvaj sve, uz upozorenje pri pokretanju. / GPS fixes older than this are deleted. `0` = keep everything, with a startup warning. |
| `Retention__TimeEntryCoordinateDays` | `0` (čuvaj uz smenu / keep with the shift) | Briše koordinate smene starije od roka; sati ostaju. / Clears shift coordinates past the window; the hours stay. |
| `Retention__AuditEntryDays` | `0` (čuvaj sve / keep everything) | Jedini rok koji podrazumevano čuva — vidi §5. / The one default that keeps — see §5. |
| `Retention__RefreshTokenGraceDays` | `30` | Posle isteka tokena. / Past the token's own expiry. |
| `Retention__SentOutboxMessageDays` | `14` | Isporučene poruke. / Delivered messages. |

Čisti `DataRetentionService`, na svakih 6 sati, u ograničenim serijama. /
Swept by `DataRetentionService` every six hours in bounded batches.

### 3.1 Preporuka / Recommendation

**SR** — Ako nemate obavezu da čuvate duže, `Retention__LocationRecordDays`
niže od 180 je lakše braniti. Za koordinate smene: postavite
`Retention__TimeEntryCoordinateDays` na period posle kojeg zarada više ne može
biti osporena u vašoj jurisdikciji. /
**EN** — Unless you are obliged to keep longer, a `LocationRecordDays` below 180
is easier to defend. For shift coordinates, set
`TimeEntryCoordinateDays` to the period after which a wage can no longer be
disputed in your jurisdiction.

---

## 4. Brisanje ličnih podataka / Erasing personal data

```
POST /api/v1/privacy/employees/{employeeId}/erase
{ "reason": "Zahtev lica, ref DSR-2026-014" }
```

Samo Super Admin. Nepovratno. Razlog je obavezan i upisuje se u audit trag. /
Super Admin only. Irreversible. The reason is required and recorded in the audit
trail.

| Briše se / Removed | Ostaje / Kept |
|---|---|
| Ceo GPS trag / The whole GPS track | Sati, projekat, status smene / Hours, project, shift status |
| Koordinate prijave/odjave / Clock-in and clock-out coordinates | Broj radnika, datum zaposlenja, radno mesto / Employee number, employment date, position |
| Razlog odsustva i napomena / Absence reason and review note | Tip i datumi odsustva / Absence type and dates |
| Telefon, e-pošta, adresa, datum rođenja / Phone, email, address, date of birth | Satnice / Pay rates |
| Obaveštenja, tokeni uređaja, sesije / Notifications, device tokens, sessions | Audit trag / The audit trail (§5) |
| Ime → `Erased`, prezime → broj radnika / Name redacted to the employee number | |

**SR** — Zašto ne „obriši sve": poslodavac je dužan da čuva evidenciju o radu i
zaradi godinama posle odlaska radnika. Komanda koja bi to obrisala zamenila bi
problem privatnosti problemom knjigovodstva. Rezultat je radni list koji se i
dalje sabira, a više ne govori ko, gde, ni zašto je bio na bolovanju. /
**EN** — Why not "delete everything": an employer must retain work and pay
records for years after somebody leaves. A command that removed those would
trade a privacy failure for a bookkeeping one. The result is a timesheet that
still adds up and no longer says who, where, or why they were off sick.

**Prilozi se ne brišu automatski.** Bajtovi su u objektnom skladištu ili na
disku; brisanje reda u bazi bi ostavilo fajl bez traga o njemu. Odgovor
komande vraća njihov broj — obrišite ih zasebno. /
**Attachments are not deleted automatically.** The bytes live in object storage
or on disk; deleting the database row would orphan the file. The response
returns their count — remove them separately.

---

## 5. Audit trag i brisanje / The audit trail and erasure — **[ZA VLASNIKA / FOR THE OWNER]**

**SR** — Brisanje **ne dira** audit trag. Zapisi beleže ko je šta menjao,
uključujući izmene koje je ta osoba napravila kao korisnik; njihovo čišćenje
uništilo bi integritet traga za sve ostale. Zauzeti stav je da se trag čuva
radi dokazivanja usklađenosti, što je i samo po sebi pravni osnov.

**To je odluka za pravnika, ne za programera.** Ako se proceni drugačije, u
pitanju je izmena koda, ne podešavanje. Test
`ErasureTests.The_audit_trail_is_left_intact` je mesto koje će pući i pokazati
na ovu odluku.

**EN** — Erasure **does not touch** the audit trail. Entries record who changed
what, including changes the person made as a user, and scrubbing them would
destroy the trail's integrity for everybody else. The position taken is that the
trail is retained to demonstrate compliance, which is itself a lawful basis.

**That is a decision for a lawyer, not an engineer.** If it is judged otherwise,
it is a code change rather than a setting. The test
`ErasureTests.The_audit_trail_is_left_intact` is what fails and points at it.

---

## 6. Šta mora da odluči vlasnik / What the owner must decide — **[ZA VLASNIKA / FOR THE OWNER]**

**SR** — Kod ovo ne može da popuni. Bez ovoga sistem tehnički radi, ali praćenje
lokacije zaposlenih nema osnov.

**EN** — Code cannot fill these in. Without them the system runs, but tracking
employees' location has no basis.

1. **Pravni osnov za praćenje lokacije.** Saglasnost radnika je slab osnov u
   radnom odnosu — odnos nije ravnopravan, pa se saglasnost teško smatra
   slobodnom. Legitimni interes je uobičajeniji, ali traži zabeležen test
   odmeravanja. / **Lawful basis for location tracking.** Consent is weak in an
   employment relationship — the parties are not equals, so it is hard to call
   it freely given. Legitimate interest is the usual route, but it needs a
   documented balancing test.
2. **Obaveštenje radnicima**, na srpskom: šta se beleži, kada (samo tokom radnog
   vremena?), koliko dugo, ko vidi, i kako da traže brisanje. / **A notice to
   the workforce** covering what is recorded, when, for how long, who sees it,
   and how to request erasure.
3. **DPIA.** Sistematsko praćenje zaposlenih ga po pravilu traži. Tehničke
   ulaze — šta, gde, koliko, ko — daje odeljak §1 ovog dokumenta. /
   **A DPIA.** Systematic monitoring of employees normally requires one. The
   technical inputs — what, where, how long, who — are in §1 above.
4. **Da li se prati van radnog vremena.** Sistem trenutno ne zna za radno vreme:
   mobilna aplikacija šalje fikseve dok je praćenje uključeno. Ako se ne sme
   pratiti van smene, to je izmena u aplikaciji, ne podešavanje. /
   **Whether tracking runs outside working hours.** The system has no concept of
   a shift window: the mobile app reports while tracking is on. If tracking
   outside a shift is not permitted, that is an app change rather than a
   setting.
5. **Ugovori sa obrađivačima** za Firebase (push) i objektno skladište
   (prilozi). / **Processor agreements** for Firebase (push) and object storage
   (attachments).

---

## 7. Šta je urađeno, a šta nije / Done and not done

| Stavka / Item | Stanje / State |
|---|---|
| Popis podataka izveden iz šeme / Data inventory derived from the schema | **urađeno / done** (§1) |
| Rokovi čuvanja, podesivi i primenjeni / Retention, configurable and enforced | **urađeno / done** (§3) |
| Ograničenje pristupa lokacijama po ulozi / Role-scoped access to location | **urađeno / done** (§2) |
| Put za brisanje, sa testovima / Erasure path, with tests | **urađeno / done** (§4) |
| Audit trag ko je šta menjao / Audit trail of who changed what | **urađeno / done** |
| Kopija podataka na telefonu, ograničena i objavljena / Device-side copy, bounded and documented | **urađeno / done** (§1.2) |
| Pravni osnov, obaveštenje, DPIA / Lawful basis, notice, DPIA | **nije — traži vlasnika i pravnika / not done — needs the owner and a lawyer** (§6) |
| Izvoz podataka na zahtev lica / Data export on a subject request | **nije / not done** — trenutno se radi ručno iz baze / currently a manual database query |
| Ograničenje praćenja na radno vreme / Tracking limited to working hours | **nije / not done** (§6.4) |
