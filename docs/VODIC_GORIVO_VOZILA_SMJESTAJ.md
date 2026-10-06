# Vodič: gorivo, DKV izvod, datumi vozila i smještaj tokom odsustva

Za administratore i Super Admina; dijelovi 2 i 7 su i za radnike. Opisuje ono što je dodato 6. oktobra 2026: TD broj vozila, unos goriva sa telefona, provjeru DKV izvoda, datume koji ističu, skidanje sa smještaja tokom odsustva i Ctrl+K pretragu.

Aplikacija na telefonu: unos goriva traži verziju **1.1.18**, a TD i datumi vozila u aplikaciji verziju **1.1.19** ili noviju.

---

## 1. TD broj vozila

**TD** je interni broj vozila u firmi, onaj koji DKV štampa na izvodu uz karticu (npr. `15`).

* Obavezan je pri dodavanju i izmjeni vozila, i **jedinstven** među aktivnim vozilima.
* Vozila koja su postojala prije nemaju TD. Na njihovom kartonu piše upozorenje, a **dok se TD ne upiše, vozilo se ne može ponovo snimiti** (ni u panelu ni u aplikaciji).
* Vidi se u zaglavlju vozila (`ZG-1234 · TD 15`), kao kolona u listi, i po njemu se pretražuje.
* TD služi za provjeru DKV izvoda: broj na izvodu mora odgovarati vozilu na koje je kartica upisana.

## 2. Unos goriva (telefon)

Gorivo evidentira osoba koja toči: **Predradnik i iznad**, a **radnik** samo za vozilo koje je zaduženo na njega (zaduženje: skeniraj QR kod vozila, "Zaduži").

Meni > **Troškovi vozila** > **Evidentiraj trošak**. Za gorivo je obavezno:

| Polje | Napomena |
|---|---|
| Vozilo | Radniku se nudi samo vozilo koje drži. Ako nema nijedno, piše da ga prvo mora zadužiti |
| Iznos | Iznos s računa |
| Litri | Veći od nule |
| Kilometraža | Stanje na brojilu |
| Slika računa | Kamera ili galerija. Šalje se kao prilog tog troška |

Dugme "Evidentiraj" ostaje neaktivno dok sve ne bude uneseno. Ako se trošak snimi a slika ne prođe, aplikacija to kaže; sljedeći pritisak šalje samo sliku, pa se gorivo ne evidentira dvaput.

Radnik vidi samo svoja točenja. Popravke, servis i ostalo evidentira Predradnik i iznad; ostatak modula troškova radniku ostaje zatvoren.

> Starije verzije aplikacije i web forma ne traže kilometre i sliku. Takav unos DKV provjera označi (vidi 3.4).

## 3. Provjera DKV izvoda

Mjesto: **Troškovi vozila** > dugme **Provjera DKV izvoda** (ili Ctrl+K > "Provjera DKV izvoda"). Samo Admin i Super Admin.

### 3.1 Princip

**Unos vozača je evidencija** (litri, kilometri, slika računa). **DKV izvod ga provjerava**: izvod nema litara, pa se ništa ne prepisuje iz njega, nego se svaki red izvoda uparuje s unosom vozača.

Uparivanje ide po: **vozilo** (preko kartice) + **dan** + **iznos**. Dan prije i dan poslije se tolerišu (točenje oko ponoći često upišu pod prethodni dan). Dva točenja istog dana uparuju se svako sa svojim unosom po iznosu. Jedan unos vozača može potvrditi samo jedan red izvoda.

### 3.2 Tok

1. **Učitaj** DKV CSV (ili .xlsx), onaj izvezen iz DKV portala. Kolone se prepoznaju po nazivu, ne po redoslijedu.
2. **Pregled** prije snimanja: koliko je novih, koliko već u sistemu, koliko redova je podudarno, za provjeru, bez unosa vozača ili s nepoznatom karticom. Nepoznate kartice se mogu odmah dodijeliti vozilu.
3. **Uvezi izvod**. Isti fajl učitan dvaput ne dodaje ništa. Red koji je bio *Ni obračunano*, a kasnije stigne kao *Obračunano*, ažurira se na mjestu.
4. Redovi koji se ne slažu čekaju na listi **Redovi izvoda**. Admini dobiju **obavještenje** (osim onoga ko je učitao).

### 3.3 Stanja redova

| Stanje | Znači |
|---|---|
| Podudarno | Postoji unos vozača za to vozilo, dan i iznos. Ništa se ne radi |
| Za provjeru | Postoji razlika; uz red piše koja |
| Nema unosa vozača | Kartica je poznata, ali niko nije evidentirao to točenje (možda vozač kasni) |
| Nepoznata kartica | Broj kartice nije upisan ni na jedno vozilo |
| Riješeno / Preskočeno | Neko je odlučio ručno; razlog je sačuvan |

### 3.4 Šta se označava kao razlika

* **Drugi iznos**: vozač je za taj dan evidentirao drugi iznos (prikazuju se oba).
* **TD ne odgovara**: izvod kaže TD drugog vozila nego što je to na kojem je kartica, ili vozilo još nema TD. Oznaka "SD SMART" na izvodu je opća i ne provjerava se.
* **Pogrešno gorivo**: benzin na dizel vozilu i obrnuto. AdBlue se ne provjerava, hibridi su izuzeti.
* **Nepotpun unos**: unos se slaže, ali nema kilometraže ili slike računa.

### 3.5 Šta admin može uraditi s redom

| Radnja | Kada | Razlog |
|---|---|---|
| **Spoji s unosom** | Biraš između unosa tog vozila ±3 dana koji nisu već spojeni | |
| **Potvrdi kako jeste** | Razliku prihvataš | **Obavezan** |
| **Prihvati bez unosa** | Nema unosa vozača, a točenje je valjano | **Obavezan** |
| **Evidentiraj iz izvoda** | Niko nije unio točenje: pravi se trošak goriva iz reda izvoda. Upisuješ **litre** (obavezno, jer gorivo bez litara nije dozvoljeno) i kilometre (opciono) | |
| **Preskoči** | Red se ne računa nikome | **Obavezan** |
| **Dodijeli karticu** | Nepoznata kartica: bira se vozilo, pa se redovi te kartice ponovo uparuju | |
| **Provjeri ponovo** | Poslije kašnjenja vozača: uparuje sve otvorene redove iznova | |

Poslije dodavanja slike računa ili kilometara na unos, "Provjeri ponovo" (ili sljedeći uvoz) prebacuje red u podudaran. Otvoreni redovi se ponovo uparuju i pri svakom novom uvozu.

### 3.6 Trag

Svaka izmjena reda ide u **revizijski trag** (Administracija > Revizija): ko, kada, stara i nova vrijednost. Svaki uvoz se pamti (fajl, ko, kada, koliko novih, ažuriranih, duplikata) i vidi se pod **Raniji uvozi**. Izvorni red iz izvoda se čuva neizmijenjen.

### 3.7 Prije prvog uvoza

1. Svako vozilo mora imati **TD broj** (1).
2. Svaka DKV kartica mora biti upisana na svoje vozilo (kartice vozila u panelu). Kartice koje nedostaju možeš dodijeliti i tokom uvoza.
3. Vozači moraju imati vozilo zaduženo da bi mogli evidentirati gorivo.

> Stari "Uvoz goriva" (koji traži litre u fajlu) i dalje postoji, ali ne radi s DKV izvozom. Koristi provjeru DKV izvoda.

## 4. Datumi vozila i podsjetnici

U formi vozila: **Registracija važi do**, **Tehnički pregled važi do**, **Osiguranje važi do**, **Sljedeći servis do**, i za iznajmljena ili lizing vozila **Iznajmljeno do** (kraj najma ili lizinga; za vlastito vozilo se ne unosi).

Za vozilo izdato drugoj firmi, uz izdavanje se upisuje **Povratak do** (planirani povratak; stvarni datum povratka je zasebna radnja "Vrati").

* **Prikaz:** zaglavlje vozila ima po jednu oznaku za svaki datum, a lista vozila po kolonu. Boja: **žuto** u posljednjih 30 dana, **crveno** kad je rok prošao, a neunesen datum piše "nije uneseno". Sortiraju se kolone *Registrovano do* i *Iznajmljeno do*. Na telefonu se datumi vide i mijenjaju na kartonu vozila.
* **Obavještenje:** Admin i Super Admin dobiju obavještenje **30 dana** i ponovo **7 dana** prije svakog od datuma. Datum unesen u posljednjoj sedmici dobije samo hitno obavještenje. Kad se datum promijeni (npr. produži registracija), podsjetnici počinju ispočetka. Istekli rok se ne javlja; za to služi crvena boja. Provjera se vrši jednom dnevno.
* Izvoz vozila (Excel) sadrži TD i sve datume.

## 5. Smještaj tokom odsustva

Kad se odsustvo odobri nekome ko stanuje u smještaju firme, dijalog **Odobri** kaže gdje stanuje i nudi dvije opcije. Isto pitanje je u dijalogu za evidentiranje odsustva kao već odobrenog, i u dashboard widgetu zahtjeva.

| Vrsta odsustva | Skidanje sa smještaja |
|---|---|
| **Godišnji odmor** | Predznačeno: boravak se završava **dan prije** prvog dana odsustva, pa se smještaj za dane odsustva ne obračunava |
| Bolovanje, neplaćeno, obuka, ostalo | **Pita se**; ništa se ne mijenja dok admin ne označi |

* **Vrati u isti smještaj** (po povratku): kvačica, **ne** predznačena. Po povratku radnika admin ga ponovo smješta ručno, ili označi ovu kvačicu pa se u isti smještaj upisuje novi boravak od dana poslije odsustva (s istim terećenjem gradilišta). Ne upisuje se ako je boravak ionako završavao prije povratka, ni ako je radnik u međuvremenu smješten drugdje.
* Ne dira se boravak koji počinje tek unutar odsustva.
* Na prethodnom boravku ostaje napomena o odsustvu, a sve ide u revizijski trag.

**Ograničenja:** ako se odobreno odsustvo kasnije skrati, otkaže ili mu se promijene datumi, smještaj se ne vraća sam. Odsustva odobrena prije ove izmjene ne mijenjaju smještaj.

## 6. Ctrl+K pretraga

**Ctrl+K** otvara paletu: stranice, radnje "novo…" i zapisi. Zapisi se traže od dva znaka, a u rezultatima se pojavljuje samo ono što uloga smije otvoriti.

| Šta se traži | Po čemu | Gdje vodi | Ko vidi |
|---|---|---|---|
| Zaposleni | ime, broj, pozicija, e-pošta | zaposleni | Predradnik i iznad |
| Projekti, klijenti | naziv, klijent… | projekat, klijent | Predradnik i iznad |
| **Vozila** | marka, model, **registracija, TD**, VIN | vozilo | Predradnik i iznad |
| Alat, materijali, zadaci, smještaj | naziv, serijski broj… | zapis | Predradnik i iznad |
| **Kartice za gorivo** | **broj kartice**, dobavljač, vozilo (naziv, tablica, TD) | vozilo na koje je kartica | oni koji smiju vidjeti troškove (finansijsko pravo) |
| **Redovi DKV izvoda** | **broj kartice**, vozilo (naziv, tablica, TD), proizvod | Provjera DKV izvoda, već filtrirana na unesene riječi | Admin i Super Admin |
| **Odsustva** | ime ili broj zaposlenog | Odsustva, red je istaknut | Predradnik i iznad |
| **Refundacije** | opis ili ime | Refundacije | Predradnik i iznad |
| **Narudžbe artikala** | artikal, napomena ili ime | Narudžbe | Predradnik i iznad |
| **Fakture** | broj, klijent, projekat | Fakture | oni koji vode fakture |
| **Mjeseci platnog spiska** | naziv | platni spisak | samo Super Admin |
| **Oglasna ploča** | naslov ili tekst | Oglasna ploča | Predradnik i iznad |
| Korisnički računi, grupe obavještenja | e-pošta, naziv | račun, grupa | Admin i Super Admin |

Pored toga, u paleti se nudi **Provjera DKV izvoda** kao stranica (jer nije u meniju), i radnje "Novo vozilo", "Evidentiraj trošak vozila" itd.

Rezultat otvara zapis, a ako zapis nema vlastitu stranicu (red izvoda, odsustvo), otvara listu na tom redu ili s filterom. Filter DKV stranice se skida klikom na x na oznaci "Pretraga: …".

## 7. Za razvoj

**Novi API (v1):**

| Ruta | Ko | Šta |
|---|---|---|
| `GET /vehicle-fuel`, `POST /vehicle-fuel`, `GET /vehicle-fuel/mine` | svi zaposleni | Gorivo za vozača: moja točenja, evidentiranje, vozila koja držim. Radnik je ograničen na gorivo za vozilo koje mu je zaduženo, a sliku računa smije prilagati samo svom unosu. (`/vehicle-fuel/vehicles` je stara verzija rute iz aplikacije 1.1.18 i ostaje) |
| `GET /fuel-transactions`, `/counts`, `/batches`, `/{id}/candidates` | Admin+ | Redovi izvoda, brojači, raniji uvozi, unosi koje se red može spojiti |
| `POST /fuel-transactions/import/preview`, `/import` | Admin+ | Pregled i uvoz izvoda |
| `POST /fuel-transactions/{id}/resolve`, `/assign-card`, `/recheck` | Admin+ | Rješavanje reda, dodjela kartice, ponovno uparivanje |
| `GET /absences/housing-impact` | Admin+ | Gdje osoba stanuje prvog dana odsustva |
| `POST /absences/{id}/review`, `POST /absences` | | Prihvataju `releaseAccommodation` i `returnToAccommodation` |

Većina listi dobila je parametar `search` (kartice, redovi izvoda, odsustva, refundacije, narudžbe).

**Model:** `Vehicle` ima `TdNumber`, `RegistrationValidUntil`, `TechnicalInspectionValidUntil`, `InsuranceValidUntil`, `NextServiceDue`, `RentedUntil`; `VehicleRentalOut` ima `ExpectedEndDate`. Novo: `FuelTransaction` (red izvoda; ključ je kartica + vrijeme + šifra proizvoda + iznos), `FuelImportBatch`, `VehicleDateReminder`. Sve ove tabele, osim podsjetnika, su u reviziji.

**Obavještenja:** `DkvStatementMismatch` (uvoz sa neslaganjima), `VehicleDateExpiring` (30 i 7 dana).

**Uparivanje** je u `DkvMatcher` (čista logika bez baze, sa testovima), a `DkvImportPlanner` je zajednički za pregled i uvoz, pa se oni ne mogu razilaziti.

**Testovi:** za integracijske testove treba PostgreSQL (`ConstructionTests__Postgres`); vidi `README.md` (sekcija o testovima).
