# Vodič: Raspored i planiranje radne snage

Za Voditelje, Admine i Super Admina. Opisuje novu stranicu **Raspored** (7. oktobar 2026.), koja zamjenjuje stari Raspored i Tablu raspoređivanja. Stari linkovi (`/assignment-board`) vode na nju. Predradnici i niži i dalje vide raniji pregled ko je gdje.

## Vodič kroz stranicu

Dugme **Vodič kroz Raspored** (gore desno) pokreće interaktivni obilazak od 13 koraka, oko minut: osvijetli stvarne dijelove stranice (prikaze, razdoblje, traku, potrebu, zamjenu), sam prebacuje prikaze i opisuje šta se u njima radi. Kreće se dugmadima ili strelicama na tastaturi, a Esc ga prekida. Pri prvom otvaranju stranice nudi se sam, a ako se odbije, ne pita ponovo.

## 1. Četiri prikaza

| Prikaz | Za šta služi |
|---|---|
| **Po gradilištima** | Ko je gdje na jedan dan. Kartica za svako gradilište s ekipom i brojačem (npr. 3 / 4). Prazna mjesta su isprekidana polja s nazivom vještine koja fali. Sa strane su Slobodni, Odsutni i projekti koji uskoro počinju |
| **Vremenska traka** | Radnici kao redovi, vrijeme kao kolone: traka za svaki period na gradilištu, šrafirano tamo gdje je radnik odsutan. Na dnu su redovi *Slobodno* i *Nedostaje* po koloni. Može se suziti na jednu vještinu |
| **Potreba po projektima** | Svaki projekat naspram onoga što treba, po periodima. Strelica ▸ otvara vještine projekta. Ispod je tabela po vještinama: najveći manjak, slobodnih i stanje |
| **Lista** | Jedan dan, jedan red po radniku, gradilište kao padajući izbor. Najbrže za sređivanje |

## 2. Razdoblje

Prikazi Vremenska traka i Potreba imaju izbor: **Sedmica**, **2 sedmice**, **Mjesec**, **Kvartal**, **Prilagođeno** (od–do). Mjesec i kvartal su **kalendarski** (cijeli oktobar; 4. kvartal je oktobar–decembar). Strelice prelaze na prethodno ili sljedeće razdoblje. Do mjesec dana kolone su dani, a duže od toga sedmice.

Prikazi za jedan dan (Po gradilištima, Lista) imaju izbor dana, dugme *Danas* i kalendar.

Najduže razdoblje koje se učitava odjednom je 400 dana.

## 3. Raspoređivanje

Klik na radnika (ili traku u vremenskoj traci) otvara prozor:

* **Od / Do:** razdoblje na koje se odnosi izmjena, unaprijed popunjeno prema onome što je kliknuto.
* **Rasporedi na:** gradilište, ili *Slobodan*. Izmjena **postavlja** raspored za ta razdoblja, ne dodaje: ako je radnik bio negdje drugdje, taj period se prekida, a ostatak njegovog boravka ostaje. Ponovljen zahtjev ne mijenja ništa novo.
* **Zamijeni drugim radnikom:** dvojica razmjenjuju raspored za to razdoblje.
* Odsustva se ne dira: osoba je tada samo odsutna, pa se poslije odsustva sama vraća na gradilište.

Klik na prazno mjesto na kartici gradilišta (ili ćeliju u Potrebi) otvara listu za popunu: **slobodni u razdoblju** (s brojem slobodnih dana) i **prebaci s drugog gradilišta**. Radnici prave vještine su na vrhu i označeni.

Svaka izmjena prikazuje poruku s dugmetom **Poništi** koje vraća prethodno stanje.

Raspoređivanje na gradilište obavještava radnika kao i do sada, a oprema (alat, vozila) koju radnik drži prelazi na gradilište na kojem je danas.

## 4. Potreba po vještini

Svaki projekat može reći koliko radnika koje vještine treba (npr. 2 zidara, 1 električar). **Vještina je pozicija radnika** (polje Pozicija na radniku); isti naziv pisan drugačije velikim slovima ili s razmakom je ista vještina.

* Potreba je **jedan broj za cijelo trajanje** projekta, računa se od ponedjeljka do petka, unutar datuma projekta.
* Uređuje se olovkom pored projekta u prikazu *Potreba po projektima*. Novi projekat se pravi kao i do sada (dugme *Novi projekat* vodi na formu).
* Višak jedne vještine ne pokriva manjak druge: 3 zidara ne zamjenjuju električara.

## 5. Zamjena za odsutne

Kartica **Treba zamjenu** (iznad prikaza) pokazuje odobrena odsustva u narednih 30 dana zbog kojih gradilištu fali radnik njegove vještine. Uz svako piše koliko ima prijedloga ili da ih nema.

**Redoslijed prijedloga:** ista vještina i slobodan cijelo razdoblje; ista vještina, slobodan dio dana; ista vještina s gradilišta koje ima više te vještine nego što mu treba; na kraju druga vještina (označena kao ne odgovara). Ko je i sam odsutan u tom razdoblju ne nudi se.

* Zamjenik ide na gradilište **samo za dane odsustva** (ako je slobodan samo dio dana, popunjavaju se samo ti dani). Poslije toga se vraća na ono što je radio, a odsutni radnik se sam vraća na svoje gradilište.
* Isti prijedlozi se nude i **pri odobravanju odsustva** (Odsustva > Odobri, i na kontrolnoj tabli). Izbor je opcionalan: *Bez zamjene* odobrava samo odsustvo. Ako je odsustvo odobreno, a zamjena nije mogla biti postavljena, dijalog to kaže i ne odobrava ponovo.
* Ako nema nikoga, mjesto ostaje prazno i vidi se u Potrebi po projektima, a gornja oznaka kaže da se slobodnima ne može popuniti (treba angažovati).
* Obavještenje o odobrenom odsustvu koje ostavlja gradilište bez radnika (*Treba zamjenu*) šalje se kao i do sada.

## 6. Upozorenja i potvrda radnika

**Upozorenja ("Provjerite")** pojavljuju se ispod kartice Treba zamjenu kad je neko:

* istih radnih dana raspoređen na **dva gradilišta** (dozvoljeno je, ali je često greška), ili
* raspoređen na gradilište **prije početka ili poslije kraja** projekta.

To su upozorenja, ne zabrane. Uz svaki red je *Otvori* koji vodi na radnika. Pri raspoređivanju na gradilište čiji rok završava prije izabranog razdoblja, dijalog to piše (npr. "Hala B (do 06.12.)").

**Potvrda na telefonu (aplikacija 1.1.21):** radnik u *Moj raspored* pritiskom na **Potvrdi** javlja da je vidio gdje je raspoređen. U Rasporedu, u prikazu *Po gradilištima*, uz radnika je kvačica (potvrđeno) ili pješčani sat (čeka), brojač *n nije potvrdilo*, a u dijalogu radnika piše stanje. **Izmjena dana** rasporeda (skraćivanje, produženje, premještanje) traži novu potvrdu. Rasporedi koji su postojali prije ove izmjene računaju se kao potvrđeni.

## 7. Provjera podataka

Administracija > **Provjera podataka** (Admin i Super Admin) nabraja zapise koji kvare druge ekrane, najvažnije prvo:

| Provjera | Zašto |
|---|---|
| Raspoređeni na završen projekat | Raspored ih prikazuje tamo gdje više ne rade |
| Radnici bez pozicije | Ne mogu se planirati po vještini ni predložiti kao zamjena |
| Projekti bez potrebe | Raspored ne može reći gdje fali ljudi |
| Projekti bez datuma | Računaju se kao da traju zauvijek |
| Vozila bez datuma registracije, tehničkog ili osiguranja | Podsjetnici ne rade bez datuma |
| Vozila bez TD broja | Broj sa DKV izvoda se ne može provjeriti prema vozilu |
| Kartice za gorivo bez vozila | Redovi izvoda ostaju neuparani |

Svaka grupa pokazuje ukupan broj i prvih 50 zapisa, svaki s linkom na mjesto gdje se popravlja. Ništa ovdje ne sprječava rad. Provjere koje su u redu navedene su na dnu.

## 8. Uvjerenja i osposobljenost

Na stranici radnika (Voditelj i iznad) kartica **Uvjerenja i osposobljenost** nabraja za šta je radnik osposobljen i do kada: naziv (rad na visini, vozačka za viljuškar, zavarivanje), datum do kojeg važi (prazno: ne ističe) i napomena. Isto uvjerenje pisano drugačije velikim slovima ili s razmakom je isto uvjerenje, i radnik ga ne može imati dvaput.

Projekat može tražiti uvjerenja od svih koji su raspoređeni na njega: u prikazu *Potreba po projektima*, olovka pored projekta, polje **Potrebna uvjerenja**.

* **Upozorenje:** Raspored u kartici *Provjerite* piše kad je neko raspoređen na gradilište bez važećeg traženog uvjerenja, računajući dane poslije isteka. To je upozorenje, ne zabrana.
* **Zamjene:** u listi prijedloga zamjenika, među istom vrstom prijedloga, prvi su oni koji imaju tražena uvjerenja, a ostalima piše šta nemaju.
* **Ističu:** uvjerenja koja ističu u narednih 30 dana (i ona koja su istekla) prikazuju se u widgetu *Zahtijeva pažnju*. Podsjetnici porukom se ne šalju; za to služe dokumenti (skenovi) koji ističu.

## 9. Zahtijeva pažnju

Widget na početnoj stranici skuplja šta čeka tebe, samo ono što tvoja uloga može riješiti, s brojem i primjerima, a klik otvara stranicu gdje se rješava: zahtjevi za odsustvo, radni sati i troškovi vozila na pregledu, narudžbe i refundacije, DKV redovi koji se ne slažu, nepotvrđeni rasporedi, datumi vozila, dokumenti i uvjerenja koja ističu i zapisi za sređivanje iz Provjere podataka. Radnik i Predradnik ne vide ništa, jer nemaju šta da rješavaju.

## 10. Predradnik u Rasporedu

* **Pregled:** Predradnik otvara isti Raspored, ali vidi **samo svoju poslovnu jedinicu** (onu u kojoj trenutno radi, kako je upisana na njegovom radniku). Radnici i gradilišta drugih jedinica mu se ne šalju, bez obzira na to koju jedinicu izabere u zaglavlju. Nema dugmadi za izmjenu; bolovanje vidi samo kao "odsutan". Ako nije smješten ni u jednu jedinicu, stranica to kaže i prazna je.
* **Pravo raspoređivanja:** Admin ili Super Admin u Korisnici > Predradnik uključe **Smije raspoređivati radnike**. Tada može raspoređivati, osloboditi i zamjenjivati radnike, ali **samo radnike svoje jedinice na gradilištima svoje jedinice**; sve drugo API odbija. Potrebu projekta (vještine i uvjerenja) i dalje mijenja samo Voditelj i iznad. Pravo se vraća na isključeno kad se korisniku promijeni uloga.
* Radnici i dalje nemaju ovaj ekran.

## 11. Radni dani i praznici

Raspored prati kalendar svakog gradilišta:

* **Ponedjeljak–petak** su radni dani. **Subota i nedjelja** su radni samo za gradilišta koja imaju uključeno "Radi subotom" odnosno "Radi nedjeljom". Za takvo gradilište se potreba računa i tim danima, a vremenska traka tada ne siva te kolone.
* **Državni praznici** zemlje upisane na gradilištu (polje Zemlja) ne računaju se kao radni dani tog gradilišta: nema potrebe, nema manjka. Praznik je označen u zaglavlju vremenske trake (isprekidana crta, naziv u opisu) i u sažetku dana. Gradilište bez upisane zemlje ne poštuje nijedan praznik.
* Dan je "radni" za prikaz kad radi barem jedno gradilište u prikazu. Slobodni radnici se broje samo na takve dane.

## 12. Raspored na papiru

Dugme **Izvoz u Excel** (gore desno) pravi datoteku za štampanje ili okačiti na gradilištu: list **Raspored** (red po radniku, kolona po danu, u ćeliji gradilište ili GO/BO) i list **Po gradilištima** (za svako gradilište i dan: potrebno, raspoređeno, imena). Obuhvata razdoblje na ekranu (u prikazima za jedan dan, tu sedmicu), najviše 62 dana, na izabranom jeziku. Predradnik dobija samo svoju jedinicu, a bolovanje piše kao "odsutan".

## 13. Ograničenja ove verzije

* Potreba je jedan broj po vještini za cijeli projekat. Potreba koja se mijenja po sedmicama nije podržana.
* Radnik ima jednu vještinu (svoju poziciju).
* Raspored je vidljiv Voditeljima i iznad bez suženja po gradilištu, kao i stara tabla raspoređivanja.

## 14. Za razvoj

| Ruta | Ko | Šta |
|---|---|---|
| `GET /api/v1/planning?from&to&branchId` | Predradnik+ (Predradnik: samo svoja jedinica) | Radnici s postavljanjima i odobrenim odsustvima, projekti s potrebom, pozicije. Najviše 400 dana |
| `POST /api/v1/planning/assign` | Voditelj+, ili Predradnik s pravom (samo svoja jedinica) | `{employeeId, projectId \| null, from, to, onlyFreeDays}`: postavlja raspored za razdoblje |
| `POST /api/v1/planning/swap` | Voditelj+, ili Predradnik s pravom (samo svoja jedinica) | `{employeeAId, employeeBId, from, to}`: razmjena u jednoj transakciji |
| `PUT /api/v1/planning/projects/{id}/needs` | Voditelj+ | `{needs: [{position, count}], requiredCertificates?: string[]}`: zamjenjuje potrebu projekta; bez `requiredCertificates` ostavlja tražena uvjerenja kakva jesu |
| `POST /api/v1/postings/{id}/acknowledge` | svi zaposleni | Radnik potvrđuje svoj raspored; tuđi vraća 404 |
| `GET /api/v1/exports/schedule?from&to&language&branchId` | Predradnik+ | Excel: raspored po radniku i po gradilištu, najviše 62 dana |
| `GET /api/v1/data-quality` | Admin+ | Grupe zapisa za sređivanje |
| `GET /api/v1/attention` | svi zaposleni | Šta čeka pozivaoca, ograničeno na ono što uloga može riješiti |
| `GET, PUT /api/v1/employees/{id}/certificates`, `DELETE .../{certificateId}` | Voditelj+ | Uvjerenja radnika; PUT s `id` mijenja postojeće, bez njega dodaje |

**Model:** `ProjectStaffingNeed` (projekat, pozicija, broj), `EmployeeCertificate` (radnik, naziv, važi do), `ProjectCertificateRequirement` (projekat, naziv); sve u reviziji. Postavljanja i dalje čuva `EmployeeProject`. Rezanje i spajanje razdoblja je u `PostingRanges` (čista logika s testovima), a pravila manjka i prijedloga zamjene u `planningLogic.ts` na klijentu, koju koriste stranica i dijalog odobravanja odsustva.

**Testovi:** `PostingRangesTests` (unit), `PlanningTests` (integracijski, treba PostgreSQL), `planningLogic.test.ts`, `PlanningPage.test.tsx`, `ApproveAbsenceDialog.test.tsx`.
