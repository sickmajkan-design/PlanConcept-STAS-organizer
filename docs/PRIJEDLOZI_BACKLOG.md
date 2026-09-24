# Prijedlozi i backlog — sažetak iz sesije od 2026-09-23

Sve što je predloženo, sa stanjem: **URAĐENO** (u kodu i na testnom serveru), **ČEKA ODLUKU**, ili **PLANIRANO**. Obračun mjeseca (Evidencija) ima svoj dokument: [EVIDENCIJA_OBRACUN_PLAN.md](EVIDENCIJA_OBRACUN_PLAN.md).

---

## 1. Obavještenja i rekalibracija podataka — URAĐENO

Problem: kad se nešto obriše (zaposleni, dokument, zadatak…), obavještenja i značke koji se na to odnose ostaju i pokazuju na prazno.

| Stavka | Stanje |
|---|---|
| Brisanje obavještenja čiji je zapis obrisan, provjerom **svih** referenci u obavještenju (projekat, zaposleni, vozilo, alat, dokument, zadatak, apsens, materijal, smještaj), ne samo jedne po tipu | Urađeno |
| Čišćenje pri **loginu** | Urađeno |
| Čišćenje pri **čitanju zvonca** (broj nepročitanih i lista) | Urađeno |
| Periodični prolaz u pozadini na svakih 15 minuta (bilo 2 sata) | Urađeno |
| **Live osvježavanje panela** poslije svake uspješne akcije (značke, zvonce, lista, otvorena stranica) | Urađeno |
| Zvonce se osvježava svakih 30 s (bilo 60 s) | Urađeno |
| Podaci obrisanog zaposlenog (unosi vremena, dokumenti) se sakrivaju u listama i značkama | Urađeno |
| Značke u meniju računaju se iz živih podataka | Već je tako bilo |

**Ostaje (PLANIRANO):**
- **Ponovno pravljenje obavještenja pri loginu** za dokumente koji ističu, ugovore o smještaju i niske zalihe: izračunati šta treba da postoji i napraviti ono što fali. Prije toga provjeriti kako `DailyReminderService` sprečava duplikate.
- Brisanje direktno u bazi vidi se tek pri sljedećem čitanju/osvježavanju (panel nema push).

## 2. Dokumenti — URAĐENO, djelimično PLANIRANO

Odluka: fajlovi na serveru ostaju pod **neprozirnim ključevima** (`employees/<id>/<id>.pdf`). Imena i tipovi dokumenata ne idu u putanje, backupe i logove; preimenovanje zaposlenog ne znači premještanje fajlova. Čitljiva struktura se pravi pri izvozu.

| Stavka | Stanje |
|---|---|
| Izmjena dokumenta (tip, napomena, rok važenja, rok čuvanja), Admin i iznad | Urađeno (`PUT /api/v1/attachments/{id}`) |
| Dugmad Izmijeni / Obriši na stranici dokumenata koji ističu | Urađeno |
| Dokument sa aktivnim rokom čuvanja ne može se obrisati | Već je tako bilo, sada i na ovoj stranici |
| Dokumenti obrisanog zaposlenika/projekta/vozila/alata se sakrivaju | Urađeno |
| **Preuzmi sve kao ZIP** (`Radnici/Ime/Kategorija/fajl`), na stranici dokumenata i na listi dokumenata zapisa | Urađeno |
| ZIP: pojedinačna provjera prava čitanja, limit 500 dokumenata i 500 MB, log ko je izvezao, duplikati imena dobijaju `(2)` | Urađeno |
| Preview, download, upload | Već je postojalo |

**Ostaje:**
- **Opcija B — "Sačuvaj u folder na računaru"** (Chrome/Edge, izbor foldera i dozvola): ČEKA ODLUKU da li svi koriste Chrome ili Edge.
- **Rokovi čuvanja po tipu dokumenta** (zadane vrijednosti po kategoriji): ne postavljamo pravne rokove sami — ČEKA da kupac navede rokove.
- **Backup dokumenata na drugi disk / S3**: ČEKA ODLUKU. Dokumenti su osjetljivi (ljekarski nalazi, ugovori).
- Provjeriti na serveru da li fajlovi starih dokumenata postoje u skladištu (lokalno nisu, pa ZIP sadrži samo napomenu).

## 3. Lakše podešavanje za kupca (onboarding) — sloj 1 URAĐENO (lokalno), ostalo PLANIRANO

Zapažanje iz koda i dokumentacije: nema vodiča za prvo pokretanje; podešavanja su razbacana; tehnički dio (Firebase, keystore, AI ključ, domen, `.env`, backup) prepušten je vlasniku; mobilna aplikacija se povezuje ručnim podešavanjem uz poseban build. **Ovo je pregled koda, ne razgovor sa kupcem** — prije gradnje provjeriti sa jednim kupcem gdje se stvarno zaglavi.

Tri sloja:

**Sloj 1 — prvi dan (radni sistem za 30 minuta)**
| # | Stavka | Prioritet |
|---|---|---|
| 1 | Uvoz radnika iz Excela/CSV (pregled grešaka prije potvrde, bez duplikata, samo Admin i iznad, audit log) | Must — **urađeno** |
| 2 | Pozivnica radniku preko linka/QR-a (bez ručnog pravljenja naloga i lozinke) | Must — **urađeno** |
| 3 | Lista "šta još fali" na početnoj stranici ("3 radnika bez gradilišta, nema praznika") | Must — **urađeno** |
| 4 | Čarobnjak prvog pokretanja: firma → prvo gradilište → uvoz radnika → pozivnice (svaki korak ima "preskoči") | Should |

**Sloj 2 — svakodnevni rad**
| # | Stavka | Prioritet |
|---|---|---|
| 5 | Razumne zadane vrijednosti (smjena 07–15, praznici za državu, godišnji po zakonu) | Should |
| 6 | Provjera pri unosu (radnik bez gradilišta, gradilište bez lokacije) | Should |
| 7 | Šabloni gradilišta ("kopiraj iz prošlog") | Could |
| 8 | Jedno mjesto "Podešavanja" sa grupama: Firma, Ljudi, Obračun, Obavještenja | Could |

**Sloj 3 — tehnička strana**
| # | Stavka | Prioritet |
|---|---|---|
| 9 | Jedan build mobilne aplikacije, povezivanje kodom firme ili QR-om | Should |
| 10 | Provjera zdravlja u panelu ("push radi / backup je od jučer / mail nije podešen") sa uputstvom na srpskom | Should |
| 11 | Instalacija jednom naredbom za isporučioca (skripta: `.env`, domen, backup) | Could |

**Won't (sada):** self-service registracija novih firmi, plaćanje i pretplata.

**Kako je urađeno (sloj 1):**
- **Uvoz:** `POST /api/v1/employees/import` (Admin i iznad), pregled bez snimanja (`dryRun`) pa stvarni uvoz. Datoteka se čita u browseru (Excel ili CSV, prepoznaje uobičajene nazive kolona, datume u više formata). Red sa greškom se prijavi i izostavi, ostali se uvoze. Postojeća osoba se nalazi po broju, e-pošti pa imenu; zadano se ne dira, a opcija "dopuni" popunjava samo prazna polja, nikad ne prepisuje. Broj radnika se sam generiše (`R-0001`…). Ograničenje 1000 redova.
- **Pozivnica:** `POST /api/v1/invitations` pravi jednokratan link (važi 7 dana, novi poništava stari). Server čuva samo hash tokena. Radnik otvara `/invite/<token>`, bira lozinku i nalog se pravi vezan za njegov zapis. Svaki neispravan link (nepoznat, iskorišten, istekao) izgleda isto. Link i QR kod prikazuju se jednom, na stranici radnika, uz dugme Kopiraj. Slanje emailom nije uključeno (email na serveru nije podešen); link se šalje ručno (poruka, Viber, QR). Nova tabela `employee_invitations` (migracija `AddEmployeeInvitations`).
- **Šta još fali:** `GET /api/v1/setup/checklist` računa iz živih podataka (podaci o firmi, radnici bez naloga, radnici bez gradilišta, gradilišta bez lokacije, nedostaju praznici, nema radnika/gradilišta). Kartica na početnoj stranici, nestaje kad je sve uređeno.
- **Testovi:** 15 integracionih (uvoz, pozivnice, lista), 11 jediničnih za čitanje tabele, 2 za obavještenja obrisanog zaposlenog. Cijeli skup: 551 jedinični i 712 integracionih prolaze.

**Primjer stavke — Uvoz radnika iz Excela**
As a Admin, I want to učitati spisak radnika iz fajla, so that ih ne unosim jednog po jednog.
- [ ] Preuzmem šablon, popunim ga, učitam i vidim pregled ("47 novih, 3 sa greškom") prije potvrde.
- [ ] Radnik koji već postoji (isti broj) se ažurira ili preskače po izboru, bez duplikata.
- [ ] Loš red (nedostaje ime, pogrešan format) prikazuje razlog, a ostali se ne blokiraju.
- [ ] Samo Admin i iznad, uz zapis u audit log.

**Otvorena pitanja:** gdje se kupac zaglavi (prvo podešavanje, dodavanje radnika ili konfiguracija telefona); ko podešava (kupac ili isporučilac); tipičan broj radnika (10, 50, 200).

## 4. Evidencija (Ledgers) — pojednostavljenje — PLANIRANO

Rani plan (šablon pri prvom mjesecu, automatsko punjenje iz sistema, Jednostavan/Napredni režim, jednostavniji jezik, provjere prije zaključivanja) je **zamijenjen konkretnijim planom** nakon uvida u kupčev Excel. Vidi [EVIDENCIJA_OBRACUN_PLAN.md](EVIDENCIJA_OBRACUN_PLAN.md).

Potvrđeno: Evidencija ostaje **samo za Super Admina**; korisnik je vlasnik; izvoz u Excel za knjigovođu je prioritet.

## 5. Redoslijed koji preporučujem

1. Obračun mjeseca, Faza 1 (računske kolone, šablon, zbir firme, lista "Za provjeru", izvoz u Excel).
2. Obračun mjeseca, Faza 2 (sati iz radnog vremena, cijene iz sistema).
3. Onboarding sloj 1 (uvoz radnika, pozivnica, lista "šta fali").
4. Ponovno pravljenje obavještenja pri loginu.
5. Dokumenti: rokovi čuvanja i backup, čim kupac potvrdi pravila.
6. Onboarding slojevi 2 i 3.

## 6. Pitanja koja čekaju kupca

- Obračun: šest pitanja u §8 dokumenta [EVIDENCIJA_OBRACUN_PLAN.md](EVIDENCIJA_OBRACUN_PLAN.md).
- Dokumenti: Chrome/Edge, pravni rokovi čuvanja, backup.
- Onboarding: gdje se zaglavi, ko podešava, koliko radnika.
