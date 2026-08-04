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

## Faza 0 — Popravi polomljeno / Fix what is broken — **ODRAĐENO / DONE**

**Nema novih funkcija. / No new features.** Detalji koraka koje mora uraditi
vlasnik: `PROVISIONING.md`. / Owner-only steps: see `PROVISIONING.md`.

| Stavka / Item | Problem | Stanje / State |
|---|---|---|
| GPS u pozadini / Background GPS | Lokacija se slala samo dok je aplikacija na ekranu — tajmer prestaje da radi čim aplikacija ode u pozadinu. | **Odrađeno.** Position stream uz Android foreground servis i Apple background updates. Ne pokriva slučaj kada korisnik ručno ukloni aplikaciju — vidi `PROVISIONING.md` §3. |
| Red za slanje / Send queue | Bafer je bio samo u memoriji: sve uhvaćeno tokom nestanka mreže gubilo se kad Android povrati proces. | **Odrađeno.** Trajni red, ograničen na jedan batch i 12 sati starosti. |
| Push / Firebase | Kod postoji, projekat nije provizioniran. | **Kod i konfiguracija odrađeni** (Gradle plugin uslovno, `Firebase__CredentialsJson` kroz compose). Sam Firebase projekat mora napraviti vlasnik. |
| Potpisivanje Android builda / Android signing | Nije bilo release keystore-a. | **Odrađeno** — čita se iz `android/key.properties` van repozitorijuma, uz pad na debug ključ da build i dalje prolazi. Keystore pravi vlasnik. |

**Zašto prvo / Why first:** ovo su jedine dve funkcije koje *tvrdimo* da imamo a
ne rade. Sve dalje na listi (podsetnici o isteku dokumenata, dodela zadatka,
prijava na gradilište) oslanja se na push ili GPS.

**Procena / Estimate:** 1–2 nedelje. **Pokrivenost posle / Coverage after: ≈31%**

---

## Faza 1 — Radni sati / Work hours — **ODRAĐENO / DONE**

Ključni modul. / The keystone module.

| Stavka / Item | Stanje / State |
|---|---|
| Entitet `TimeEntry` + migracija | **Odrađeno.** Smena u toku je red bez kraja, ne zasebna tabela. Parcijalni jedinstveni indeks dozvoljava tačno jednu otvorenu smenu po radniku; check ograničenja odbijaju negativnu pauzu i kraj pre početka. Sva četiri proverena direktno na PostgreSQL-u. |
| Mobilno: prijava/odjava sa GPS pečatom | **Odrađeno.** Kartica smene sa proteklim vremenom, lista sopstvenih unosa. Pozicija je dokaz kad je ima, nikad uslov — rad u podrumu mora da se evidentira. |
| Admin: pregled, odobravanje, ispravke | **Odrađeno.** Lista sa filterima „čeka pregled" i „u toku", odobri/vrati na doradu, ručni unos i ispravka. |
| Zbir sati po zaposlenom | **Odrađeno.** Agregacija u bazi (`GROUP BY`), ne prolaskom kroz stranice. |
| Pravila | **Odrađeno.** Preklapanje, granica unosa unazad (31 dan), maksimalna smena (16 h), zaključavanje odobrenog, i zabrana odobravanja sopstvenih sati. |

**Nije rađeno / Not built:** nedeljna mreža kao tabela dan-po-dan. Zbir po
zaposlenom za period odgovara na isto pitanje uz znatno manje posla; mreža se
može dodati kasnije bez izmena na API-ju.

**Pokrivenost / Coverage: ≈35%**

---

## Faza 2 — Dokumenti i fotografije / Documents and photos — **ODRAĐENO / DONE**

| Stavka / Item | Stanje / State |
|---|---|
| `IFileStorage` + dve implementacije | **Odrađeno.** Disk je podrazumevan, pa klon i CI rade bez ijednog spoljnog servisa; podešavanje bucket-a prebacuje na bilo koji S3-kompatibilan servis. |
| Upload/download, ograničenja tipa i veličine | **Odrađeno.** 20 MB, allow-lista ekstenzija, tip se izvodi iz ekstenzije a ne iz zahteva. |
| Prilog na `Employee`, `Project`, `Vehicle`, `Tool` | **Odrađeno.** Četiri nullable strana ključa uz check ograničenje „tačno jedan", pa kaskada i integritet i dalje rade. |
| Dokumenti sa datumom isteka | **Odrađeno.** Ugovori, sertifikati, lekarski pregledi, licence, osiguranja. |
| Podsetnik na istek preko push-a | **Odrađeno.** Dnevni prolaz, idempotentan — druga replika ne javlja isto dvaput. |
| Mobilno: pregled + slikanje gradilišta | **Odrađeno.** Radnik može da doda fotografiju na projekat i ništa drugo. |

**Odstupanje od plana / Deviation:** umesto potpisanih URL-ova, API prenosi
bajtove u oba smera. Potpisan link izlazi iz autorizacije koja ga je izdala —
ko ga ima, ima i fajl do isteka. Ovde su u pitanju dokumenti i fotografije, ne
video, pa je jedan odgovor na pitanje „sme li ovaj korisnik ovaj fajl" vredniji
od uštede propusnog opsega. Potpisivanje ostaje moguće kasnije iza istog
interfejsa.

**Nova zavisnost / New dependency:** `AWSSDK.S3` (server) i `image_picker`
(mobilna). Obe su neophodne za navedene funkcije — S3 protokol se ne piše
ručno, a fotografija se bez pickera ne može uzeti.

**Pokrivenost / Coverage: ≈42%**

---

## Faza 3 — Zadaci i nedostaci / Tasks and defects — **ODRAĐENO / DONE**

| Stavka / Item | Stanje / State |
|---|---|
| `WorkItem` — jedna tabela za oba | **Odrađeno.** Nedostatak je zadatak sa mestom i fotografijom; sve ostalo je isto, pa bi dve tabele značile duple upite, ekrane i notifikacije zbog jednog polja. |
| Pravila u bazi | **Odrađeno.** Nedostatak mora imati gradilište; pozicija je cela ili je nema. Oba proverena na PostgreSQL-u. |
| Životni ciklus | **Odrađeno.** Prelazi su tabela, ne lanac `if`-ova. Zatvaranje traži nadređenog — to je provera da je posao urađen, pa nije poziv iste osobe koja ga je radila. |
| Fotografije nedostatka | **Odrađeno.** Peti vlasnik priloga, tačno put predviđen u Fazi 2 — fotografija nestaje sa nedostatkom. |
| Push pri dodeli i pred rok | **Odrađeno.** Dnevni prolaz uz isti idempotentni obrazac; pomeren rok briše oznaku, pa se novi datum najavljuje. |
| Mobilno: „moji zadaci" + prijava nedostatka | **Odrađeno.** Radnik pomera svoje stavke i prijavljuje nedostatak sa GPS pečatom. |

**Pokrivenost / Coverage: ≈50%**

---

## Faza 4 — Raspoređivanje i odsustva / Scheduling and absences — **ODRAĐENO / DONE**

| Stavka / Item | Stanje / State |
|---|---|
| `EmployeeProject` sa rasponom datuma | **Odrađeno.** Raspored je sada raspon, ne članstvo: radnik se kroz nedelju seli sa gradilišta na gradilište, vraća se kasnije kao drugi raspored, i može pokrivati dva gradilišta odjednom — poslednje namerno, jer nadzornik to stvarno radi. |
| Migracija bez gubitka podataka | **Odrađeno.** `Id` iz `gen_random_uuid()`, `StartDate` izveden iz `AssignedAt`. Provereno na bazi sa podacima, ne pretpostavljeno. |
| Preklapanja kao ograničenja baze | **Odrađeno.** `EXCLUDE USING gist` nad `daterange` za oba slučaja, pa dva zahteva u trci ne mogu oba proći. Rukovaoci proveravaju samo da bi odgovor bio rečenica. |
| Skidanje sa gradilišta | **Odrađeno.** Zatvara raspored današnjim danom umesto da ga briše — radnik je bio tamo, a satnica pored toga to i kaže. Raspored koji nije počeo se briše. |
| Odsustva sa odobrenjem | **Odrađeno.** Zahtev dok ga neko ne odgovori. Sopstveno odsustvo ne odobrava niko, bez obzira na ulogu — ista provera koju satnica već nosi. |
| Tabla rasporeda u admin panelu | **Odrađeno.** Nedelja po nedelja, jedan upit za celu tablu. Na tabli je samo odobreno odsustvo; zahtev bez odgovora bi značio da se posao planira oko slobodnih dana koje niko nije dao. |
| Mobilno: moj raspored i moja odsustva | **Odrađeno.** Isti `/api/schedule`, server suzi radnika na njegov red. Radnik traži odsustvo i povlači zahtev bez odgovora; odobreno mora nadređeni da odbije. |

**Poznati nedostaci / Known gaps:**

- Push obaveštenje kad se odsustvo odobri ili odbije **ne postoji**. To je i
  dalje tako za sve preglede u sistemu (satnica takođe ne šalje), pa je ovde
  ostavljeno dosledno — ali radnik trenutno mora sam da otvori aplikaciju da
  bi video odgovor. Prvi kandidat za dopunu.
- Prevlačenje mišem po tabli (drag-and-drop) nije urađeno. Tabla prikazuje i
  filtrira; raspoređivanje ide preko ekrana radnika. Njihova reklamna funkcija,
  ali ne menja šta sistem zna.
- Poruke validacije formi su i dalje samo na engleskom — na sva tri sloja
  (FluentValidation, zod, ARB nije u pitanju). Nije uvedeno ovom fazom;
  pogađa svih devet postojećih formi jednako.

**Pokrivenost / Coverage: ≈58%**

---

## Faza 5 — Troškovi i statistika / Costs and statistics — **ODRAĐENO / DONE**

| Stavka / Item | Stanje / State |
|---|---|
| Cena rada sa datumom | **Odrađeno.** Svi su dobili povišicu u junu; martovski izveštaj i dalje mora reći koliko je mart koštao. Jedna kolona sa „trenutnom cenom" bi nečujno prepisala svaki ranije odrađen izveštaj, a broj bi i dalje izgledao uverljivo. Nova cena zatvara prethodnu, preklapanja odbija ograničenje baze. |
| Promet materijala | **Odrađeno.** Stanje odgovara samo na „koliko je ostalo". Ne može reći koliko je potrošeno ni koje gradilište — a to su dva pitanja zbog kojih izveštaj postoji. Stanje ostaje kao keš zbira; promet i izmena stanja idu u istoj transakciji, pa magacinski ekran ne može da odluta od istorije. |
| Vrednovanje izdatog materijala | **Odrađeno.** Po proseku dosadašnjih nabavki, i taj broj se **upisuje** na red. Da se računa u trenutku izveštaja, nabavka sledećeg meseca po drugoj ceni bi promenila koliko je završen posao koštao. |
| Gorivo i servisi vozila | **Odrađeno.** Jedna tabela: gorivo, servis, popravka, osiguranje i registracija se razlikuju u dva polja a slažu u svemu ostalom, i pitanje „koliko nas je ovaj kombi koštao" ih ionako sabira. Izveštaj razdvaja gorivo nazad i računa l/100 km — broj koji zaista otkriva kvar ili tuđu upotrebu kartice. |
| Izveštaj po gradilištu i vozilu | **Odrađeno.** Samo odobreni sati ulaze u trošak; neodobreni su tvrdnja, ne trošak. Sati koje nijedna cena ne pokriva se **prijavljuju**, ne prećutkuju — zbir koji tiho izostavi trećinu ekipe izgleda isto kao onaj koji ne izostavlja. |
| Uloge drugačije nego drugde | **Odrađeno.** Poslovođa evidentira nabavku i vidi šta je njegovo gradilište potrošilo, ali cena rada je tuđa plata, pa mu izveštaj vraća taj deo kao nulu umesto da odbije ceo izveštaj. |
| Mobilno: trošak sa pumpe | **Odrađeno.** Jedino mesto u ovom modulu gde telefon pobeđuje kancelariju: čovek koji toči stoji pored računa i kilometraže, a sve što mora da zapamti biva upisano pogrešno ili nikako. |

**Poznati nedostaci / Known gaps:**

- **Izvoz u Excel nije urađen.** Bio je u planu faze kao presečna funkcija nad
  svim listama; ostaje neurađen i nije zamenjen ničim.
- Trošak vozila se ne raspoređuje po gradilištima. Kombi nije raspoređen na
  gradilište kao čovek, pa bi svako pripisivanje bilo izmišljena raspodela koju
  podaci ne podržavaju. Vozni park ima svoj izveštaj.
- Valuta nigde ne piše. Sistem čuva jednu valutu i ne kaže koju; klijenti zato
  ne štampaju oznaku umesto da izmisle pogrešnu.
- Vrednovanje je prosečna cena, ne FIFO. Gomila šljunka nema serije da se troše
  po redu, a FIFO bi tražio tabelu slojeva zarad pitanja koje na gradilištu niko
  ne postavlja.

**Pokrivenost / Coverage: ≈75%**

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
| 0 | GPS, push, potpisivanje — **odrađeno** | 1–2 ned. | 31% |
| 1 | Radni sati — **odrađeno** | 2–3 ned. | 35% |
| 2 | Dokumenti i fotografije — **odrađeno** | 2 ned. | 42% |
| 3 | Zadaci i nedostaci — **odrađeno** | 2 ned. | 50% |
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
