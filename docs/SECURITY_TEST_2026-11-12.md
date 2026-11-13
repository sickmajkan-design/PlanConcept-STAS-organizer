# Sigurnosno testiranje iz perspektive napadača — 2026-11-12

Samo lokalno: API na `127.0.0.1:5000` i panel na `localhost:5173`, prazna baza na Postgresu `5433`. Nijedan server niti produkcija nisu dirani. Testovi nisu slali kod ni podatke na internet.

## Rezultat u jednoj rečenici

Jedan stvarni sigurnosni propust (pohranjeni XSS kroz pregled Word dokumenta, dokazan u pregledniku), jedna funkcionalna greška koja liči na SQL injection a nije, i nekoliko manjih napomena. SQL injection, zaobilaženje prijave i eskalacija ovlaštenja nisu uspjeli.

## Nalazi

### N1 — Pohranjeni XSS kroz pregled Word/Excel dokumenta (SREDNJE)

**Gdje:** `src/construction_admin/src/components/AttachmentPreviewDialog.tsx`, funkcija `sanitizeHtml`.

**Dokaz.** Napravio sam `.docx` sa šest linkova (`evil-links.docx`, koji ostaje u privremenom folderu sesije) i učitao ga kao prilog zaposleniku. Pregled u panelu ga je otvorio, a klik na tri linka je izvršio skriptu:

```
mammoth -> panel sanitizer:
plain         href=null                       (uklonjen)
uppercase     href=null                       (uklonjen)
tab           href="jav<TAB>ascript:alert(1)"      PROŠAO
newline       href="java<LF>script:alert(1)"       PROŠAO
leading-ctrl  href="<U+0001>javascript:alert(1)"   PROŠAO
data:         href="data:text/html;base64,..."     PROŠAO
Playwright (pravi Chromium, panel na :5173):  ALERTS FIRED ["1","1","1"]
```

Provjera je radila nad pravim panelom i pravim API-jem, tako da je skripta izvršena u izvoru panela.

**Uzrok.** Filter uklanja samo `href` koji počinje tačno s `javascript:`. Preglednik pri čitanju URL-a briše tabulatore, nove redove i kontrolne znakove, pa `jav⇥ascript:` ipak postaje `javascript:`. Lista zabranjenih elemenata i atributa (denylist) nikad nije potpuna.

**Uticaj.** Skripta radi kao osoba koja gleda dokument i može pozivati API s njenim tokenom (token je u `sessionStorage`, vrijedi 15 minuta, a panel može tražiti novi preko kolačića). Napadač je svako ko može učitati dokument koji osoblje kasnije otvori (radnik, zahtjev za dokument, kupac ako dobije upload). Potreban je klik na link u pregledu.

**Ublažavanje u produkciji (samo iz čitanja koda, ne testirano uživo).** `deploy/Caddyfile` ima `script-src 'self'` bez `unsafe-inline`, što bi trebalo blokirati `javascript:` linkove. To je odbrana u dubinu, a ne popravak, jer lokalni razvoj nema taj CSP i CSP ne štiti od `data:` ni od ostalih slabosti filtra.

**Preporuka (popravak).**
1. Umjesto zabranjene liste, dozvoljena lista: za svaki `href` ukloniti sve znakove ispod U+0020 i razmake, pa `new URL(vrijednost, location.href)`; dozvoliti samo `http:`, `https:`, `mailto:`, `tel:`. Sve ostalo ukloniti.
2. Ukloniti i `formaction`, `xlink:href`, `srcdoc`, `action` i `style`.
3. Linkove otvarati s `rel="noopener noreferrer"` i `target="_blank"`.
4. Alternativa: DOMPurify s predefinisanim profilom (nova zavisnost, mali paket).
5. Test u Vitestu s ovih šest vektora (`evil-links.docx` ih sadrži) i E2E koji klikne na link.

### N2 — Pretraga poslanih obavijesti uvijek pada s 500 (NISKO, funkcionalna greška)

**Gdje:** `GET /api/Notifications/broadcasts?Search=...`, `BroadcastFeatures.cs:227-229`.

**Dokaz.** Svaka vrijednost daje 500, i bezopasna (`hello`) i napadačka (`'`, `' OR '1'='1`, `'; SELECT pg_sleep(4)--`). Vrijeme odgovora se nije povećalo, pa to nije injekcija. Iz loga servera:

```
Npgsql.PostgresException: 42883: function lower(jsonb) does not exist
```

**Uzrok.** `AudienceJson` je `jsonb`, a upit zove `ToLower()` na njemu. Unos se šalje kao parametar, ne spaja se u SQL.

**Uticaj.** Polje za pretragu na ekranu "Poslane obavijesti" ne radi nikome. Odgovor ne otkriva unutrašnje detalje (samo `traceId`).

**Popravak.** `EF.Functions.ILike(b.Title, ...)` i za publiku tekstualni oblik (`AudienceJson::text`, ili posebna tekstualna kolona), te escape za `%` i `_` u izrazu. Dodati test u `BroadcastTests` (trenutno nema testa za `Search`).

### N3 — Slaba lozinka `Password1` je prihvaćena (NISKO)

Pravila traže 8 znakova, veliko, malo slovo i broj. Lozinka koja je u svakoj listi najčešćih prolazi. `123456`, `password` i `a` su odbijene.

**Preporuka.** Lista najčešćih lozinki lokalno (npr. 10.000, bez poziva na internet) i duža minimalna dužina (12). Već je na listi obaveza prije LIVE servera.

### N4 — Napomene (INFO)

- **Zaključavanje računa:** 10 pogrešnih pokušaja zaključa račun 15 minuta, uz istu poruku `Invalid email or password.` (nema otkrivanja da li račun postoji, što je dobro). Cijena: bilo ko ko zna e-poštu može nekoga zaključati. Ograničenje po IP adresi već postoji (120 u prozoru), pa je to svjestan kompromis iz koda.
- **Token pristupa nakon odjave:** `GET /api/auth/me` vraća 200 nakon logouta dok token ne istekne (15 min). Uobičajeno za JWT, a refresh token je nakon odjave odbijen (401). Ako treba trenutni opoziv (npr. kad se korisnik onemogući), provjeravati `jti` prema tabeli sesija.
- **`/health/dependencies` je javan** i pokazuje stanje antivirusa. Ograničiti na interni pristup.
- **Samo razvojno okruženje:** zaglavlje `Server: Kestrel` i nema HSTS na samom API-ju. `deploy/Caddyfile` postavlja `-Server` i `Strict-Transport-Security` (samo pročitano, nije provjereno uživo).
- **Staro, ne greška:** skripte `scripts/qa/flows_*` imaju 4 zastarjela očekivanja (npr. `AB-09` zove rutu koja više ne postoji, `FG-02` i `IN-*` su bili posljedica toga što sam skripte pokrenuo istovremeno nad istim nalozima). Provjerio sam `FG-02` zasebno: admin bez odobrenja dobija 403 na novčanim ekranima. `AB-02` (API prihvata preklapajuće odsustvo za istog radnika, 201) treba provjeriti da li je to namjerno.

## Šta je izdržalo (s dokazima)

| Napad | Rezultat |
|---|---|
| **SQL injection** na 82 tekstualna GET parametra × 7 uzoraka (uključujući `pg_sleep`) | Nijedan odziv nije kasnio, nema curenja SQL greške; jedina 500 je N2 |
| **SQL injection** u prijavi (4 varijante) | uvijek 401 |
| **Fuzz** svih 202 pisanja (SQL, XSS, 20.000 znakova, null bajt, emoji, `${jndi}`, `{{7*7}}`) | 0 grešaka servera |
| Sirovi SQL u kodu | samo `LocationPartitions.cs`; imena dolaze iz datuma i sistemskog kataloga, ne od korisnika |
| **JWT**: izmijenjena uloga, `alg: none`, bez potpisa, loš potpis | sva četiri odbijena (401) |
| **Refresh token** | rotacija radi; ponovna upotreba iste vrijednosti = 401 i porodica opozvana |
| **Eskalacija**: radnik/predradnik/PM/admin prave Admin/SuperAdmin | 8 od 8 odbijeno (403) |
| Radnik mijenja vlastitu ulogu | 403 |
| Radnik čita tuđi zapis zaposlenika | 403 |
| **Anonimni pristup** (148 GET ruta × 7 uloga; varijante putanje `/API/USERS`, `;.css`, `%2f`) | samo namjerno javne rute |
| **CORS** za `https://evil.example` i `null` | nema `Access-Control-Allow-Origin` |
| **Prijava u pregledniku** s SQL i XSS tekstom | ostaje na `/login`, nijedan `alert` |
| **Pohranjeni XSS** (`<img onerror>`, `<svg onload>`) u naziv kupca/projekta/obavijesti/zaposlenika, na 6 ekrana | prikazano kao tekst, nema umetnutog DOM-a, nema `alert` |
| **Upload**: HTML kao `.pdf`, `.html`, `.svg`, `.exe` kao `.png`, duplo proširenje | odbijeno (400/409) |
| Upload s imenom `../../../evil.pdf` | ime očišćeno u `evil.pdf`; preuzimanje uvijek `attachment` + `nosniff` |
| Upload 26 MB | odbijen |
| **Logo SVG** sa `<script>`, `onload`, `javascript:`, `foreignObject`, vanjskim `href` | sve uklonjeno; odgovor ima `sandbox` CSP i `nosniff` |
| **Tokeni** poziva i resetovanja lozinke | 256 bita slučajnosti; 40 + 30 pogađanja = 404/400 |
| **Prijave:** isti odgovor za nepoznat i postojeći račun, vrijeme 0,26 s i 0,29 s | nema otkrivanja računa; `forgot-password` isto (202) |
| Greške klijenta (anonimno) | 3 MB tijelo = 400; 2 MB stack = 400; poplava 80 zahtjeva = 28 prihvaćeno, 52 usporeno (429) |
| HTTP metode `TRACE/PUT/DELETE/PATCH` na prijavi | 405 |
| Odvajanje kupaca (portal): `flows_finance_portal_locations.py` | 31/31 prolazi |

## Nije testirano

- **Produkcija i test server** (po vašoj odluci samo lokalno): stvarni CSP iz Caddyja, HTTPS, `Secure` kolačić, rate limit iza proxyja.
- **Mobilna aplikacija** (Flutter): lokalno spremanje tokena, presretanje prometa, deep linkovi.
- **MFA** tok (isključen) i prolazna ključa (passkeys).
- **Granice poslovnih jedinica** s računima koji imaju ograničen domet po jedinici (nema takvih naloga u skripti za postavljanje). Preporuka: dodati ih u `qa_setup.py` pa ponoviti horizontalne provjere.
- **Sadržaj odgovora za radnika i predradnika:** za većinu ruta provjeravani su samo statusni kodovi, ne i da li lista sadrži tuđe redove.

## Regresioni E2E (nakon testiranja)

Postojeći Playwright paket protiv pravog API-ja i panela: **39 od 39 prolazi** (desktop 31, tablet 3, telefon 5, 14,5 minuta). Obuhvata prijavu, kolačić koji skripta ne može pročitati, odjavu, zaposlenike, dokumente, zahtjeve za dokumente, Knjigu troškova, pomoć i raspored na telefonu.

Moja sigurnosna specifikacija `src/construction_admin/e2e/_sec-attacks.spec.ts` (git je ignoriše) ima 4 testa: 3 prolaze, a četvrti (Word pregled) pada i dokazuje N1. Kad se N1 popravi, taj test treba da prođe i može se preimenovati u stalni test.

Mobilni E2E na emulatoru još nije rađen: u projektu nema `integration_test`, to je sljedeći korak.

## Stanje poslije popravaka (2026-11-12, lokalno, nije komitirano)

| Nalaz | Popravak | Dokaz |
|---|---|---|
| **N1** XSS u pregledu Word/Excel | `src/utils/sanitizePreviewHtml.ts`: dozvoljena lista šema (`http`, `https`, `mailto`, `tel`, slika kao `data:image/...`), uklanjanje kontrolnih znakova prije čitanja šeme, uklanjanje `svg`, `form`, `style`, `formaction`; linkovi `noopener` | 19 Vitest testova; isti Word fajl u pravom pregledniku: svih 6 linkova uklonjeno, `ALERTS FIRED []` |
| **N2** pretraga obavijesti 500 | `BroadcastFeatures.cs`: poklapanje u memoriji nad naslovom i publikom, bez `lower(jsonb)` | 4 testa (naslov, velika slova, `%`/`_`, SQL tekst); živo: 200 za sve četiri riječi |
| **N3** `Password1` | `PasswordRules.cs`: lokalna lista čestih lozinki (ignoriše brojeve i simbole na kraju, `0→o`, `@→a`), samo tačno poklapanje | 43 jedinična testa; živo: `Password1`, `Password123!`, `Welcome2024`, `Passw0rd!` = 400, `Test1234!` i `Konj-Zelena-Cigla-7` = 201 |

Ograničenje N3: kratka lista, ne baza procurjelih lozinki (komentar `shortcut:` u kodu).
Cijeli E2E paket nakon popravaka: **39 od 39**; moja sigurnosna specifikacija: 4 od 4.
