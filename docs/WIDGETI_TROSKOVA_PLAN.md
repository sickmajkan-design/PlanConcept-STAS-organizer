# Widgeti troškova i finansija — logika i backlog

Autor: Product Owner pogled, 2026-09-24. Status: **ODLUKE VLASNIKA UNESENE (sekcija 8) — spremno za razradu faze 1.**

## 1. Cilj

Direktor u 10 sekundi treba da odgovori na tri pitanja:

1. **Koliko trošimo** — ukupno i po projektu?
2. **Da li zarađujemo** — prihod minus rashod, ukupno i po projektu?
3. **Kako se to kreće** — dnevno, sedmično, mjesečno, ili za period koji sam izabere.

Danas imamo dva widgeta (`CompanyKpi` sa troškom tekućeg mjeseca i `CostTrend` sa zadnjih 6 mjeseci). Oba su **fiksna**: period se ne bira, prihoda nema, projekti se ne razdvajaju.

## 2. Pojmovi (da svi isto govorimo)

| Pojam | Definicija | Izvor u sistemu |
|---|---|---|
| **Rashod** | Sve što je firma potrošila: rad (satnice), kooperanti (dnevnica/paušal), vozila, alat, smještaj, materijal, opšti troškovi | `costsApi.companyReport` (ukupno), `projectReport` (po projektu) |
| **Projektni trošak** | Dio rashoda vezan za konkretan projekat | `projectReport` |
| **Opšti (neprojektni) trošak** | Rashod koji nije vezan ni za jedan projekat (kancelarija, režije…) | `GeneralExpense` |
| **Prihod** | Novac koji je stvarno stigao (ne ugovorena vrijednost). Dvije vrste: **projektni** i **neprojektni** (najam vozila, alata i sl.) | `ProjectRevenue` (projektni); neprojektni **je novi** — vidi sekciju 6 |
| **Kooperant** | Zaposleni sa vrstom `Subcontractor` (`EmployeeType`) — već postoji u sistemu i na listi zaposlenih | postojeće |
| **Plaćanje kooperantu** | Rashod po dogovorenoj cijeni: dnevnica ili paušal po projektu, umjesto satnice | **novo** — vidi sekciju 6 |
| **Zarada** | Prihod − rashod za isti period | izvedeno |
| **Marža** | Zarada / prihod, u % | izvedeno |

Napomena: **ugovorena vrijednost** (`Project.ContractValue`) nije prihod. Prikazuje se samo kao referenca ("naplaćeno 40% ugovora").

## 3. Zajednička kontrola perioda

Svi finansijski widgeti dijele isti izbor perioda, da ne moraš da ga podešavaš na svakom posebno.

**Preseti:** Danas · Ova sedmica · Ovaj mjesec · Prošli mjesec · Ovaj kvartal · Ova godina · **Prilagođeno** (od–do).

**Granulacija grafika** (kako se period dijeli na stubiće):

| Period | Podrazumijevano | Može i |
|---|---|---|
| Danas | po satu ne — jedna vrijednost | — |
| Sedmica | po danu | — |
| Mjesec | po danu | po sedmici |
| Kvartal / godina | po mjesecu | po sedmici |
| Prilagođeno | ≤ 31 dan: po danu; ≤ 6 mjeseci: po sedmici; više: po mjesecu | ručno |

**Pravila:**
- Sedmica počinje **ponedjeljkom**.
- Svaki widget uz iznos prikazuje **poređenje sa prethodnim istim periodom** (▲/▼ %), npr. ovaj mjesec vs prošli. Ako prethodni period nema podataka, prikaže se "—", ne "∞%".
- Izabrani period se pamti po korisniku (kao raspored table), a **ne** po widgetu. Postoji dugme "Vrati na ovaj mjesec".
- Period se piše u naslovu widgeta ("Rashodi — 01.09–24.09.2026"), da snimak ekrana ne laže.
- Datumi se računaju po **datumu nastanka troška** (`OccurredOn`), ne po datumu unosa.

## 4. Widgeti

Postojeća dva se **ne brišu** nego se uklapaju u novi skup (vidi sekciju 7, migracija).

### 4.1 Grupa A — Firma (ukupno)

| # | Widget | Šta prikazuje | Pitanje na koje odgovara |
|---|---|---|---|
| A1 | **Finansije firme — pregled** | 3 pločice: Prihod, Rashod, Zarada (+ marža %), svaka sa ▲/▼ prema prethodnom periodu | Da li smo u plusu? |
| A2 | **Prihod vs rashod** | Grupisani stubići (prihod/rashod) po granulaciji + linija zarade | Kako se kretalo? |
| A3 | **Struktura rashoda** | Prstenasti grafik po kategorijama: rad, vozila, alat, smještaj, materijal, opšti | Na šta odlazi novac? |
| A4 | **Rashod po danu u sedmici / po mjesecu** (potrošnja) | Trend potrošnje sa prosjekom (isprekidana linija) | Da li trošimo iznad uobičajenog? |

### 4.2 Grupa B — Projekti

| # | Widget | Šta prikazuje | Pitanje |
|---|---|---|---|
| B1 | **Top projekata po rashodu** | Horizontalni stubići, prvih 5 (podesivo do 10), klik vodi na projekat | Gdje najviše trošimo? |
| B2 | **Zarada po projektima** | Tabela: projekat · prihod · rashod · zarada · marža; sortiranje po koloni; negativna zarada crvena | Koji projekti donose, a koji gube? |
| B3 | **Projekat u fokusu** | Korisnik bira **jedan** projekat; prikaz: budžet/ugovor, naplaćeno, potrošeno, preostalo, mini-trend, struktura rashoda | Kako stoji taj projekat? |
| B4 | **Projekti van budžeta** (upozorenje) | Lista projekata čiji rashod prelazi prag. Prag se **bira po projektu**: (a) % ugovorene vrijednosti (podrazumijevano 80% upozorenje / 100% crveno) ili (b) posebni budžet projekta, ako je unesen. Budžet ima prednost nad postotkom. | Šta gori? |

B3 je jedini widget kome treba **podešavanje instance** (koji projekat). Zbog toga `instanceId` mora nositi konfiguraciju (sekcija 6).

### 4.3 Šta namjerno NE radimo u ovoj fazi

- Prognoza / predviđanje troška.
- Više valuta (sistem ima jednu valutu).
- Izvoz widgeta u PDF/Excel (postoje izvještaji; widget je za pregled).
- Budžet po kategoriji.

## 5. Kome se šta vidi (prava) — ODLUČENO

**Osnovno pravilo:** finansijski widgeti (prihod, rashod, zarada — svi iz grupa A i B) vide **samo Super Admin**, isto kao Evidencija. Niko drugi ih ne vidi dok ih Super Admin izričito ne otključa.

**Delegiranje:** Super Admin može dozvoliti pristup finansijskim widgetima drugoj ulozi ili pojedinom korisniku (Admin, uprava, ...). Ovo je **novi mehanizam** — danas Evidencija nema dodjelu prava, samo je zaključana na Super Admina.

| Nivo dodjele | Šta znači |
|---|---|
| Po **ulozi** | "Sve uloge Admin vide finansijske widgete" |
| Po **korisniku** | "Samo Marko iz uprave" (izuzetak od uloge) |
| Opseg | **Widgeti i stranice troškova/izvještaja** — jedno pravo "Finansije" (odlučeno) |

**Pravilo "ne pokazuj brojku koju ne možeš otvoriti" — ODLUČENO:**
- Jedno pravo **Finansije** otključava **oba**: widgete i stranice troškova/izvještaja. Nema slučaja da widget pokazuje iznos, a klik na njega vodi na "nemaš pristup".
- **Jedini izuzetak** su **procjene i statistike** izvedene iz tih iznosa i vremena posmatranja (npr. trend ▲/▼, procjena do kraja mjeseca, prosjek po danu, udio kategorije u %). Njih smije vidjeti i korisnik bez prava Finansije — ali **nikad uz apsolutni iznos** iz kog su izvedene (vidi O-7 za granice).
- Widget koji za korisnika ima samo statistiku prikazuje to jasno ("Statistika — bez iznosa"), ne prazne pločice.

Pravila:
- Ono što korisnik ne smije vidjeti **ne nudi se u "Dodaj widget"**, a backend svejedno provjerava svaki poziv (`series`, `by-project`, `breakdown`) — sakrivanje u UI nije zaštita.
- Oduzimanje prava djeluje odmah: widget sa sačuvanog rasporeda se **ne prikazuje** i ne pokušava učitati podatke; raspored ostaje sačuvan pa se widget vrati ako se pravo vrati.
- Dodjela i oduzimanje prava upisuje se u **revizijski log** (ko, kome, kada).
- Samo Super Admin vidi ekran za dodjelu; delegirani korisnik **ne može dalje delegirati**.
- Rukovodilac projekta bez dodijeljenog prava ne vidi ništa od ovoga (raniji prijedlog "svoji projekti" je ukinut dok vlasnik ne odluči drugačije).

## 6. Tehnička logika (za razvoj)

**Problem danas:** ne postoji izvještaj sa više perioda. `CostTrend` pravi 6 paralelnih poziva `companyReport`. Za dnevnu granulaciju mjeseca to bi bilo 31 poziv — neprihvatljivo.

**Novi podaci koje odluke traže (prije widgeta):**

| Nova evidencija | Polja | Napomena |
|---|---|---|
| **Neprojektni prihod** (`CompanyRevenue`) | iznos, datum, **izvor/kategorija** (najam vozila, najam alata, ostalo), opciono veza na vozilo/alat, napomena | Kad je najam vezan za konkretno vozilo/alat, prihod se vidi i na njegovoj kartici (isplati li se ta imovina). Kategorije se mogu dodavati. |
| **Plaćanje kooperantu** (`SubcontractorPayment`) | `employeeId` (mora biti zaposleni vrste `Subcontractor` — server odbija ostale), projekat (opciono), **način**: dnevnica ili paušal, iznos, broj dana (za dnevnicu), datum | Ulazi u rashod projekta ako je vezano za projekat, inače u opšti rashod. Kooperant koji radi po satnici (postojeći `EmployeeRate`) i dalje ulazi kroz evidenciju rada; plaćanje kooperantu je za ugovorenu cijenu. **Isti rad se ne smije brojati dvaput** — vidi O-6. |
| **Budžet projekta** (`Project.Budget`, opciono) | iznos planiranog troška | Nezavisno od `ContractValue` |
| **Pravo pregleda finansija** | uloga/korisnik → dozvoljeno | Vidi sekciju 5 |

**Predlog:** jedan endpoint za seriju, jedan za rang projekata:

```
GET /api/v1/costs/series?from=&to=&granularity=day|week|month&projectId=
→ { buckets: [{ from, to, expense, revenue, profit }],
    totals: { expense, revenue, profit, margin },
    previous: { expense, revenue, profit }        // isti raspon, pomjeren unazad
  }

GET /api/v1/costs/by-project?from=&to=&top=
→ [{ projectId, name, contractValue, revenue, expense, profit }]

GET /api/v1/costs/breakdown?from=&to=&projectId=
→ [{ category, amount }]
```

- `revenue` u `series` = projektni + neprojektni prihod; odgovor ga razdvaja (`revenueProject`, `revenueOther`) da A1/A2 mogu prikazati oba.
- Računanje je na **serveru** (bucketing u SQL-u), klijent samo crta.
- `previous` vraća server, da klijent ne bi računao granice perioda (sedmice, prijestupne godine, DST).
- React Query ključ uvijek sadrži `from/to/granularity`, da widgeti sa istim periodom dijele jedan poziv.
- Osvježavanje: nakon unosa troška/prihoda (već postoji live osvježavanje panela) plus svakih 5 min dok je tab aktivan.
- Konfiguracija instance (npr. `projectId` za B3) čuva se u `DashboardWidgetConfig` kao opciono polje `settings` (JSON). Raspored se već čuva na backendu, pa je to proširenje istog zapisa.
- Zajednički period: jedan `DashboardPeriodProvider` iznad table; widgeti ga čitaju iz konteksta.

## 7. Migracija postojećih widgeta

| Danas | Sutra |
|---|---|
| `CostTrend` | Zamjenjuje ga **A2** (isti tip ostaje u bazi kao alias da se sačuvani rasporedi ne pokvare) |
| `CompanyKpi` (pločica troška) | Pločica troška ostaje, ali čita **zajednički period** umjesto "ovaj mjesec"; ostale pločice (zaposleni, zadaci, flota) se ne diraju |
| `ProjectsRealization` | Ostaje; B2/B3 ga dopunjuju, ne zamjenjuju |

Korisnici sa sačuvanim rasporedom ne dobijaju nove widgete automatski — nalaze ih u "Dodaj widget".

## 8. Odluke vlasnika (2026-09-24)

| # | Pitanje | Odluka |
|---|---|---|
| 1 | Neprojektni prihod | **Postoji** (najam vozila, alata, ...). Nova evidencija `CompanyRevenue`. |
| 2 | Šta ulazi u rashod rada | **Satnice** iz evidencije rada, **i** plaćanje kooperantu po dogovorenoj cijeni (dnevnica ili paušal po projektu). Nova evidencija `SubcontractorPayment`. |
| 3 | Ko vidi finansije | **Samo Super Admin**, uz mogućnost da on dodijeli pravo drugim ulogama/korisnicima (Admin, uprava). |
| 5 | Kooperant | **Zaposleni vrste `Subcontractor`** — koristi se postojeća vrsta, bez posebne evidencije lica. |
| 6 | Obim prava | **Jedno pravo Finansije** otključava widgete **i** stranice; widget ne pokazuje iznos koji korisnik ne može otvoriti. **Izuzetak:** procjene i statistike izvedene iz iznosa i perioda posmatranja. |
| 4 | Budžet projekta | **Oboje, bira se**: prag u % ugovorene vrijednosti ili poseban budžet projekta. |

### Novo otvoreno

- **O-2** — **ODLUČENO (preporuka):** svaka uplata kooperantu je zaseban zapis; paušal je samo oznaka načina.
- **O-3** — **ODLUČENO (preporuka):** način plaćanja je na nivou plaćanja, ne kooperanta; isti kooperant može imati dnevnicu na jednom i paušal na drugom projektu.
- **O-4** — **ODLUČENO (preporuka):** rad po satnici se računa iz evidencije rada, uz oznaku "procjena" dok mjesec nije zaključen.
- **O-6** — **ODLUČENO (preporuka):** `SubcontractorPayment` za dnevnicu ili paušal **isključuje** obračun satnice za isti dan i projekat; server upozori pri unosu preklapanja, da rashod ne bude dupliran.
- **O-7** — **ODLUČENO (preporuka):** bez prava Finansije smije se prikazati samo **trend, procenat promjene i udio u strukturi**; ne smije se prikazati procenat uz vidljivu osnovu (npr. marža uz poznat prihod), niti procjena u iznosu. Statistiku bez iznosa vide samo oni kojima Super Admin dodijeli posebno opcionо pravo **"Finansije — samo statistika"**; nije dostupna svima s pristupom tabli.


## 9. Faze isporuke

| Faza | Sadržaj | Zašto ovim redom |
|---|---|---|
| **0** | **Pravo pregleda finansija** (dva nivoa: "Finansije" i "Finansije — samo statistika"; samo Super Admin + dodjela ulozi/korisniku, revizijski log; jedno pravo za widgete i stranice) | Ništa finansijsko ne smije na tablu prije zaključavanja |
| **1** | `CompanyRevenue` (evidencija + unos), endpointi `series` + `breakdown`, kontrola perioda, **A1, A2** | Odgovara na "da li zarađujemo", sa oba izvora prihoda |
| **2** | `SubcontractorPayment`, `Project.Budget`, `by-project`, **B1, B2** | Rashod postaje potpun tek kad kooperanti uđu; zatim rang projekata |
| **3** | `settings` u konfiguraciji widgeta, **B3**, **A3** | Traži proširenje modela |
| **4** | **A4, B4** (prag % ili budžet) | Zavisi od budžeta iz faze 2 |

## 10. Kriterijumi prihvatanja (faza 1)

- Izbor perioda mijenja sve finansijske widgete na tabli istovremeno.
- Preset "Ova sedmica" počinje ponedjeljkom; "Prilagođeno" odbija `od > do` i period duži od 5 godina.
- Zbir stubića u A2 jednak je ukupnom iznosu u A1 za isti period (na cent).
- Period bez ijednog troška i prihoda prikazuje poruku "Nema podataka za izabrani period", ne prazan grafik ni nule koje izgledaju kao podatak.
- Dnevna granulacija mjeseca radi sa **jednim** mrežnim pozivom.
- Na telefonu widgeti se slažu jedan ispod drugog, a kontrola perioda je na vrhu table i ostaje vidljiva (sticky).
- Novi tipovi widgeta dodati na oba mjesta: `widgetTypes.ts` i `DashboardWidgetTypes` u Application sloju.
- Korisnik bez prava ne vidi finansijske widgete u izboru, ne dobija ih sa sačuvanog rasporeda, a direktan poziv endpointa vraća 403.
- Oduzimanje prava djeluje bez ponovne prijave i upisuje se u revizijski log.
- Prihod od najma vozila/alata unesen u `CompanyRevenue` pojavljuje se u A1 i A2, odvojen od projektnog prihoda.
- Paušal kooperantu vezan za projekat povećava rashod tog projekta u B1/B2 za tačan iznos; dnevnica = iznos × broj dana.
- Projekat sa unesenim budžetom koristi budžet u B4, a bez njega procenat ugovorene vrijednosti.
- Korisnik sa pravom Finansije može klikom na svaki iznos u widgetu otvoriti pripadajuću stranicu; korisnik bez prava ne vidi nijedan iznos, samo dozvoljenu statistiku.
- `SubcontractorPayment` se ne može sačuvati za zaposlenog čija vrsta nije `Subcontractor`.

## 11. Dodatne odluke (2026-09-24)

- Pravo **Finansije** se dodjeljuje **bilo kojoj postojećoj ulozi ili pojedinom korisniku**; nova uloga "Uprava" se ne uvodi.
- **Unos** prihoda firme i plaćanja kooperantima ima samo ko ima pravo Finansije; ostali ne vide ni formu za unos.
- Redoslijed gradnje: **faza 0, pa faza 1.**

## 12. Stanje implementacije — faza 0 (lokalno, nije commitovano)

**Urađeno:**
- `User.FinanceAccess` (`None` / `StatisticsOnly` / `Full`), migracija `AddUserFinanceAccess`. SuperAdmin uvijek dobija `Full`.
- Dodjela je **po korisniku** (isti obrazac kao `CanViewCustomerTaxDetails`), na formi korisnika, samo za SuperAdmina. Promjene se bilježe u revizijskom logu jer je `User` audit-ovan. **Dodjela po ulozi nije urađena** — traži posebnu tabelu postavki; ako je i dalje potrebna, ide kao zasebna stavka.
- Server: `GET /costs/company` traži `Full` (čita se iz baze pri svakom pozivu, pa oduzimanje djeluje odmah).
- Tabla: widget `CostTrend` se ne nudi i ne iscrtava bez prava; pločica troška i link "Troškovi" u `CompanyKpi` isto. Sačuvan raspored se ne briše.
- Testovi: frontend (dashboard) i integracioni (`CompanyCostsTests`) — integracioni **nisu pokrenuti** jer traže bazu (Docker).

**Nije urađeno / za odluku:**
- Ostali izvještaji troškova (`/costs/projects`, stranice troškova) i dalje prate staru provjeru uloga (`ForemanAndAbove`). Da "jedno pravo" zaista pokrije i stranice, treba ih zaključati istim pravom — to mijenja ponašanje za sve Adminе i Rukovodioce koji ih danas koriste.
- Nivo `StatisticsOnly` se može dodijeliti, ali još nema widgeta koji ga koristi (dolazi sa fazom 1).
- Postojeći Admini nakon migracije nemaju pristup (`None`); niko nije automatski dobio `Full`.

## 13. Stanje implementacije — zaključavanje i faza 1 (lokalno, nije commitovano)

**Zaključano pravom Finansije:** `GET /costs/company`, `/costs/projects`, `/costs/projects/{id}/breakdown`, `/costs/vehicles`, `/costs/tools`; stranica Troškovi (ruta i meni); widgeti `CostTrend` i `ProjectsRealization`. Ostaje otvoreno: evidencije pojedinačnih troškova (vozila, alat, ostali troškovi, materijal) i prihodi po projektima (realizacija) i dalje prate staru provjeru uloga.

**Faza 1 — urađeno:**
- **Neprojektni prihod:** entitet `CompanyRevenue` (izvor: najam vozila / najam alata / ostalo, opciona veza na vozilo ili alat), migracija `AddCompanyRevenues`, unos/izmjena/brisanje/lista na `/api/v1/finance/company-revenues`, stranica **Prihodi firme** (meni Troškovi). Sve traži pravo Finansije (Potpun).
- **Serija:** `GET /api/v1/finance/series?from&to&granularity=Day|Week|Month` — jedan poziv za bilo koju granulaciju; rashod svakog stupca je izvještaj troškova firme za datume tog stupca, zbir stupaca = ukupno, uz `previous` (isti broj dana neposredno prije). Granice: 732 dana; najviše 93 dana / 110 sedmica / 30 mjeseci stupaca.
- **Zajednički period** za sve finansijske widgete (preseti + prilagođeno, pamti se po korisniku, dugme za povratak na ovaj mjesec, sticky na telefonu) i naslov widgeta sa datumima.
- **Widgeti:** **A1 Finansije — pregled** (`FinanceOverview`: prihod, rashod, zarada, marža, ▲/▼ prema prethodnom periodu) i **A2 Prihod i rashod** (`IncomeVsExpense`: parovi stubića; zarada ispod grafika).
- Testovi: `FinanceBucketsTests` (5), `periods.test.ts` (13), 3 nova dashboard testa, integracioni `FinanceSeriesTests` (**nisu pokrenuti** — traže bazu).

**Odstupanja od plana:** prilagođeni period je najviše **2 godine** (ne 5) zbog ograničenja izvještaja troškova; profit nije linija na grafiku nego broj ispod (x-charts kompozicija bi tražila veći zahvat). Rashod po stupcu se računa serverski poziv-po-stupcu (najviše ~110) — ako postane sporo, sljedeći korak je jedan SQL upit po kategoriji.

**Nije urađeno (faza 2+):** `SubcontractorPayment`, `Project.Budget`, `by-project`, B1–B4, A3, A4, nivo "samo statistika" u widgetima.

## 14. Zaključavanje pojedinačnih evidencija (lokalno, nije commitovano)

Atribut `[FinanceAccess]` na akcijama (čita pravo iz baze pri svakom pozivu, 403 bez njega): **plate/satnice** (`employee-rates`), **ostali troškovi** (`general-expenses`), **ručne isplate** (`finance-entries`), **prihodi po projektima** (`project-revenues`) i **godišnja realizacija** (`projects/annual-realization`). Stranice i stavke menija za to su sakrivene bez prava; "Postavke obračuna" bez prava otvara Praznike.

**Namjerno NIJE zaključano:** troškovi vozila i alata, najam vozila/alata (i njihove cijene), materijal i cijene smještaja. Mobilna aplikacija ih koristi za unos goriva i najma na terenu; zaključavanje bi je pokvarilo za predradnike i radnike. Ako se žele zaključati, treba prvo odvojiti unos (ostaje otvoren) od pregleda iznosa (zaključan).

## 15. Faza 2 (lokalno) — kooperanti, budžet, widgeti po projektu

**Odstupanje od plana — nema nove evidencije `SubcontractorPayment`.** Postojeća `FinanceEntry` (ručna isplata) već ima ono što je plan tražio: zaposleni, projekat, datum, iznos i vrstu **paušal** (`WorkerPaymentFixed`) ili **dnevnica** (`WorkerPaymentDaily`), i već ulazi u trošak firme. Druga evidencija bi značila dva izvora istine za isti novac. Kooperant je i dalje zaposleni vrste `Subcontractor`; unosi se na stranici Ručne isplate.

**O-6 implementirano:** paušal ili dnevnica **kooperanta** za projekat i dan isključuje njegove evidentirane sate za taj projekat i dan iz obračuna rada (`ProjectLabourPricing`), pa se isti rad ne broji dvaput — i u trošku firme i u trošku projekta. Satnični unos (`WorkerPaymentHourly`) je korekcija sata i ne isključuje ništa; isto važi za paušal/dnevnicu običnog zaposlenog.

**Budžet projekta:** `Project.Budget` (opciono, ≥ 0), migracija `AddProjectBudget`. Namjerno **nije** u DTO-ovima projekta (da ga ne vidi ko nema pravo Finansije); čita se i piše samo kroz `GET/PUT /api/v1/finance/projects/{id}/budget`, a kartica **Budžet** na stranici projekta prikazuje se samo uz pravo.

**Po projektu:** `GET /api/v1/finance/by-project?from&to&top` — prihod (naplata po ugovoru), rashod (izvještaj troškova projekta + paušal/dnevnica kooperanata), zarada, marža, ugovor i budžet; najviše 100 redova, najveći rashod prvi.

**Widgeti:** **Projekti po rashodu** (`TopProjectsByExpense`, prvih 5, klik vodi na projekat) i **Zarada po projektima** (`ProfitByProject`, prvih 10, sortiranje po koloni, gubitak crveno, prazna marža kao crtica). Oba prate zajednički period i traže pravo Finansije.

**Poznato ograničenje:** izvještaj troškova **projekta** ne uključuje ručne isplate u svoj `Total` (namjerno, zbog dupliranja sa satima), dok trošak **firme** uključuje sve ručne isplate. Zbir projekata zato ne mora biti jednak trošku firme. Po projektu se dodaje samo paušal/dnevnica kooperanata, jer ta više ne duplira sate.

**Testovi:** integracioni (nisu pokrenuti — nema baze): isključivanje sati (paušal, dnevnica, satni unos, običan zaposleni), `by-project`, budžet i prava. Frontend: 3 nova testa widgeta.

**Nije urađeno (faza 3–4):** `settings` po widgetu i B3 Projekat u fokusu, A3 struktura rashoda, A4 trend potrošnje, B4 upozorenje van budžeta (prag % ugovora ili budžet), nivo "samo statistika".

