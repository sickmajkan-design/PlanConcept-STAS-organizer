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

## 6. Ograničenja ove verzije

* Radni dani su ponedjeljak–petak. Gradilišta koja rade subotom ili nedjeljom imaju te dane ako se radnik rasporedi na njih, ali potreba se tamo ne računa.
* Potreba je jedan broj po vještini za cijeli projekat. Potreba koja se mijenja po sedmicama nije podržana.
* Radnik ima jednu vještinu (svoju poziciju).
* Raspored je vidljiv Voditeljima i iznad bez suženja po gradilištu, kao i stara tabla raspoređivanja.

## 7. Za razvoj

| Ruta | Ko | Šta |
|---|---|---|
| `GET /api/v1/planning?from&to&branchId` | Voditelj+ | Radnici s postavljanjima i odobrenim odsustvima, projekti s potrebom, pozicije. Najviše 400 dana |
| `POST /api/v1/planning/assign` | Voditelj+ | `{employeeId, projectId \| null, from, to, onlyFreeDays}`: postavlja raspored za razdoblje |
| `POST /api/v1/planning/swap` | Voditelj+ | `{employeeAId, employeeBId, from, to}`: razmjena u jednoj transakciji |
| `PUT /api/v1/planning/projects/{id}/needs` | Voditelj+ | `{needs: [{position, count}]}`: zamjenjuje potrebu projekta |

**Model:** `ProjectStaffingNeed` (projekat, pozicija, broj; u reviziji). Postavljanja i dalje čuva `EmployeeProject`. Rezanje i spajanje razdoblja je u `PostingRanges` (čista logika s testovima), a pravila manjka i prijedloga zamjene u `planningLogic.ts` na klijentu, koju koriste stranica i dijalog odobravanja odsustva.

**Testovi:** `PostingRangesTests` (unit), `PlanningTests` (integracijski, treba PostgreSQL), `planningLogic.test.ts`, `PlanningPage.test.tsx`, `ApproveAbsenceDialog.test.tsx`.
