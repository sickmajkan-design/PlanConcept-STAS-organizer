# Evidencija → Obračun mjeseca — plan implementacije

Status: **Faza 1 implementirana lokalno (nije commitovana ni deployovana); Faze 2 i 3 su prijedlog.** Sastavljeno 2026-09-23 na osnovu tabele koju kupac danas vodi u Excelu i modula `/ledgers` ("Evidencija") kakav je sada. Korisnik je vlasnik firme; knjigovođa kasnije pregleda i prepisuje u knjigovodstveni program. Evidencija ostaje **samo za Super Admina**.

Interaktivna maketa (izmišljeni podaci): [mockups/obracun-mjeseca.html](mockups/obracun-mjeseca.html).

---

## 0. Šta je urađeno (Faza 1)

- **Šablon "Obračun radnika"** pri pravljenju novog mjeseca: 20 kolona (cijena radnika, 5 sedmica, sati, cijena za klijenta, zarada radnika, naplata, marža, doprinosi, rent a car, gorivo, stanovanje, godišnji, razlika/bonus, akontacija, regres, rezultat) i 8 kućica sažetka sa predznacima. Opcija "popuni gradilišta i radnike iz sistema" pravi sekciju za svako gradilište sa radnicima dodijeljenim tog mjeseca.
- **Računske kolone** sa fiksnim formulama (proizvod dvije kolone plus zbir sa predznacima, bez slobodnog editora): sati = zbir sedmica; zarada = cijena × sati − akontacija + razlika + godišnji; naplata = sati × cijena za klijenta; marža = naplata − zarada; rezultat = marža − troškovi. Kolona od koje zavisi druga ne može se obrisati; izračunata kolona zadržava tip.
- **Ručna korekcija** upisom preko izračuna: označena narandžasto, sa strelicom za povratak, i ulazi u listu "Za provjeru".
- **Zbir firme** iz kućica sažetka nad svim sekcijama (izračunate kolone se sabiraju red po red).
- **Lista "Za provjeru"**: sati bez cijene za klijenta, ručno upisana polja, radnik sa više sati kroz sekcije nego što mjesec ima (zadano 176).
- **Kopiranje mjeseca** prenosi kolone, formule (sa prevezanim kolonama), kućice (bez ručno upisanih iznosa) i sekcije, bez brojki.
- **Izvoz u Excel** (`GET /api/v1/ledgers/{id}/export`): sekcija po sekcija sa zbirovima i drugim listom sa sažetkom; brojevi su brojevi, ručne korekcije imenovane u koloni "Napomena".
- **Testovi:** 15 integracionih i 7 jediničnih za obračun; cijeli skup 558 + 729 prolazi. Brojke iz kupčeve tabele su uzete kao primjer (npr. 160 h × 33 = 5280, zarada 3200, marža 2080, rezultat 380; akontacija ne mijenja rezultat firme).
### Faza 2 (urađeno lokalno)

- **Sati iz radnog vremena po sedmici.** Kolone sedmica se zovu po kalendarskim sedmicama mjeseca (KW36…KW40) i pune se iz **odobrenih** unosa radnog vremena radnika, i to samo na gradilištu te sekcije (radnik podijeljen na dva gradilišta u svakoj sekciji pokazuje svoj dio). Šest kolona sedmica, jer mjesec može dotaknuti šest sedmica; neiskorištena je prazna.
- **Cijena rada iz sistema:** važeća satnica radnika na zadnji dan mjeseca (iz cijena rada).
- **Ručni upis preko automatske vrijednosti** je ručna korekcija (označena), ali se prijavljuje u "Za provjeru" samo ako se **razlikuje** od radnog vremena; sati upisani za nekoga čije je radno vrijeme prazno ništa ne dovode u pitanje. Za redove bez radnika (kooperanti, kancelarija) upis je običan unos, ne korekcija.
- **Nova provjera:** sati poslani ali neodobreni (ne računaju se, lako se zaborave).
- **Kopiranje mjeseca** sada prenosi cijene radnika, cijene za klijenta, doprinose i regres (isto su i sljedeći mjesec), prevezuje kolone sedmica na sedmice novog mjeseca i preimenuje ih; sati i ostali iznosi kreću prazni.
- **Naplata klijentu** nema izvor u sistemu (ne postoji cijena po klijentu), pa se upisuje ručno jednom i prenosi u sljedeće mjesece.
- Zbirovi sažetka računaju i redove koji imaju samo automatske vrijednosti (bez ijedne upisane ćelije).
- **Testovi:** 26 integracionih (uključujući sate iz radnog vremena, cijenu, pravila korekcije, neodobrene sate, kopiranje i računanje sedmica mjeseca) i 13 jediničnih za kalkulator.
### Faza 3 (urađeno lokalno): gorivo, rent a car i stanovanje iz modula

- **Gorivo:** odobreni troškovi goriva vozila **dodijeljenih radniku** u mjesecu.
- **Rent a car:** mjesečna cijena zakupa tih vozila, srazmjerno danima u kojima je važila (cijeli mjesec = tačan iznos).
- **Stanovanje:** radnikov dio kirije smještaja, računat istim kalkulatorom kao stranice smještaja (kirija se dijeli na stanare). Boravak vezan za gradilište ide na red tog gradilišta.
- **Pravilo prvog reda:** trošak koji pripada osobi, a ne gradilištu (gorivo, zakup, boravak bez gradilišta), ide na **prvi red te osobe** u mjesecu. Tako radnik u dvije sekcije nema isti račun za gorivo dvaput.
- **Ista pravila korekcije kao za sate:** ručni upis preko automatske vrijednosti se označava, a u "Za provjeru" ulazi samo ako se razlikuje od onoga što sistem zna i to nije nula.
- **Kućica "Gorivo" u sažetku** sada sabira kolonu goriva umjesto ručnog iznosa. Gorivo vozila koja nisu ničija ne ulazi; za to postoji ručna kućica.
- Šablon prevezuje i ove kolone pri kopiranju mjeseca.
- **Testovi:** 31 integracioni za obračun (uključujući pet za fazu 3).

**Nije urađeno (ostaje):** vrsta reda (Montir/Kooperant) i količina ("Kooperanti × N"), fiksni troškovi po zemlji iz opštih troškova, uvoz starog mjeseca iz Excela, jednostavan/napredni režim. Otvoreno pitanje: gorivo se sada uzima samo iz **odobrenih** troškova (kao i sati); kupac treba potvrditi da je to željeno.

## 1. Problem

Evidencija je danas prazna tabela: za svaki mjesec kupac mora sam odlučiti šta su kolone, sekcije, redovi, kućice sažetka i predznaci. Šest pojmova, jedan ekran od ~2200 linija koda. Prvi mjesec počinje od nule; "kopiraj strukturu iz prošlog mjeseca" postoji, ali ne pomaže prvi put.

Kupac to danas radi u Excelu (jedan list po mjesecu, oko 27 sekcija, 120+ redova). Cilj: **za manje od 5 minuta ima popunjen mjesec**, i to tačan.

## 2. Šta tabela radi (izvedeno iz formula)

Sekcija = klijent (gradilište). Red = radnik, ili "kooperanti" (bez imena, samo broj sati i cijena). Vrste redova: montir, običan radnik, kooperant, kancelarija.

| Kolona | Formula |
|---|---|
| Sati | zbir sedmica (KW36…KW40) |
| Zarada radnika | cijena radnika × sati − akontacija + razlika/bonus + godišnji |
| Naplata klijentu ("OBRT FIRMA") | sati × cijena za klijenta |
| Prihod firme (marža) | naplata − zarada radnika |
| Rezultat sekcije ("UKUPNO DOBROPIS") | marža − doprinosi − rent a car − gorivo − stanovanje − akontacija − regres |
| Ukupan prihod | zbir marži svih sekcija |
| **Zarada firme** | ukupan prihod − gorivo − regres − fiksni (SLO, DE, HR) − doprinosi − auta − stanovi − alat/uniforme |

Akontacija se poništava u rezultatu sekcije (oduzeta u zaradi radnika, pa ponovo oduzeta od marže), tj. ne mijenja rezultat firme — samo trenutak isplate.

## 3. Greške u postojećoj tabeli (razlog za ugrađen obračun)

Nađeno programskom provjerom formula:

1. **Sažetak izostavlja sekcije.** REGRES u sažetku 5500, zbir svih sekcija 6200; nedostaju dvije sekcije, a ubačene su četiri pogrešne reference. Ispravna zarada je za ~700 niža od prikazane. Isto važi za AUTA (danas nule).
2. **Pogrešna referenca u formuli za sate** jednog radnika (uzima sedmicu iz reda drugog radnika).
3. **16 redova ima cijenu za klijenta, ali nema formulu za naplatu** — njihov prihod je samo negativan trošak. Nije jasno da li je namjerno.
4. **Kancelarija: zarada i prihod su upisani brojevi**, ne formule, i međusobno se ne poklapaju.
5. **Kolona "R"** zove se "Godišnji odmor" u prvoj sekciji, "Akontacija" u ostalim, a formula je uvijek *dodaje*.
6. **Isti radnik u više sekcija** bez provjere ukupnih sati.
7. Zaglavlja se ručno mijenjaju (KW, mjesec), a **gorivo se upisuje ručno** u sažetak umjesto da se povuče iz troškova.

## 4. Šta Evidencija danas ima, a šta fali

| Potreba | Danas |
|---|---|
| Sekcije po klijentu, redovi po radniku | Ima |
| Kućice sažetka sa +/− | Ima |
| Kopiranje strukture iz prošlog mjeseca | Ima |
| Računske kolone | **Nema** (samo ručne i automatske: trošak vozila/alata/materijala) |
| Sati iz radnog vremena po sedmici | **Nema izvora** |
| Cijena rada iz sistema | **Nema izvora** (cijene rada postoje u sistemu) |
| Vrsta reda (montir/kooperant/kancelarija) | Nema |
| Izvoz u Excel | Treba provjeriti |

Ulazi koje sistem već ima u drugim modulima: radno vrijeme, cijene rada, uvoz goriva i troškovi vozila, smještaj (cijene i boravci), opšti troškovi, projekti/kupci.

## 5. Preporuka

**Ne pravimo slobodan editor formula.** Vlasnik bi u njemu napravio baš greške iz §3. Pravimo gotov modul **"Obračun mjeseca"** sa fiksnim, testiranim formulama iz §2:

- sistem sam zbraja **sve** sekcije (nema nedostajućih referenci);
- ručna korekcija je moguća, ali **označena** ("ručno") dok se ne potvrdi;
- "Kooperanti × N" u jednom redu;
- tri kartice (Sati, Zarada, Troškovi) umjesto 25 kolona, sažetak firme odozgo;
- lista **"Za provjeru"** prije zaključivanja mjeseca.

Modul se ugrađuje uz postojeći Ledger (sekcije, redovi, kućice se ponovo koriste), ne kao zasebna paralelna struktura.

## 6. Faze

**Faza 1 — obračun sa ručnim unosom sati (Must)**
1. Šablon "Obračun radnika" (sekcije, redovi, kolone i formule iz §2).
2. Računske kolone sa ograničenim skupom operacija (množenje, sabiranje, oduzimanje), bez slobodnih formula.
3. Zbir firme nad svim sekcijama.
4. Vrsta reda i količina ("Kooperanti × N").
5. Izvoz mjeseca u Excel (jedan list, sekcije po klijentu, raspored kao na ekranu).
6. Lista "Za provjeru".

**Faza 2 — ulazi iz sistema (Must)**
7. Sati iz radnog vremena po sedmici (KW) i za mjesec; kooperanti i kancelarija ručno.
8. Cijena rada iz sistema.

**Faza 3 — ostali izvori i udobnost (Should)**
9. Gorivo iz uvoza goriva, stanovanje iz smještaja, rent a car iz vozila, fiksni troškovi iz opštih troškova.
10. Uvoz prošlog mjeseca iz Excela.
11. Kopiranje mjeseca sa cijenama, bez sati.

**Won't (sada):** slobodne formule, pristup Adminu, vlastiti obračun poreza i doprinosa.

## 7. User storyji (ključni)

### Obračun mjeseca iz šablona
**As a** vlasnik (Super Admin), **I want to** izabrati šablon obračuna i dobiti popunjen mjesec, **so that** ne pravim kolone i formule ručno.

- [ ] Given prazan mjesec, when biram "Obračun radnika", then dobijam sekcije iz aktivnih gradilišta, radnike u njima i kolone iz §2 sa formulama.
- [ ] Given sekcija bez radnika, then ostaje prazna, bez greške.
- [ ] Edge: radnik obrisan tokom mjeseca se ne prikazuje; stari mjeseci ostaju netaknuti.
- [ ] Samo Super Admin vidi ekran i iznose.

Points: 8 · Priority: Critical

### Računske kolone
**As a** vlasnik, **I want to** da se zarada radnika i marža računaju same, **so that** ne računam ručno.

- [ ] Given sati i cijene, then zarada, naplata i marža se izračunaju u redu i u zbiru sekcije, i mijenjaju se čim promijenim sate.
- [ ] Given nedostaje cijena ili sati, then polje ostaje prazno i označeno (ne 0).
- [ ] Edge: dijeljenje nulom nije moguće (nema dijeljenja u skupu operacija); negativni iznosi (ispravke) se prikazuju ispravno.
- [ ] Given ručno upisan iznos preko računa, then red nosi oznaku "ručno" i pojavljuje se u listi "Za provjeru".

Points: 8 · Priority: Critical

### Zbir firme nad svim sekcijama
**As a** vlasnik, **I want to** da ukupna zarada uvijek obuhvata sve sekcije, **so that** nijedan iznos ne ostane izvan sažetka.

- [ ] Given nova sekcija (klijent), then ulazi u zbir bez ikakve izmjene formule.
- [ ] Given sekcija bez naplate, then prikazuje se negativna marža, ne izostavlja se.
- [ ] Test: zbir sažetka = zbir rezultata svih sekcija, za svaku kolonu.

Points: 5 · Priority: Critical

### Lista "Za provjeru"
**As a** vlasnik, **I want to** vidjeti šta ne štima prije zaključivanja mjeseca, **so that** ne šaljem knjigovođi pogrešan obračun.

- [ ] Radnik čiji je zbir sati kroz sekcije veći od očekivanog (podesivo, zadano 176 h).
- [ ] Red sa satima a bez cijene za klijenta.
- [ ] Ručno upisana zarada.
- [ ] Sekcija bez klijenta/projekta.
- [ ] Given nema problema, then poruka "sve je uredu".

Points: 5 · Priority: High

### Izvoz u Excel
**As a** knjigovođa (preko vlasnika), **I want to** Excel sa istim rasporedom kao na ekranu, **so that** prepisujem u knjigovodstveni program bez prekucavanja.

- [ ] Jedan list, sekcije po klijentu, redovi po radniku, zbirovi po sekciji i sažetak firme.
- [ ] Brojevi su brojevi (ne tekst), da knjigovođa može sabirati.
- [ ] Ručne korekcije su označene u izvozu.
- [ ] Izvoz se upisuje u audit log (ko i kada).

Points: 5 · Priority: High

### Sati iz radnog vremena (Faza 2)
**As a** vlasnik, **I want to** da se sati radnika povuku iz radnog vremena, **so that** ih ne upisujem dva puta.

- [ ] Given odobreni unosi vremena, then sati po sedmici (KW) i mjesecu se popune i označe kao "auto".
- [ ] Given radnik nema aplikaciju (kooperant, kancelarija), then sati se upisuju ručno.
- [ ] Given unos je izmijenjen nakon obračuna, then obračun pokazuje da se razlikuje od radnog vremena.
- [ ] Edge: sedmica koja prelazi granicu mjeseca dijeli sate po datumu.

Points: 8 · Priority: High

## 8. Otvorena pitanja (čekaju kupca)

1. **Redovi bez naplate:** dva klijenta imaju cijenu za klijenta, ali formula za naplatu ne postoji. Namjerno, ili greška u tabeli?
2. **Kolona "godišnji/akontacija" (R):** *dodaje* na platu. Da li to tako i treba?
3. **Kancelarija:** fiksna plata ručno, ili računamo kao ostale?
4. **Doprinosi (Prispevke):** fiksno po radniku ili iz nečeg računa?
5. **Očekivani sati po radniku** za upozorenje o dvostrukom unosu (predlog 176 h).
6. **Značenje kolona "SATNICA" i "SATNICA WH/TRANE":** pretpostavka je cijena radnika i cijena za klijenta — potvrditi.

### Odgovori kupca (2026-09-26)

Plan izvedbe: [PLAN_OBRACUN_ODGOVORI_KUPCA.md](PLAN_OBRACUN_ODGOVORI_KUPCA.md).

1. **Namjerno.** Na satnicu naplata = sati × cijena; kod paušala i aufmaßa unose se ručno izdani računi.
2. **Godišnji se dodaje** na platu, **akontacija se oduzima** (dobili unaprijed).
3. **Kancelarija je fiksno.**
4. **Doprinosi** se računaju iz platnih lista, nisu uvijek isti.
5. **Sate isključivo ručno** unose iz potpisanih satnica, jer je samo to mjerodavno (sati iz aplikacije samo kao kontrola).
6. Da: "SATNICA" je cijena radnika, "SATNICA WH/TRANE" cijena za klijenta.
7. **Gorivo** se može preuzimati sa DKV.

## 9. Napomena o podacima

Izvorna tabela sadrži stvarna imena i plate radnika. **Nije kopirana u repozitorijum.** Maketa i ovaj dokument koriste izmišljene podatke i samo agregate.
