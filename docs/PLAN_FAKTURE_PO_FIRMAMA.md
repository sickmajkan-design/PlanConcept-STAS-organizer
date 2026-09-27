# Fakture klijentima, na jednu ili više firmi: plan

Sastavljeno 2026-09-27 (Product Owner). Zamjenjuje prvu verziju od istog dana. Ispravka: **izdavalac je naručilac platforme (El Plan Concept GmbH), a primaoci su njegovi klijenti (kupci)**. Dario račune ne prima, nego ih izdaje. Nadovezuje se na A9–A12 (obračun i naplata) i B12–B15 (firme ispod klijenta) iz [BACKLOG_SVE_NA_JEDNOJ_LISTI.md](BACKLOG_SVE_NA_JEDNOJ_LISTI.md), te na odgovore iz [PLAN_ODGOVORI_KUPCA_2026-09-27.md](PLAN_ODGOVORI_KUPCA_2026-09-27.md).

## 1. Šta ima smisla napraviti

Posao firme je posuđivanje radnika, pa se većina faktura **može sastaviti iz podataka koji već postoje**: sati radnika po gradilištu × cijena za klijenta. Zato modul nije prazan obrazac, nego:

1. **Nacrt fakture se sastavlja sam** iz upisanih (potpisanih) sati, za klijenta, projekat i mjesec (način obračuna "satnica").
2. **Ručne stavke** za paušal i aufmaß (kupac: naplata je zbir računa; paušal se dijeli "prema odrađenom poslu").
3. **Jedna ili više firmi klijenta**: faktura ide na jednu firmu, na više firmi kao jedan dokument sa podjelom, ili kao po jedna faktura za svaku firmu.
4. **Izdana faktura hrani obračun**: naplata sekcije je zbir izdanih faktura za tu firmu i mjesec.
5. **Potpisane satnice** se čuvaju uz klijenta i projekat po kalendarskoj sedmici (odgovor 1) i vežu se na fakturu kao dokaz.

Pretpostavka koju treba potvrditi: program **sastavlja fakturu i izvozi PDF** za knjigovodstvo. Zakonski važeće izdavanje (redni broj, PDV, e-račun) ostaje u knjigovodstvenom programu dok se to ne odluči (vidi §5).

## 2. Epic: Fakture

**Goal:** Faktura klijentu se sastavlja iz podataka o satima i radu, vezana je za tačno one firme kojima se izdaje, a obračun čita izdane fakture.
**Users affected:** Super Admin i korisnici kojima on dodijeli pravo. Niko drugi ne vidi iznose (odgovor 11).
**Success metric:** Faktura za satnicu se sastavi u manje od pet dodira; zbir faktura po firmi za mjesec jednak je naplati te firme u obračunu.

| Redoslijed | Priča | Bodovi | Prioritet |
|---|---|---|---|
| 1 | FA1. Podaci za fakturisanje (klijent i njegove firme) | 3 | Critical |
| 2 | FA2. Ručna faktura na jednu firmu | 8 | Critical |
| 3 | FA3. Nacrt iz sati (satnica) | 13 | Critical |
| 4 | FA4. Faktura na više firmi | 8 | Critical |
| 5 | FA5. Pregled, status i izvoz | 5 | High |
| 6 | FA6. Obračun čita fakture po firmi | 5 | Critical |
| 7 | FA7. Potpisane satnice po sedmici uz fakturu | 5 | High |
| 8 | FA8. Storno i ispravka | 5 | High |
| 9 | FA9. PDF izvoz fakture | 5 | Medium |

Isporuka: **FA1 → FA2 → FA6** (ručna faktura hrani obračun), zatim **FA3 → FA4**, zatim FA5, FA7–FA9.

### FA1. Podaci za fakturisanje

**As a** Super Admin,
**I want to** da za klijenta i svaku njegovu firmu piše naziv, adresa, poreski broj i rok plaćanja,
**So that** faktura ima tačnog primaoca.

**Acceptance Criteria:**
- [ ] Given klijent bez dodatnih firmi, then je klijent sam primalac (jedna implicitna firma).
- [ ] Given klijent sa firmama (B12), then svaka firma ima vlastite podatke, a prazna polja preuzimaju podatke klijenta.
- [ ] Given podaci izdavaoca (El Plan Concept) u postavkama firme (naziv, adresa, IBAN, poreski broj), then se koriste na svakoj fakturi.
- [ ] Edge: faktura se ne može izdati primaocu bez poreskog broja i adrese; poruka kaže šta fali.

**Story Points:** 3 · **Priority:** Critical

### FA2. Ručna faktura na jednu firmu

**As a** Super Admin,
**I want to** sastaviti fakturu sa vlastitim stavkama,
**So that** mogu fakturisati paušal i aufmaß (zbir računa je naplata).

**Acceptance Criteria:**
- [ ] Given projekat i firma, when dodam fakturu, then upisujem stavke (opis, količina, jedinica, cijena), datum, rok plaćanja i mjesec obračuna kojem se pripisuje.
- [ ] Given stavke, then se zbir računa sam; jedna valuta (EUR).
- [ ] Given paušal koji traje više mjeseci, then se dio pripisuje mjesecu "prema odrađenom poslu", ručnim upisom iznosa po mjesecu; zbir mjeseci mora biti jednak iznosu fakture.
- [ ] Given nacrt, then ga mogu mijenjati; poslije izdavanja se zaključava (izmjena samo kroz FA8).
- [ ] Edge: iznos 0 ili nula stavki se odbija; broj fakture je jedinstven (odbija se duplikat).

**Notes / Out of Scope:** Koristi postojeću evidenciju prihoda po projektu (`ProjectRevenue`), ne pravi drugu (bez dupliranja naplate).

**Story Points:** 8 · **Priority:** Critical

### FA3. Nacrt iz sati (satnica)

**As a** Super Admin,
**I want to** da se za klijenta, projekat i mjesec sastavi nacrt iz sati,
**So that** ne prepisujem sate × cijenu.

**Acceptance Criteria:**
- [ ] Given sekcija sa načinom obračuna "satnica", when pokrenem "Sastavi iz sati", then nastaje nacrt sa stavkom po radniku (sati × cijena za klijenta) ili sažeto po ulozi, kako izaberem.
- [ ] Given izvor sati je ručni unos iz potpisanih satnica (A1), then se koristi taj, a ne sati iz aplikacije.
- [ ] Given radnik bez cijene za klijenta, then je stavka označena "nema cijene", a nacrt se ne može izdati dok se ne dopuni.
- [ ] Given nacrt već postoji za isti mjesec i projekat, then se nudi izmjena postojećeg, a ne drugi.
- [ ] Given kooperanti (odgovor 2: piše se samo naziv firme), then je red po nazivu firme kooperanta, sa satima koje je poslao mejlom.
- [ ] Given sedmica preko granice mjeseca, then se dijeli po datumu (odgovor je već primijenjen).
- [ ] Given kancelarija ili radnici bez sati, then nisu na fakturi.

**Story Points:** 13 · **Priority:** Critical (zavisi od A1, A9)

### FA4. Faktura na više firmi

**As a** Super Admin,
**I want to** fakturu izdati na više firmi istog klijenta,
**So that** svaka firma plati samo svoj dio.

**Acceptance Criteria:**
- [ ] Given nacrt iz sati i radnici raspoređeni u firme (B13), then izbor "po firmama" pravi **po jednu fakturu za svaku firmu** sa satima njenih radnika (zbir svih faktura = zbir svih sati × cijena).
- [ ] Given ručna faktura, then izbor "podijeli na firme" traži iznos po firmi; zbir dijelova mora biti jednak ukupnom, a razlika je vidljiva u poruci.
- [ ] Given "podijeli ravnomjerno", then ostatak od centa ide prvoj firmi.
- [ ] Given firma drugog klijenta, then se ne nudi.
- [ ] Given radnik bez firme (stari zapisi), then se prikazuje "bez firme" i faktura se ne izdaje dok se ne dopuni.
- [ ] Edge: ista firma dvaput u jednoj podjeli se odbija; račun sa jednom firmom je običan račun (isti model).

**Notes / Out of Scope:** Račun na više klijenata (a ne samo firmi jednog klijenta) nije u opsegu.

**Story Points:** 8 · **Priority:** Critical (zavisi od B12, B13)

### FA5. Pregled, status i izvoz

**As a** Super Admin,
**I want to** vidjeti sve fakture po klijentu, firmi, projektu i mjesecu, sa statusom,
**So that** znam šta je izdato, a šta plaćeno.

**Acceptance Criteria:**
- [ ] Given lista, then su vidljivi broj, datum, klijent, firma, projekat, iznos i status (nacrt / izdana / plaćena / storno) sa istim statusnim oznakama kao ostale liste.
- [ ] Given filteri i pretraga po broju, then lista se sužava i pokazuje zbir prikazanih.
- [ ] Given izdana faktura sa prošlim rokom plaćanja, then je označena "kasni".
- [ ] Given izvoz u Excel, then po jedan red po fakturi i firmi.
- [ ] Given prazna lista, then prazno stanje sa pozivom "Sastavi prvu fakturu".

**Story Points:** 5 · **Priority:** High

### FA6. Obračun čita fakture po firmi

**As a** Super Admin,
**I want to** da naplata paušalne i aufmaß sekcije bude zbir izdanih faktura te firme za mjesec,
**So that** marža bude tačna.

**Acceptance Criteria:**
- [ ] Given sekcija vezana za klijenta i firmu, then je naplata zbir izdanih faktura (bez nacrta i storniranih) pripisanih tom mjesecu.
- [ ] Given nema izdanih faktura za mjesec, then je naplata "nema računa", a ne 0, i ulazi u "Za provjeru".
- [ ] Given mjesec je zaključen, then se nova faktura ne može pripisati tom mjesecu bez otključavanja.
- [ ] Given satnica sekcija, then naplata ostaje formula i **poredi se** sa zbirom faktura; razlika ide u "Za provjeru".

**Story Points:** 5 · **Priority:** Critical (zavisi od A11)

### FA7. Potpisane satnice po sedmici uz fakturu

**As a** Super Admin,
**I want to** čuvati potpisane satnice uz klijenta i projekat po kalendarskoj sedmici,
**So that** za svaku fakturu postoji dokaz.

**Acceptance Criteria:**
- [ ] Given klijent i projekat, when dodam fajl (PDF ili fotografija), then izaberem kalendarsku sedmicu (KW) i on se čuva pod tim klijentom, projektom i sedmicom.
- [ ] Given faktura iz sati, then su uz nju vidljive priložene satnice za obuhvaćene sedmice, a sedmice bez priloga imaju upozorenje (ne blokira).
- [ ] Given prilog, then ga vide samo korisnici sa pravom na iznose i satnice; brisanje ide u audit log.

**Notes / Out of Scope:** Isto što i A3, ali ključ je klijent + projekat + KW (odgovor 1). Prepoznavanje sati sa slike nije u opsegu.

**Story Points:** 5 · **Priority:** High

### FA8. Storno i ispravka

**As a** Super Admin,
**I want to** stornirati ili ispraviti izdanu fakturu,
**So that** naplata ostane tačna, a istorija vidljiva.

**Acceptance Criteria:**
- [ ] Given izdana faktura, when je stornirem, then nastaje negativna faktura sa vezom na original i istom podjelom po firmama.
- [ ] Given izmjena poslije izdavanja, then audit log bilježi ko, kada, staro i novo.
- [ ] Given brisanje izdane fakture, then se odbija; nudi se storno.

**Story Points:** 5 · **Priority:** High

### FA9. PDF izvoz fakture

**As a** Super Admin,
**I want to** izvesti fakturu u PDF,
**So that** je pošaljem klijentu ili knjigovođi.

**Acceptance Criteria:**
- [ ] Given faktura, when izvezem, then PDF sadrži podatke izdavaoca i primaoca, stavke, iznos, rok plaćanja i broj.
- [ ] Given faktura na više firmi, then je jedan PDF po firmi.
- [ ] Given nacrt, then je PDF označen vodenim žigom "NACRT".

**Notes / Out of Scope:** Slanje e-poštom iz programa i e-račun (XRechnung/ZUGFeRD) su kasnija stavka; zavisi od odluke §5.

**Story Points:** 5 · **Priority:** Medium

## 3. Prava (odgovor 11)

- Fakture, cijene poslova i sve u € vidi samo Super Admin i korisnici kojima on izričito dodijeli pravo.
- Pravo se **ne može dodijeliti ulozi Radnik** (kupac: "fiksno, da ne može ići na radnike"). Pravo je po osobi, ne po ulozi, i svako dodjeljivanje ide u audit log.
- Admin bez dodijeljenog prava ne vidi iznose. Voditelj vidi raspored, flotu i projekat na kojem je raspoređen, ali bez €.

## 4. Model podataka (predlog)

- `Invoice`: izdavalac, klijent, projekat, broj, datum, rok, status, mjesec obračuna, veza na storno.
- `InvoiceLine`: opis, količina, jedinica, cijena, radnik ili firma kooperanta (za satnicu).
- `InvoiceShare`: `InvoiceId`, `CompanyId`, `Amount` (dijelovi po firmi; zbir = iznos fakture, provjera u aplikaciji i u bazi).
- `ProjectRevenue` (postojeći) ostaje izvor naplate; izdana faktura ga puni (jedan red), da se prihod ne računa dvaput.
- Prilog satnice: postojeći modul priloga sa ključem klijent + projekat + KW.

## 5. Rizici i pitanja

- **Zakonski izgled fakture** u Njemačkoj (podaci, PDV ili obrnuta naplata, redni broj, e-račun od 2027) treba da potvrdi knjigovođa. Predlog: prva verzija je "nacrt za knjigovodstvo", a ne zvanična faktura.
- **Zavisnost od B12 i B13.** Bez firmi ispod klijenta i rasporeda radnika u firmu, FA4 nije moguć; FA2 i FA3 rade na jednoj implicitnoj firmi.
- **Zaokruživanje** podjele mora uvijek dati tačan zbir.
- **Pitanja za Darija:** (1) Izdajete li fakturu u programu ili samo želite nacrt/PDF za knjigovođu? (2) Za dvije firme istog klijenta: jedna faktura ili po jedna za svaku? (3) Ko sve izdaje fakture osim vas? (4) Koji je rok plaćanja i jesu li isti za sve klijente?

## 6. Definition of Done

Implementirano i pregledano, testovi prolaze (uključujući podjelu 100 € na 3 firme, storno podijeljene fakture, satnicu iz ručno upisanih sati), kriteriji prihvatanja provjereni, plan ažuriran, release note napisan, isporučeno na test server.
