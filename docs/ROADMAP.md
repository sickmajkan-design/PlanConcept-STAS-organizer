# Roadmap — koje module dodati / Which modules to add next

Ovaj dokument nastavlja se na `PRODUCTION_READINESS_AUDIT.md`. Audit je merio da li
je *ono što imamo* spremno za produkciju. Ovaj dokument meri **šta nam još fali u
odnosu na tržište** i kojim redom to graditi.

This document follows on from `PRODUCTION_READINESS_AUDIT.md`. The audit measured
whether *what we have* is production-ready. This one measures **what is still
missing against the market** and in which order to build it.

---

## Polazna tačka / Starting point

**SR** — Poređenje sa STAS Organizerom (rekonstruisano iz njihove stranice
„Značajke", opisa u prodavnicama aplikacija i domaćeg PR-a; njihov sajt nije bio
dostupan iz razvojnog okruženja) daje **≈27% pokrivenosti**: 4 pune funkcije, 6
delimičnih, 15 nedostajućih od 26 posmatranih oblasti.

Struktura razlike je važnija od broja. Mi imamo **registar** — ko, šta i gde
postoji. Njima povrh toga radi **transakcioni sloj** — šta se desilo, koliko je
koštalo i šta treba uraditi. Ušteda koju oni prodaju („1–2 sata administracije
dnevno po osobi") dolazi iz tog drugog sloja, ne iz prvog.

**EN** — The comparison against STAS Organizer (reconstructed from their features
page, app-store listings and local press; their site was unreachable from the
development environment) puts us at **≈27% coverage**: 4 complete, 6 partial, 15
missing out of 26 areas.

The shape of the gap matters more than the number. We have the **registry** — who
and what exists, and where. They additionally run the **transactional layer** —
what happened, what it cost, and what needs doing. The saving they sell ("1–2
hours of admin per person per day") comes from that second layer, not the first.

---

## Princip prioritizacije / Prioritisation principle

Redosled nije „po vrednosti" nego **po zavisnostima pa po vrednosti**. Tri pravila:

1. **Popravi polomljeno pre nego što dodaš novo.** Dve od devet isporučenih
   funkcija ne rade u produkciji. Deseta funkcija to ne popravlja, a demo obara.
2. **Prvo ono što otključava ostalo.** Skladište fajlova otključava tri modula.
   Radni sati su izvor podataka za svaki kasniji izveštaj o troškovima.
3. **Skupo i integraciono ide na kraj.** Računi, narudžbe i chat traže tuđe
   sisteme ili realtime infrastrukturu; vrednost po danu rada im je najniža.

Ordering is not "by value" but **by dependency, then value**. Three rules:

1. **Fix what is broken before adding what is new.** Two of nine shipped modules
   do not work in production. A tenth module does not fix that, and it sinks a demo.
2. **Build what unlocks the rest first.** File storage unlocks three modules.
   Work hours are the data source for every later cost report.
3. **Expensive and integration-heavy goes last.** Invoices, orders and chat need
   third-party systems or realtime infrastructure; their value per day of work is
   the lowest on the list.

---

## Faza 0 — Popravi polomljeno / Fix what is broken

**Nema novih funkcija. / No new features.**

| Stavka / Item | Problem | Posao / Work |
|---|---|---|
| GPS u pozadini / Background GPS | Lokacija se šalje samo dok je aplikacija na ekranu (`location_tracking_controller.dart:105`). Za praćenje na terenu neupotrebljivo. | Foreground service (Android) + background location (iOS), red za slanje, dozvole, baterija |
| Push / Firebase | Kod postoji, projekat nije provizioniran — nijedna notifikacija ne izlazi. | FCM projekat, `google-services.json`, APNs ključ, end-to-end provera |
| Potpisivanje Android builda / Android signing | Nema release keystore-a. | Keystore, `key.properties` van repozitorijuma, CI korak |

**Zašto prvo / Why first:** ovo su jedine dve funkcije koje *tvrdimo* da imamo a
ne rade. Sve dalje na listi (podsetnici o isteku dokumenata, dodela zadatka,
prijava na gradilište) oslanja se na push ili GPS.

**Procena / Estimate:** 1–2 nedelje. **Pokrivenost posle / Coverage after: ≈31%**

---

## Faza 1 — Radni sati / Work hours

Ključni modul. / The keystone module.

- Entitet `TimeEntry`: zaposleni, projekat, početak, kraj, pauza, tip sata
  (redovan / prekovremeni / vikend), status odobrenja, ko je odobrio
- Mobilno: prijava i odjava sa gradilišta, sa GPS pečatom iz Faze 0
- Admin: nedeljna mreža po radniku i projektu, masovno odobravanje, ispravke
- Pravila: preklapanje unosa, unos unazad, zaključavanje odobrenog perioda

**Zašto drugo / Why second:** najveća dnevna ušteda administracije; jedini razlog
da radnik otvori aplikaciju svaki dan; i izvor podataka bez kojeg statistika
troškova (Faza 5) ne postoji.

**Procena / Estimate:** 2–3 nedelje. **Pokrivenost / Coverage: ≈35%**

---

## Faza 2 — Dokumenti i fotografije / Documents and photos

Nova infrastruktura — `Infrastructure/` trenutno **nema** sloj za fajlove.
New infrastructure — `Infrastructure/` currently has **no** file layer.

- `IFileStorage` apstrakcija + S3-kompatibilna implementacija (MinIO lokalno)
- Upload/download sa potpisanim URL-ovima, ograničenje tipa i veličine,
  provera vlasništva pri preuzimanju
- Prilog na `Employee`, `Project`, `Tool`, `Vehicle`
- Dokumenti radnika sa datumom isteka: ugovori, sertifikati, lekarski pregledi
- **Podsetnik na istek preko push-a** (koristi Fazu 0)

**Zašto treće / Why third:** jedan sloj otključava tri modula — dokumenta radnika,
gradilišnu dokumentaciju i fotografije nedostataka (Faza 3). Podsetnici na istek
sertifikata su neposredna korist za usklađenost.

**Procena / Estimate:** 2 nedelje. **Pokrivenost / Coverage: ≈42%**

---

## Faza 3 — Zadaci i nedostaci / Tasks and defects

- `Task`: dodeljen, rok, projekat, prioritet, status → **push pri dodeli i pred rok**
- `Defect`: zadatak vezan za projekat, sa fotografijama iz Faze 2 i pozicijom
- Lista „moje danas" na mobilnom

**Zašto četvrto / Why fourth:** koristi obe prethodne faze i tek ovde push postaje
smislen — do sada bi slao notifikacije ni o čemu. Nedostaci (punch list) su
standardna očekivana funkcija u građevini.

**Procena / Estimate:** 2 nedelje. **Pokrivenost / Coverage: ≈50%**

---

## Faza 4 — Raspoređivanje i odsustva / Scheduling and absences

- Nadogradnja `EmployeeProject`: sada nosi samo `AssignedAt`. Treba raspon datuma,
  pa jedan radnik može biti na različitim gradilištima kroz nedelju
- Kalendarski prikaz u admin panelu, prevlačenje radnika i timova po danu
- Godišnji odmori, bolovanja i dostupnost — da kalendar prikazuje ko *stvarno* može
- Objedinjen profil radnika: sve što je zadužio na jednom mestu

**Zašto peto / Why fifth:** ovo je njihova reklamna funkcija (drag-and-drop
raspoređivanje), ali bez modula sati raspored je samo slika — ne zna se šta je
zaista odrađeno.

**Procena / Estimate:** 2–3 nedelje. **Pokrivenost / Coverage: ≈58%**

---

## Faza 5 — Troškovi i statistika / Costs and statistics

- Servisi vozila i evidencija goriva (sada imamo samo `FuelType` enum)
- Promet materijala — ulaz/izlaz i nabavna cena (sada samo trenutno stanje)
- Obračun: sat × cena rada + materijal + gorivo + servis → **trošak po projektu,
  vozilu i radniku**
- Izvoz u Excel kao presečna funkcija nad svim listama

**Zašto šesto / Why sixth:** ovo je ono zbog čega vlasnik firme plaća pretplatu,
ali se ne može izgraditi pre nego što podaci iz Faza 1 i 5 postoje. Graditi
statistiku ranije znači graditi prazne grafikone.

**Procena / Estimate:** 3 nedelje. **Pokrivenost / Coverage: ≈75%**

---

## Faza 6 — Odloženo / Deferred

| Stavka / Item | Zašto kasnije / Why later |
|---|---|
| Narudžbe i računi / Orders and invoices | Traži vezu sa knjigovodstvom; regulatorno različito po tržištu |
| Usluge i raspored usluga / Services scheduling | Vredno tek firmama koje rade održavanje, ne opštoj građevini |
| Investitorski pristup / Investor access | Jeftino *posle* resource-scoped autorizacije (H11 iz audita) — do tada rizik curenja podataka |
| Offline rad / Offline mode | Skupo, samo mobilno, dira svaki ekran |
| Chat | Realtime infrastruktura; zadaci + notifikacije pokrivaju 80% potrebe za deo cene |
| iOS build | Traži Apple Developer nalog i Mac za potpisivanje |

---

## Šta svaki novi modul mora da ispoštuje / Definition of done per module

Svaki modul odavde nasleđuje postojeće konvencije — nije završen dok nema sve:
Every module from here inherits the existing conventions — not done until it has all of:

1. Entitet + EF konfiguracija + **migracija** (trenutno postoji samo `InitialCreate`)
2. CQRS handleri sa FluentValidation, `ProjectTo` za liste, soft delete
3. Kontroler sa autorizacijom po ulozi; provera preko `RoleAdministration` gde je bitno
4. Admin ekran (`src/construction_admin/src/pages/`) i mobilni ekran
   (`src/construction_mobile/lib/features/`) — koristiti `createCrudApi`,
   `ResourceDataGrid`, `FilteredPagedListNotifier`
5. **Dvojezično: `en.ts` + `sr.ts` i `app_en.arb` + `app_sr.arb`.** Pažnja na
   `EnumKind` — ista engleska reč traži različit srpski oblik po entitetu
   (`Available` → „Slobodno" za vozilo, „Slobodan" za alat)
6. Unit testovi validatora + integracioni test protiv prave baze

---

## Sažetak / Summary

| Faza / Phase | Sadržaj / Content | Trajanje / Duration | Pokrivenost / Coverage |
|---|---|---|---|
| 0 | GPS, push, potpisivanje | 1–2 ned. | 31% |
| 1 | Radni sati | 2–3 ned. | 35% |
| 2 | Dokumenti i fotografije | 2 ned. | 42% |
| 3 | Zadaci i nedostaci | 2 ned. | 50% |
| 4 | Raspoređivanje i odsustva | 2–3 ned. | 58% |
| 5 | Troškovi i statistika | 3 ned. | 75% |
| 6 | Narudžbe, računi, offline, chat, iOS | otvoreno / open | ~100% |

**Ukupno do 75% pokrivenosti: ~3 meseca za jednog programera.**
**Total to 75% coverage: ~3 months for one developer.**

Procene su za jednog programera preko sva tri koda (API, admin, mobilna) i
uključuju testove i prevode — ne uključuju dizajn, QA ni pregovore sa klijentom.
Estimates are for one developer across all three codebases (API, admin, mobile)
and include tests and translations — they exclude design, QA and client
negotiation.
