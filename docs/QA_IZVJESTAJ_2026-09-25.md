# QA izvještaj — potpuno testiranje platforme i aplikacije

Datum: 2026-09-25. Testirano: API, admin panel (web) i mobilna aplikacija (Android). Verzije: panel i API iz grane `claude/construction-workforce-phase-1-diz0zx`, aplikacija 1.1.5 → 1.1.6.

## 1. Zaključak

**Server je stabilan i dobro zaštićen. Mobilna aplikacija je imala ozbiljne funkcionalne greške koje automatski testovi nisu mogli vidjeti; sve su ispravljene i provjerene na emulatoru.**

- API: **0 grešaka servera (5xx)** na oko 1.650 zahtjeva (svi GET endpointi × 7 uloga, te ~1.000 zahtjeva s namjerno lošim i napadačkim ulazom na 126 endpointa za pisanje).
- Prava pristupa: svih 7 uloga se ponaša kako je zamišljeno, uključujući izolaciju dva klijenta i trenutno oduzimanje finansijskog prava.
- Aplikacija: pronađeno **12 grešaka** (1 kritična, 3 visoke, 3 srednje, 5 niskih). Sve su popravljene, uz regresione testove.
- **Preporuka:** prije stvarnog rada ponoviti kratak test na **stvarnom telefonu** (GPS u pozadini, kamera, obavještenja), jer ga emulator ne može zamijeniti. Vidi §6.

## 2. Šta je testirano i kako

| Sloj | Metod | Obim |
|---|---|---|
| API — pristup | Skripta prijavljuje 7 uloga (anonimno, Klijent, Radnik, Predradnik, Voditelj projekta, Admin, Super Admin) i poziva svaki GET endpoint bez parametara | 92 endpointa × 7 = 644 zahtjeva |
| API — tokovi | Vremenska evidencija, odsustva, troškovi, finansijska prava (dodjela/oduzimanje), brisanje projekta, portal klijenta, lokacije, prijava/odjava/obnova tokena | ~130 provjera |
| API — otpornost | Prazan, pogrešan, ekstreman i napadački ulaz (SQL, XSS, unicode, 20.000 znakova, pogrešni datumi/ID-jevi) na sve POST/PUT endpointe | 126 endpointa × 8 = ~1.000 zahtjeva |
| Panel | Playwright: svaka ruta kao Super Admin i kao Admin bez finansijskog prava, na 1366 px i 390 px (telefon); bilježi greške u konzoli, neuspjele zahtjeve, prazne stranice, preliv | 54 rute × 2 širine × 2 korisnika = 216 učitavanja |
| Aplikacija | Izgrađena release verzija na Android emulatoru (Pixel), prijava kao Radnik i Predradnik, kroz tokove, uključujući rad bez interneta | ~25 ekrana i tokova |

Okruženje: lokalna instanca sa svježom bazom i korisnicima svih uloga, da testni server (koji klijent gleda) ostane netaknut.

## 3. Pronađene greške

Ozbiljnost: Kritična = korisnik ne može obaviti osnovni posao ili se gube podaci; Visoka = ključna funkcija radi pogrešno; Srednja = djelimično pokvareno; Niska = kozmetika ili sitna nedosljednost.

| ID | Ozbiljnost | Gdje | Opis | Status |
|---|---|---|---|---|
| QA-01 | **Kritična** | Aplikacija | U dijalogu „Završetak smjene“ nije se prikazivalo dugme „Potvrdi“ (tema daje dugmetu beskonačnu širinu, što u `Row`-u nestaje bez poruke). Radnik nije mogao završiti smjenu. Isti obrazac u dva ekrana skeniranja i odgovoru na izmjenu odsustva | Ispravljeno (1.1.5) |
| QA-02 | **Visoka** | Aplikacija + server | Aplikacija pri prijavi na smjenu nikad ne šalje gradilište, pa su svi sati bili „bez gradilišta“ i nisu ulazili u trošak rada nijednog projekta (ni geofence provjera ni obavještenja predradnicima) | Ispravljeno: bez poslanog gradilišta uzima se jedina aktivna dodjela radnika; **radnik s više dodjela bira gradilište pri prijavi na smjenu** (aplikacija 1.1.7) |
| QA-03 | **Visoka** | Aplikacija | Liste ostaju u memoriji cijele sesije aplikacije. Kad se na istom telefonu prijavi druga osoba, vidi **listu prethodne** (sate, odsustva, zadatke), i aplikacija uopće ne traži nove podatke. Curenje podataka između korisnika | Ispravljeno (svih 14 lista i 6 pomoćnih provajdera zavise od prijavljenog korisnika) |
| QA-04 | **Visoka** | Aplikacija | Predradnik i voditelj projekta u „Moje radno vrijeme“, „Odsustva“ i „Moji zadaci“ dobijali su podatke cijele ekipe kao svoje (server sužava samo Radnika), bez imena na karticama | Ispravljeno |
| QA-05 | Srednja | Aplikacija/server | Radniku se nudi „Sedmični izvještaji“, a server na svaki poziv vraća 403 („Nemate dozvolu“), i pri slanju | Ispravljeno: stavka skrivena radnicima. **Odluka vlasnika:** radnik ne šalje, poslovođa i iznad šalju (potvrđeno) |
| QA-06 | Srednja | Aplikacija | Izbornik datuma na ćirilici („Изаберите период“) u aplikaciji na latinici | Ispravljeno |
| QA-07 | Srednja | Panel | Na telefonu (390 px) početna stranica administratora je šira od ekrana: dugačka e-pošta u naslovu „Welcome, …“ se ne prelama | Ispravljeno |
| QA-08 | Niska | Aplikacija | Pretraga u „Moji zadaci“ ima tekst „Prikaži i završeno“ umjesto teksta pretrage | Ispravljeno |
| QA-09 | Niska | Aplikacija | Obavještenje s oglasne ploče ima engleski naslov; obavještenje o odsustvu završava sa dvije tačke („13.10.2026..“) | Ispravljeno |
| QA-10 | Niska | API/panel | `POST /auth/logout` bez sesije vraća 400 pri svakom učitavanju panela (greška u konzoli) | Ispravljeno (204) |
| QA-11 | Niska | API | Swagger dokumentacija vraća 500 jer dva tipa imaju isti naziv `UserDto` | Ispravljeno |
| QA-12 | Niska | Panel | Vodič prvog podešavanja je pretijesan na telefonu | Ispravljeno |

## 4. Otvoreno

| ID | Ozbiljnost | Opis | Preporuka |
|---|---|---|---|
| OPEN-01 | Niska | React upozorenje „setState u renderu“ na tabelama Evidencija i Sedmični izvještaji (samo razvojna konzola). Uzrok nije nađen; probano memoizovanje svojstava tabele i opcija veličine stranice, bez efekta | Istražiti pri prvom izdanju MUI biblioteke |
| OPEN-02 | Niska | Donja navigacija za Predradnika (5 kartica): „Radno vrijeme“ se lomi u dva reda, a „Obavještenja“ dodiruje ivicu | Skratiti oznake |
| OPEN-03 | Niska | Svi korisnici vide poruku „Slanje push obavještenja nije podešeno u ovoj verziji“ | Nestaje kad se podesi Firebase |
| OPEN-04 | Niska | Greške servera (npr. „Invalid email or password.“) su na engleskom u srpskoj aplikaciji | Poznato ograničenje: API nije lokalizovan |
| OPEN-05 | Niska | Na listi odsustva dugme „Zatraži odsustvo“ prekriva zadnju karticu; oznaka „Čeka odgovor“ na vrhu nije jasna | Dodati donji razmak; pojasniti oznaku |
| OPEN-06 | Niska | Tekst „Nisi tražio nijedno odsustvo“ je u muškom rodu za sve korisnike | Rodno neutralna formulacija |
| OPEN-07 | ~~Info~~ | ~~Radnik može čitati postavke firme (PDV/porezni broj, telefon, e-pošta…)~~ | **Ispravljeno po odluci vlasnika:** samo poslovođa i iznad; radnik je odbijen na svim ostalim provjerenim `{id}` i listama, osim vlastitog rasporeda i oglasne ploče. Javni naziv i logo ostaju |
| OPEN-08 | Info | Moguća je smjena od nula minuta (prijava pa odmah odjava) | Razmotriti minimalno trajanje |
| OPEN-09 | Pokrivenost | 15 endpointa s `{id}` (dodjele, aktivacija, označavanje pročitanim…) fuzz je pogodio samo sa nasumičnim ID-jevima, ne s pravim | Dopuniti testom sa stvarnim podacima |

## 5. Ponašanja koja su provjerena i namjerno ostavljena (nisu greške)

- Prijava na smjenu na gradilištu na koje radnik nije dodijeljen nije zabranjena, nego se ured obavještava.
- Sate pregledaju Voditelj projekta i iznad; Predradnik ne.
- Preklapajući zahtjevi za odsustvo mogu biti u čekanju, ali se ne mogu odobriti oba.
- Radnik koji traži stanje godišnjeg za kolegu dobija svoje (sužavanje bez greške).
- Predradnik vidi sve zaposlene i gradilišta (dokumentovana odluka); lokaciju vidi samo za svoju ekipu.
- Zaključavanje računa nakon pogrešnih lozinki nije razlučivo od pogrešne lozinke (namjerno, protiv pogađanja).
- Bez finansijskog prava admin ne vidi ni jedan iznos; „samo statistika“ vraća samo procente; oduzimanje djeluje odmah.

## 6. Šta NIJE testirano (i šta treba prije stvarnog rada)

Na **stvarnom Android telefonu**:
1. Praćenje lokacije u pozadini tokom cijele smjene, sa zaključanim ekranom i štednjom baterije proizvođača (Xiaomi, Huawei i sl. često gase pozadinske usluge).
2. Kamera: skeniranje QR koda, fotografija kvara i priloga.
3. Prijava smjene bez signala na gradilištu i sinhronizacija poslije.

Nije rađeno uopće: iOS aplikacija; push (Firebase nije podešen); slanje e-pošte (SMTP nije podešen); dubinski tokovi obračuna mjeseca (Evidencija), izvoz u Excel i uvoz datoteka; opterećenje i performanse; pristupačnost (WCAG); preglednici osim Chromiuma; istek sesije nakon dužeg mirovanja.

## 7. Regresioni testovi dodani u ovoj rundi

- Server: automatski izbor jedine aktivne dodjele (4 slučaja); odjava bez sesije daje 204.
- Aplikacija: dugme potvrde smjene se vidi u pravoj temi; liste se traže za prijavljenog korisnika, i ponovo kad se korisnik promijeni; latinica u izborniku datuma; prijevod obavještenja i datum bez duple tačke.
- Panel: link za preuzimanje aplikacije, novi vodič postavljanja i njegovo ponašanje po koracima.
- Ukupno nakon runde: backend 570 jediničnih + 810 integracionih, aplikacija 302, panel svi.

## 8. Odluke koje su potrebne od vlasnika

Sve tri su riješene (odluke vlasnika, 2026-09-25):
1. **Sedmične izvještaje** ne šalje Radnik; šalju poslovođa i iznad. Već je tako radilo na serveru; aplikacija ih radnicima više ne nudi.
2. Radnik s **više aktivnih dodjela** bira gradilište pri prijavi na smjenu (aplikacija 1.1.7); s jednom se uzima automatski.
3. **Postavke firme** čitaju poslovođa i iznad; radnik ne.
