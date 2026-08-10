# Provisioning — koraci koje mora uraditi vlasnik / Owner-only setup steps

**SR** — Ovo su koraci koje kod ne može da uradi umesto vas, jer traže naloge
i tajne koje pripadaju vašoj organizaciji. Sve ostalo u Fazi 0 je odrađeno i
čeka samo ove vrednosti.

**EN** — These are the steps code cannot do for you: they need accounts and
secrets that belong to your organisation. Everything else in Phase 0 is done
and waiting on these values.

---

## 1. Firebase / Push notifikacije

Bez ovoga aplikacija radi, ali nijedna push poruka ne izlazi — mobilna
prijavljuje `unconfigured`, a API loguje umesto da šalje.
Without this the app runs, but no push is delivered — the mobile app reports
`unconfigured`, and the API logs instead of sending.

### 1.1 Napravi projekat / Create the project

1. https://console.firebase.google.com → **Add project**
2. Google Analytics nije potreban. / Google Analytics is not required.

### 1.2 Android klijent / Android client

1. **Add app → Android**
2. Package name mora biti tačno / must be exactly:
   `com.planconcept.construction_mobile`
3. Preuzmi `google-services.json` i stavi ga u / Download `google-services.json` into:
   `src/construction_mobile/android/app/google-services.json`

Fajl je git-ignorisan. Build ga sam detektuje: ako postoji, primenjuje se
Google Services Gradle plugin; ako ne postoji, build i dalje prolazi i ispisuje
upozorenje. / The file is git-ignored. The build detects it: present means the
Google Services Gradle plugin is applied, absent means the build still succeeds
with a warning.

### 1.3 iOS klijent / iOS client

1. **Add app → iOS**, bundle ID iz Xcode projekta / bundle ID from the Xcode project
2. `GoogleService-Info.plist` → `src/construction_mobile/ios/Runner/`
3. APNs ključ (`.p8`) iz Apple Developer naloga → Firebase → **Cloud Messaging →
   APNs Authentication Key**. Bez ovoga iOS push ne radi ni sa ispravnim plist-om.
   / Without the APNs key iOS push does not work even with a correct plist.

### 1.4 Server / API

1. Firebase → **Project settings → Service accounts → Generate new private key**
2. Spljošti u jedan red / flatten to one line: `jq -c . service-account.json`
3. Upiši u `.env` / put it in `.env`:

   ```
   FIREBASE_CREDENTIALS_JSON={"type":"service_account",...}
   ```

Ovaj ključ može da pošalje push svakom instaliranom uređaju — u produkciji ide
u secret manager, nikad u fajl koji se komituje. / This key can push to every
installed device — in production it belongs in a secret manager, never in a
committed file.

### 1.5 Provera / Verify

```
docker compose up -d
# prijavi se na telefonu, pa iz admin panela pošalji obaveštenje
# sign in on the phone, then send a notification from the admin panel
```

Ako i dalje ne stiže, pogledaj log API-ja za
`Firebase is not configured` — to znači da `FIREBASE_CREDENTIALS_JSON` nije
stigao do kontejnera. / If nothing arrives, check the API log for
`Firebase is not configured` — that means the variable never reached the
container.

---

## 2. Potpisivanje Android builda / Android release signing

### 2.1 Napravi keystore — jednom, zauvek / Create the keystore — once, for good

```
keytool -genkey -v -keystore ~/stas-organizer-upload.jks \
  -keyalg RSA -keysize 2048 -validity 10000 -alias upload
```

> **Ako izgubite ovaj keystore, aplikacija na Play-u se više nikada ne može
> ažurirati.** Google ne prihvata build potpisan drugim ključem; jedini izlaz
> je novi listing i molba svim korisnicima da instaliraju ispočetka. Ide u
> password manager organizacije, ne na jedan laptop.
>
> **Lose this keystore and the app on Play can never be updated again.** Google
> will not accept a rebuild signed with a different key; the only remedy is a
> new listing and asking every user to reinstall. It belongs in the
> organisation's password manager, not on one laptop.

### 2.2 Poveži ga / Wire it up

```
cd src/construction_mobile
cp android/key.properties.example android/key.properties
# popuni storeFile, storePassword, keyAlias, keyPassword
```

I `key.properties` i `.jks` su git-ignorisani. / Both are git-ignored.

### 2.3 Build

```
flutter build appbundle --release
```

Bez `key.properties` build i dalje prolazi, ali je potpisan debug ključem —
Play će odbiti upload. To je namerno: greška se javlja pri slanju, a ne tiho na
uređajima korisnika. / Without `key.properties` the build still succeeds but is
debug-signed — Play rejects the upload. That is deliberate: the failure happens
at submission, not silently on users' devices.

---

## 3. Šta i dalje ne radi posle ovoga / What still does not work after this

Pošteno, da ne bude iznenađenja. / Stated plainly so there are no surprises.

| Stavka / Item | Stanje / State |
|---|---|
| GPS dok je aplikacija u pozadini ili ekran ugašen | **radi** / works |
| GPS kad korisnik ukloni aplikaciju iz „recents" | **ne radi** — Android uništava aktivnost, a sa njom i servis. Fiksevi u redu čekanja se čuvaju i šalju pri sledećem pokretanju. / does not work — the activity is destroyed and the service with it. Queued fixes survive and go out on next launch. |
| GPS posle restarta telefona | **ne radi** dok se aplikacija ne otvori / does not work until the app is opened |
| iOS build | nije građen ni potpisan — traži Apple Developer nalog i Mac / not built or signed — needs an Apple Developer account and a Mac |

Trajno rešenje za prva dva je zaseban background-service paket koji pokreće
drugi Flutter engine, nezavisan od aktivnosti. To je nova zavisnost i zaseban
posao — namerno nije uzeto u Fazi 0. / The permanent fix for the first two is a
separate background-service package running a second Flutter engine,
independent of the activity. That is a new dependency and its own piece of
work — deliberately not taken on in Phase 0.

---

## 4. Backup i restore / Backup and restore

**SR** — Skripte su u `scripts/`. Restore je *isproban*, ne samo napisan:
rezultat provere je na kraju ovog odeljka.

**EN** — The scripts are in `scripts/`. The restore has been *performed*, not
merely written: the verification result is at the end of this section.

### 4.1 Skripte / The scripts

| Skripta / Script | Radi / Does |
|---|---|
| `backup.sh` | Dump (`-Fc`) + arhiva priloga + SHA-256 za oba, pa kopija van servera ako je podešena, pa brisanje starijih od `BACKUP_RETENTION_DAYS`. / A custom-format dump, the attachment archive, a SHA-256 for each, then the off-site copy if configured, then the prune. |
| `restore.sh` | Vraća dump u bazu; sa `--files <dir>` raspakuje i priloge. Odbija da pregazi bazu koja ima tabele bez `--force`. / Restores into a database; with `--files <dir>` unpacks the attachments too. Refuses to overwrite a populated database unless `--force`. |
| `verify-restore.sh` | Ceo krug: backup → restore u privremenu bazu → poređenje broja redova → **provera da svaki prilog u bazi ima svoj fajl** → brisanje privremene. / The whole round trip, including the check that every attachment row has its file. |
| `offsite.sh` | `push` (šalje i proverava), `pull` (vraća sa udaljene lokacije), `status` (koliko je stara najnovija potvrđena kopija). / `push` uploads and verifies, `pull` fetches back, `status` reports how old the newest confirmed copy is. |
| `test-offsite.sh` | Testira potpisivanje i ceo krug prema lokalnom S3 serveru koji proverava potpis. / Exercises the signing and the round trip against a local S3 that checks the signature. |

Sve tri koriste standardne `PG*` promenljive, pa `~/.pgpass` i `PGSERVICE`
rade kao i inače. / All three use the standard `PG*` variables, so `~/.pgpass`
and `PGSERVICE` keep working.

```bash
# ručno / by hand
PGDATABASE=construction BACKUP_DIR=/mnt/backups ./scripts/backup.sh

# vežba oporavka — pokrenuti posle svake promene šeme
# restore rehearsal — run after any schema change
./scripts/verify-restore.sh
```

### 4.2 Zakazivanje / Scheduling

```bash
docker compose --profile backup up -d
```

Servis je opt-in namerno: razvojni stack ga ne treba, a produkcija ne sme da
se oslanja na podrazumevanu vrednost. / The service is opt-in on purpose: a
development stack does not need it, and a deployment should not rely on a
default.

### 4.3 Kopija van servera / The off-site copy

**SR** — Dump na volumenu pored baze preživljava obrisanu tabelu i lošu
migraciju. Ne preživljava gubitak mašine. Ovo je korak koji to menja, i sada
je automatizovan — potrebni su samo nalog i ključevi, jer su oni vaši.

**EN** — A dump on a volume beside the database survives a dropped table and a
bad migration. It does not survive losing the host. This is the step that
changes that, and it is automated now — what it needs is an account and a key
pair, because those are yours.

1. Napravite bucket kod bilo kog S3-kompatibilnog provajdera (AWS, Backblaze
   B2, Wasabi, Cloudflare R2, tuđi MinIO). / Create a bucket at any
   S3-compatible provider.
2. Napravite ključ sa pravom pisanja **samo u taj bucket**. Uključite
   versioning ili object-lock: nalog koji sme i da briše je meta za
   ransomware. / Create a key with write access to **that bucket only**. Turn
   on versioning or object-lock — an uploader that can also delete is a
   ransomware target.
3. Napravite ključ za šifrovanje i **čuvajte ga negde drugde**:
   `age-keygen -o backup-key.txt`. Backup šifrovan ključem koji je izgoreo
   zajedno sa serverom nije backup. / Generate an encryption key and **keep it
   somewhere else**. A backup encrypted to a key that burned with the server
   is not a backup.
4. Upišite u `.env`: `OFFSITE_ENDPOINT`, `OFFSITE_BUCKET`, `OFFSITE_REGION`,
   `OFFSITE_ACCESS_KEY_ID`, `OFFSITE_SECRET_ACCESS_KEY`,
   `OFFSITE_AGE_RECIPIENT`. / Put these in `.env`.

Od tada svaki backup ide gore i **proverava se** — poredi se kontrolna suma
onoga što provajder kaže da drži sa onim što je poslato. Sve dok kopija nije
potvrđena, `backup.sh` odbija da obriše lokalnu, ma koliko bila stara. / From
then on every backup is uploaded and **verified** — the provider's checksum is
compared with what was sent — and `backup.sh` refuses to prune a local copy
that has no confirmed off-site one, however old it is.

```bash
# koliko je stara najnovija potvrđena kopija (za monitoring)
# how old is the newest confirmed copy — point a monitor at this
./scripts/offsite.sh status 26

# oporavak kad servera više nema / recovery when the host is gone
./scripts/offsite.sh pull construction-20260809T152401Z.dump      /tmp/r.dump
./scripts/offsite.sh pull construction-20260809T152401Z.dump.sha256 /tmp/r.dump.sha256
./scripts/offsite.sh pull construction-20260809T152401Z-files.tar.gz /tmp/r-files.tar.gz
./scripts/offsite.sh pull construction-20260809T152401Z-files.tar.gz.sha256 /tmp/r-files.tar.gz.sha256
./scripts/restore.sh /tmp/r.dump construction --files /var/lib/construction/storage
```

### 4.4 Šta ovo i dalje NE rešava / What this still does NOT solve

**SR** — Prenos je testiran prema lokalnom S3 serveru koji proverava potpis,
ali **nikad prema pravom AWS-u**. To traži nalog. Prvi put kad podesite ovo,
pokrenite `./scripts/offsite.sh push` ručno i pogledajte izlaz pre nego što se
oslonite na noćni posao.

**EN** — The transport is tested against a local S3 that verifies the
signature, but **never against real AWS**. That needs an account. The first
time you configure this, run `./scripts/offsite.sh push` by hand and read the
output before trusting the nightly.

Takođe nije mereno: koliko restore traje na produkcionoj količini podataka. /
Also unmeasured: how long a restore takes at production data volume.

### 4.5 Rezultat provere / Verification result

**SR** — Pokrenuto protiv baze sa realnom šemom (svih 10 migracija) i podacima:

**EN** — Run against a database with the real schema (all ten migrations) and
data in it:

```
Restore verification PASSED: 22 table(s), 10522 row(s) matched.
```

Provereno je i da provera **ume da padne**, jer „PASSED" inače ne znači ništa —
sve tri greške su izazvane namerno i sve tri su uhvaćene: /
The check was also proven able to **fail**, since "PASSED" means nothing
otherwise — all three faults were induced deliberately and all three were
caught:

| Greška / Fault | Ishod / Outcome |
|---|---|
| Dump stariji od baze (7 redova dodato posle) / Dump older than the database | `FAILED`, uz `diff` koji pokazuje `projects 19 → 12` |
| Dump ne odgovara svom checksum-u / Dump does not match its checksum | Odbijeno pre dodirivanja baze / Refused before touching the database |
| Restore preko pune baze bez `--force` / Restore over a populated database | Odbijeno, uz broj tabela koje bi bile pregažene / Refused, naming the tables at risk |

#### Prilozi i kopija van servera / Attachments and the off-site copy

**SR** — Isti postupak, ponovljen za dva dela koja su ranije nedostajala. Baza
sa 12 priloga i 12 fajlova na disku:

**EN** — The same exercise, repeated for the two halves that were missing. A
database with 12 attachments and their 12 files on disk:

```
Checking 12 attachment(s) against the archive
All 12 attachment(s) have their file.
Restore verification PASSED: 23 table(s), 24 row(s) matched.
```

I ovde je provereno da **ume da padne** / Proven able to **fail** here too:

| Greška / Fault | Ishod / Outcome |
|---|---|
| Baza ima priloge, `ATTACHMENT_DIR` nije podešen / Attachments recorded, `ATTACHMENT_DIR` unset | Upozorenje pri backup-u, pa `FAILED` pri proveri: „the restore would give a list of documents that are not there" |
| Dva fajla nedostaju u arhivi / Two files missing from the archive | `FAILED`, uz imena oba ključa / `FAILED`, naming both keys |
| Arhiva oštećena posle checksum-a / Archive corrupted after the checksum | Odbijeno pre raspakivanja / Refused before unpacking |

**Oporavak sa udaljene lokacije, izveden / Recovery from off-site, performed.**
Backup je poslat na S3 endpoint, lokalna kopija je zatim **obrisana**, i sistem
je vraćen samo sa udaljene lokacije, u drugi direktorijum i drugu bazu: 12
redova, 12 fajlova, svaki red ima svoj fajl. / The backup was uploaded, the
local copy was then **deleted**, and the system was restored from the remote
copy alone, into a different directory and a different database: 12 rows, 12
files, every row with its file.

Ta vežba je odmah našla i pravu grešku: `sha256sum putanja > putanja.sha256`
upisuje apsolutnu putanju, pa se checksum napisan na mašini koja je crkla
proverava prema putanji koje na novoj mašini nema — prvi korak pravog oporavka
je pao na fajlu koji je bio potpuno ispravan. Sada se upisuje samo ime fajla. /
That drill immediately found a real bug: the checksum files recorded the
absolute path of the machine that wrote them, so the first step of a recovery
onto a different host failed on a file that was perfectly intact. They now
record the basename.

**Šta i dalje nije provereno / Still unverified:** prenos prema *pravom* AWS-u
(testiran je prema lokalnom S3 serveru koji proverava potpis), oporavak na
drugom *fizičkom* serveru, i vreme potrebno za restore na produkcionoj količini
podataka. / The transport against *real* AWS (it is tested against a local S3
that verifies the signature), recovery onto a different *physical* host, and
how long a restore takes at production data volume.

---

## 5. Puštanje u rad / Deploying

**SR** — Sve što je potrebno je u `deploy/`. Jedan host, četiri kontejnera,
TLS na ivici. /
**EN** — Everything needed is in `deploy/`. One host, four containers, TLS at
the edge.

```
docker compose -f deploy/docker-compose.prod.yml up -d
```

| Kontejner / Container | Šta radi / What it does |
|---|---|
| `caddy` | Jedini koji objavljuje portove (80, 443). Sam pribavlja i obnavlja sertifikat. / The only one publishing ports. Obtains and renews the certificate itself. |
| `admin` | React panel iza nginx-a, na 8080 unutar mreže. / The React panel behind nginx, on 8080 inside the network. |
| `api` | ASP.NET Core, nedostupan spolja. / Unreachable from outside. |
| `postgres` | Baza, nedostupna spolja. / The database, unreachable from outside. |
| `backup` | Noćni backup, upaljen po podrazumevanom — vidi §4. / Nightly backup, on by default — see §4. |

### 5.1 Pre prvog pokretanja / Before the first start

1. **`DOMAIN` mora već da pokazuje na ovaj host.** Caddy dokazuje kontrolu nad
   imenom preko porta 80; ime koje još ne pokazuje ovamo obara tu proveru i
   troši Let's Encrypt kvotu. /
   **`DOMAIN` must already resolve here.** Caddy proves control over port 80;
   a name that does not yet point here fails that check and burns quota.
2. `cp deploy/.env.example deploy/.env` i popuniti. Lozinke se **generišu**:
   `openssl rand -base64 32`. / and fill it in. Generate the passwords.
3. Portovi 80 i 443 otvoreni prema internetu; **5432 i 8080 nisu.** /
   Ports 80 and 443 open; **5432 and 8080 are not.**

### 5.2 Zašto je API i panel na istom imenu / Why the API and the panel share a name

**SR** — Refresh token je `HttpOnly; Secure; SameSite=Strict` kolačić. Sa
drugog porekla browser ga ne bi ni slao, pa bi se operater odjavljivao pri
svakom osvežavanju — bez ijedne greške bilo gde. Zato proxy šalje `/api/*` na
API, a sve ostalo na panel. /
**EN** — The refresh token is a `SameSite=Strict` cookie. From a different
origin the browser would not send it at all, and the operator would be signed
out on every reload with nothing in any log. So the proxy routes `/api/*` to
the API and everything else to the panel.

### 5.3 Jedna slika, više instalacija / One image, many installations

**SR** — `vite build` peče `VITE_*` u bundle, što bi značilo posebnu sliku po
klijentu i novi build zbog pogrešno otkucanog imena hosta. Umesto toga slika
piše `config.js` pri pokretanju iz `API_BASE_URL` i `GOOGLE_MAPS_API_KEY`.
Ista slika radi svuda; adresa je stvar deploymenta, ne builda. /
**EN** — A Vite build bakes `VITE_*` into the bundle, which would mean one
image per customer and a release to fix a hostname. Instead the image writes
`config.js` at start-up. The same image runs anywhere.

### 5.4 Migracije / Migrations

**SR** — `Database__ApplyMigrationsOnStartup` je `true` ovde, i to je bezbedno
iz jednog razloga: ovaj fajl pokreće **tačno jedan** API kontejner. Dve replike
bi se trkale oko iste migracije. Ako stack ikad dobije drugu repliku, ovo se
gasi i migracije se puštaju kao zaseban korak pre rolovanja. /
**EN** — It is `true` here, safe for exactly one reason: this file runs exactly
one API container. Two replicas would race. If a second replica is ever added,
turn this off and run migrations as their own step.

### 5.5 Provera da stack stvarno radi / Proving the stack works

```
scripts/smoke-deploy.sh
```

**SR** — Podiže pravi stack sa `DOMAIN=localhost` (Caddy tada izdaje sopstveni
sertifikat, bez Let's Encrypt) i proverava ono što YAML ne može: da TLS radi i
da HTTP preusmerava na njega, da panel dobija adresu *ove* instalacije a ne
build-a, da prijava kroz proxy vraća kolačić koji je i `HttpOnly` i `Secure`, i
da baza i API nisu dostupni spolja. Briše sve za sobom. /
**EN** — Brings the real stack up and checks what YAML cannot: that TLS works
and HTTP redirects to it, that the panel got *this* installation's address
rather than the build's, that a sign-in through the proxy returns a cookie that
is both `HttpOnly` and `Secure`, and that the database and API are unreachable
from outside. Cleans up after itself.

**SR** — Provera kolačića je najvrednija: `Secure` se postavlja iz
`Request.IsHttps`, što je tačno onda kad API veruje proxyju
(`Network__TrustedProxies`). Pogrešna adresa proxyja → kolačić bez `Secure` →
browser ga odbacuje → operater se odjavljuje pri svakom osvežavanju. Ta greška
se ne vidi ni u jednom logu. /
**EN** — The cookie check is the valuable one: `Secure` follows
`Request.IsHttps`, which is true only when the API trusts the proxy. A wrong
proxy address means a cookie without `Secure`, which the browser discards — and
that failure appears in no log at all.

Isti skript pokreće i CI, na svaki push (`.github/workflows/release.yml`). /
CI runs the same script on every push.

### 5.6 Objavljivanje slika / Publishing images

**SR** — `release.yml` gradi obe slike na svaki push, a **objavljuje** ih u
GHCR samo sa podrazumevane grane i sa `v*` taga. Slika u registru je nešto što
neko može greškom da pusti u rad, pa polugotova grana nema šta da je ostavlja
tamo. Za deployment koji treba da bude ponovljiv, pinuj tag ili SHA umesto
`latest`. /
**EN** — `release.yml` builds both images on every push and **publishes** only
from the default branch and from a `v*` tag. An image in a registry is
something somebody can deploy by accident. Pin a tag or a SHA rather than
`latest` for a deployment you intend to reproduce.

### 5.7 Šta ovde još ne postoji / What is still missing here

**SR** — Automatski deployment na server i staging okruženje. Oba traže host
kojeg nema: pipeline gradi i objavljuje slike, ali ih niko ne pušta u rad —
to je i dalje `docker compose pull && up -d` na mašini. Kad host postoji,
nedostaje jedan job sa SSH ključem u tajnama. /
**EN** — Automatic deployment to a server, and a staging environment. Both need
a host that does not exist: the pipeline builds and publishes images, but
nothing rolls them out — that is still `docker compose pull && up -d` on the
machine. When a host exists, what is missing is one job with an SSH key in
secrets.
