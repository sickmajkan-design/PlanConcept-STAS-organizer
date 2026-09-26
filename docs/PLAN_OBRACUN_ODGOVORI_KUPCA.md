# Obračun mjeseca: plan implementacije prema odgovorima kupca

Sastavljeno 2026-09-26 (Product Owner). Ulaz: odgovori Darija Stankovića, El Plan Concept GmbH, od 26.9.2026. na pitanja iz [EVIDENCIJA_OBRACUN_PLAN.md](EVIDENCIJA_OBRACUN_PLAN.md) §8 i jedno pitanje o gorivu iz §0. Evidencija ostaje **samo za Super Admina**.

## 1. Šta je kupac rekao i šta to mijenja

| # | Pitanje | Odgovor kupca | Posljedica po proizvod |
|---|---|---|---|
| 1 | Redovi sa cijenom za klijenta a bez naplate | Namjerno. Gdje se radi na **satnicu**, naplata je sati × cijena. Gdje se radi **paušalno** ili na **aufmaß / učinak**, kupac ručno unosi **izdane račune** po stvarnom aufmaßu ili paušalu. | Gradilište mora imati **način obračuna prema klijentu** (satnica / paušal / aufmaß). Naplata za paušal i aufmaß **nije formula**, nego zbir izdanih računa. Pravilo "sati bez cijene za klijenta" u "Za provjeru" vrijedi samo za satnicu. |
| 2 | "Godišnji" i "akontacija" | **Godišnji se dodaje** na platu. **Akontacija se oduzima** od plate (radnik ju je dobio unaprijed). | Dvije odvojene kolone sa suprotnim predznacima. Formula zarade je već tačna; mijenja se naziv, oznaka predznaka i test. Jedna kolona "R" iz stare tabele se više ne pravi. |
| 3 | Kancelarija | **Fiksno.** | Vrsta reda "Kancelarija": mjesečni fiksni iznos (zarada i prihod se upisuju), bez sati i bez računanja. Ne ulazi u provjere sati i cijene. |
| 4 | Doprinosi | Računaju se iz **platnih lista**, **nije uvijek isto**. | Doprinosi se **upisuju svaki mjesec** po radniku iz platne liste. Ne prenose se automatski u sljedeći mjesec (danas se prenose). Lista "Za provjeru" javlja radnika sa satima a bez doprinosa. |
| 5 | Očekivani sati po radniku | Sate **isključivo ručno** unose na osnovu **potpisanih sati**, jer je samo to mjerodavno. | Izvor sati u obračunu je **ručni unos iz potpisanih satnica**. Sati iz aplikacije (odobreni unosi radnog vremena) prestaju biti izvor i postaju **kontrola**: prikazuju se pored, uz razliku. Pokreće se promjena iz faze 2. |
| 6 | Značenje "SATNICA" i "SATNICA WH/TRANE" | Da (pretpostavka potvrđena). | Cijena radnika = kolona "SATNICA", cijena za klijenta = kolona "SATNICA WH/TRANE". Bez promjene. |
| 7 | Gorivo iz odobrenih troškova? | Gorivo se može **preuzimati sa DKV**. | Uvoz goriva po DKV izvodu je izvor goriva u obračunu. Uvoz sa mapiranjem kolona već postoji, a DKV raspored se potvrđuje na pravom izvodu. |

Sati iz aplikacije i dalje služe za prisustvo, lokaciju i procjenu troškova na kontrolnoj tabli. Za platu i naplatu mjerodavni su samo potpisani sati.

## 2. Ključne odluke koje proizilaze iz odgovora

1. **Sati u obračunu = ručni unos iz potpisanih satnica.** Automatsko punjenje iz radnog vremena postaje kontrola, ne izvor. Postavka "Izvor sati" (Ručno / Iz aplikacije) sa zadanim **Ručno**.
2. **Gradilište ima način obračuna prema klijentu.** Vrijednosti: satnica, paušal, aufmaß. Vezano za projekat, prenosi se u sekciju obračuna.
3. **Naplata za paušal i aufmaß = zbir izdanih računa.** Računi se unose ručno (broj, datum, iznos). Već postoji evidencija prihoda po projektu (`ProjectRevenue`); obračun je čita, a ne pravi novu.
4. **Doprinosi nikad nisu podrazumijevana vrijednost.** Uvijek se unose iz platne liste za taj mjesec.
5. **Kancelarija je fiksna stavka**, ne računski red.

## 3. Epici, MoSCoW i redoslijed

| Redoslijed | Epic | MoSCoW | Bodovi |
|---|---|---|---|
| 1 | A. Sati iz potpisanih satnica | Must | 21 |
| 2 | B. Način obračuna prema klijentu i naplata | Must | 26 |
| 3 | C. Doprinosi iz platnih lista | Must | 8 |
| 4 | D. Kancelarija kao fiksni red | Should | 8 |
| 5 | E. Godišnji i akontacija, jasno razdvojeni | Must | 5 |
| 6 | F. Gorivo sa DKV | Should | 13 |
| 7 | G. Kopiranje mjeseca po novim pravilima | Must | 5 |

Predlog isporuke: **Sprint 1** = A + E + G (promjene pravila u onome što već postoji), **Sprint 2** = B + C, **Sprint 3** = D + F. E i G su male, a A mijenja najosjetljivije pravilo pa ide prvo.

---

## Epic A: Sati iz potpisanih satnica

**Goal:** Obračun koristi samo sate koje je vlasnik upisao iz potpisanih satnica.
**Users affected:** Super Admin.
**Success metric:** Nijedan iznos zarade ne zavisi od sati iz aplikacije; razlika prema aplikaciji je vidljiva prije zaključivanja mjeseca.

### A1. Ručni unos sati po sedmici kao zadani izvor

**As a** vlasnik (Super Admin),
**I want to** da se sati u obračunu upisuju ručno po sedmici,
**So that** plata i naplata počivaju na potpisanim satnicama.

**Acceptance Criteria:**
- [ ] Given novi mjesec sa šablonom "Obračun radnika", when se napravi, then kolone sedmica su prazne, a ne popunjene iz radnog vremena.
- [ ] Given postavka "Izvor sati" = Ručno (zadano), when upisujem sate, then upis je običan unos, ne "korekcija", i ne pojavljuje se u "Za provjeru" samo zato što se razlikuje od aplikacije.
- [ ] Given postavka je Iz aplikacije, then ponaša se kao dosad (auto sa mogućnošću korekcije).
- [ ] Edge: mjesec koji je već obračunat po starom pravilu ostaje netaknut, a nova pravila važe od sljedećeg novog mjeseca.

**Notes / Out of Scope:** Skeniranje potpisanih satnica ide u A3.

**Story Points:** 5 · **Priority:** Critical

### A2. Sati iz aplikacije kao kontrola pored upisanih

**As a** vlasnik,
**I want to** vidjeti sate iz aplikacije pored svojih,
**So that** uočim propust prije nego što pošaljem obračun knjigovođi.

**Acceptance Criteria:**
- [ ] Given upisani sati i odobreni unosi radnog vremena, then u ćeliji ili detalju reda piše "u aplikaciji: N h" i razlika.
- [ ] Given razlika je veća od praga (podesivo, zadano 4 h u mjesecu), then red ulazi u "Za provjeru" sa oznakom "sati se razlikuju od aplikacije". Ne mijenja iznose.
- [ ] Given radnik nema aplikaciju (kooperant, kancelarija), then nema poređenja i nema upozorenja.
- [ ] Given nema unosa u aplikaciji a ima ručnih sati, then to nije greška, samo napomena "bez podataka iz aplikacije".

**Notes / Out of Scope:** Odobravanje unosa u aplikaciji ostaje odvojeno od obračuna.

**Story Points:** 8 · **Priority:** High

### A3. Potpisana satnica kao prilog

**As a** vlasnik,
**I want to** prikačiti sken potpisane satnice uz gradilište i sedmicu,
**So that** se za svaki iznos zna na šta se oslanja.

**Acceptance Criteria:**
- [ ] Given sekcija i sedmica, when dodam fajl (fotografija ili PDF), then se čuva uz tu sedmicu i vidi se oznaka "satnica priložena".
- [ ] Given zaključivanje mjeseca, then lista "Za provjeru" navodi sedmice sa satima a bez priloga (samo upozorenje, ne blokira).
- [ ] Edge: fajl veći od ograničenja se odbija sa porukom; brisanje priloga se upisuje u audit log.

**Notes / Out of Scope:** Prepoznavanje sati sa slike (OCR) nije u opsegu. Koristi postojeći modul priloga, uz pravila čuvanja iz [PRIVACY.md](PRIVACY.md).

**Story Points:** 8 · **Priority:** Medium

---

## Epic B: Način obračuna prema klijentu i naplata

**Goal:** Naplata se računa onako kako se stvarno obračunava sa klijentom: satnica, paušal ili aufmaß.
**Users affected:** Super Admin (unos i pregled), Admin/Voditelj (samo gradilište bez iznosa).
**Success metric:** Nema lažnih upozorenja za paušalna i aufmaß gradilišta; marža svake sekcije tačna prema izdanim računima.

### B1. Način obračuna na gradilištu

**As a** vlasnik,
**I want to** za svako gradilište označiti obračun (satnica / paušal / aufmaß),
**So that** sistem zna kako da računa naplatu.

**Acceptance Criteria:**
- [ ] Given gradilište, when otvorim postavke, then biram jedan od tri načina; zadano satnica.
- [ ] Given promjena načina usred mjeseca, then važi od sljedećeg obračuna, dok mjeseci u toku pokazuju upozorenje.
- [ ] Given uloga ispod Super Admina, then vidi naziv načina, ali ne cijene ni iznose.
- [ ] Edge: postojeća gradilišta dobijaju "satnica", a u "Za provjeru" se javlja ona sa cijenom za klijenta = prazno pa ih vlasnik pregleda jednom.

**Story Points:** 5 · **Priority:** Critical

### B2. Naplata za satnicu ostaje formula

**As a** vlasnik,
**I want to** da se za satnicu naplata računa sama,
**So that** ne računam sate × cijena za klijenta ručno.

**Acceptance Criteria:**
- [ ] Given sekcija sa načinom satnica, then naplata = sati × cijena za klijenta (kao dosad).
- [ ] Given sati bez cijene za klijenta u satnica-sekciji, then ulazi u "Za provjeru".

**Story Points:** 2 · **Priority:** Critical

### B3. Naplata za paušal i aufmaß iz izdanih računa

**As a** vlasnik,
**I want to** upisati izdane račune za paušalno ili aufmaß gradilište,
**So that** naplata odgovara onome što sam stvarno fakturisao.

**Acceptance Criteria:**
- [ ] Given sekcija sa načinom paušal ili aufmaß, then kolona naplate nije formula nego zbir računa upisanih za taj mjesec (broj računa, datum, iznos).
- [ ] Given nema računa za taj mjesec, then naplata je prazna i označena "nema računa", a ne 0, i javlja se u "Za provjeru".
- [ ] Given račun za više mjeseci (paušal), then vlasnik bira mjesec kojem se pripisuje, ili ga raspoređuje na dva mjeseca sa zbirom koji se mora slagati.
- [ ] Given isti račun je već unesen kroz prihode po projektu, then obračun ga koristi, a ne traži drugi unos (bez dupliranja prihoda).
- [ ] Edge: račun u drugoj valuti se odbija (sistem čuva jednu valutu); storno račun je negativan iznos i prikazuje se ispravno.
- [ ] Given ručno unesen iznos, then je označen kao "ručno" isto kao ostale korekcije.

**Notes / Out of Scope:** Pravljenje računa i e-račun nisu u opsegu.

**Story Points:** 13 · **Priority:** Critical

### B4. Provjere prilagođene načinu obračuna

**As a** vlasnik,
**I want to** upozorenja koja odgovaraju načinu obračuna,
**So that** ne pregledam lažne greške.

**Acceptance Criteria:**
- [ ] Given paušal/aufmaß sekcija sa satima a bez cijene za klijenta, then nema upozorenja.
- [ ] Given paušal/aufmaß sekcija sa radnicima a bez ijednog računa u mjesecu, then upozorenje "nema računa za mjesec".
- [ ] Given satnica sekcija bez cijene za klijenta, then upozorenje kao dosad.

**Story Points:** 6 · **Priority:** High

---

## Epic C: Doprinosi iz platnih lista

**Goal:** Doprinosi u obračunu odgovaraju platnoj listi tog mjeseca, ne prošlom.
**Users affected:** Super Admin.
**Success metric:** Nijedan doprinos ne stiže u novi mjesec bez ručne potvrde.

### C1. Doprinosi se upisuju svaki mjesec

**As a** vlasnik,
**I want to** upisati doprinose po radniku iz platne liste za taj mjesec,
**So that** obračun ne koristi stare iznose.

**Acceptance Criteria:**
- [ ] Given kopiranje mjeseca, then kolona doprinosa počinje prazna.
- [ ] Given radnik sa satima a bez doprinosa, then ulazi u "Za provjeru" ("nema doprinosa iz platne liste").
- [ ] Given radnik koji ne prima platu preko firme (kooperant), then je izuzet iz te provjere.
- [ ] Edge: nula je važeći unos (radnik bez doprinosa) i razlikuje se od praznog polja.

**Story Points:** 3 · **Priority:** Critical

### C2. Prijedlog iz prošlog mjeseca, samo kao pomoć

**As a** vlasnik,
**I want to** vidjeti prošlomjesečni doprinos pored praznog polja,
**So that** brže uočim veliko odstupanje.

**Acceptance Criteria:**
- [ ] Given upisan doprinos koji se od prošlog mjeseca razlikuje za više od praga (zadano 20%), then se prikaže blaga napomena, bez upozorenja u listi.
- [ ] Given nema prošlog mjeseca, then nema napomene.

**Story Points:** 2 · **Priority:** Low

### C3. Prilog platne liste (opcionalno)

**As a** vlasnik,
**I want to** prikačiti platnu listu uz radnika i mjesec,
**So that** se doprinos može provjeriti.

**Acceptance Criteria:**
- [ ] Given radnik i mjesec, when dodam fajl, then se čuva uz taj red uz pravila čuvanja podataka o platama.
- [ ] Given uloga ispod Super Admina, then prilog nije vidljiv.

**Story Points:** 3 · **Priority:** Low

---

## Epic D: Kancelarija kao fiksni red

**Goal:** Kancelarija se vodi kao fiksna stavka, bez sati i formula.
**Users affected:** Super Admin.
**Success metric:** Kancelarija ne izaziva nijedno upozorenje i prenosi se u sljedeći mjesec.

### D1. Vrsta reda "Kancelarija"

**As a** vlasnik,
**I want to** označiti red kao "Kancelarija" i upisati fiksni mjesečni iznos,
**So that** kancelarija ne prolazi kroz sate i cijene.

**Acceptance Criteria:**
- [ ] Given red vrste Kancelarija, then su polja zarada i prihod običan unos (ne računaju se), a sati i cijena se ne traže.
- [ ] Given kopiranje mjeseca, then fiksni iznos prelazi u novi mjesec (za razliku od doprinosa), uz mogućnost izmjene.
- [ ] Given zbir sekcije i firme, then iznosi kancelarije ulaze kao i ostali redovi.
- [ ] Given kancelarija nema sate ni cijenu za klijenta, then nema upozorenja u "Za provjeru".
- [ ] Edge: zarada i prihod kancelarije se ne moraju poklapati (vidi §3 tačka 4 u starom planu); to nije greška.

**Notes / Out of Scope:** Vrste Montir i Kooperant (broj × N) ostaju kao ranije otvorena stavka.

**Story Points:** 8 · **Priority:** Medium

---

## Epic E: Godišnji i akontacija

**Goal:** Godišnji i akontacija su dvije jasne kolone sa suprotnim predznacima.
**Users affected:** Super Admin.
**Success metric:** Zarada radnika = cijena × sati + godišnji + razlika − akontacija; rezultat firme ne zavisi od akontacije.

### E1. Odvojene kolone i predznaci

**As a** vlasnik,
**I want to** da se godišnji dodaje, a akontacija oduzima od plate,
**So that** iznos za isplatu odgovara stvarnosti.

**Acceptance Criteria:**
- [ ] Given šablon, then postoje kolone "Godišnji (+)" i "Akontacija (−)"; nema kolone koja mijenja naziv od sekcije do sekcije.
- [ ] Given godišnji 300 i akontacija 500 na 160 h × 20, then zarada = 3200 + 300 − 500 = 3000.
- [ ] Given akontacija, then rezultat firme se ne mijenja (već isplaćeno), a sažetak pokazuje iznos isplaćenih akontacija zasebno.
- [ ] Given negativna zarada poslije oduzimanja akontacije, then se prikazuje negativna vrijednost sa upozorenjem "akontacija veća od zarade" u "Za provjeru".
- [ ] Given izvoz u Excel, then kolone nose iste nazive i predznake.

**Notes / Out of Scope:** Evidencija samih isplata akontacija (datum) je odvojena stavka.

**Story Points:** 5 · **Priority:** High

---

## Epic F: Gorivo sa DKV

**Goal:** Gorivo u obračunu dolazi iz DKV izvoda, bez ručnog upisa u sažetak.
**Users affected:** Super Admin (uvoz), knjigovođa (izvoz).
**Success metric:** Sve DKV transakcije mjeseca su uvezene i raspoređene na vozilo i radnika ili označene kao neraspoređene.

### F1. Uvoz DKV izvoda

**As a** vlasnik,
**I want to** učitati DKV izvod za mjesec,
**So that** ne prepisujem gorivo.

**Acceptance Criteria:**
- [ ] Given izvod (CSV ili Excel) sa DKV portala, when ga učitam, then se prikaže pregled uvoza (koliko redova je novo, koliko već uvezeno, koliko nije prepoznato) prije potvrde.
- [ ] Given transakcija sa brojem kartice koja je vezana za vozilo, then ide na to vozilo i vozača; bez kartice ili sa nepoznatom karticom ide u "neraspoređeno" sa razlogom.
- [ ] Given ponovni uvoz istog izvoda, then nema duplih troškova.
- [ ] Given DKV mijenja raspored kolona, then mapiranje kolona može da se izmijeni bez izmjene koda.
- [ ] Edge: transakcija sa datumom iz drugog mjeseca ide u svoj mjesec; iznos u stranoj valuti se odbija sa jasnom porukom.

**Notes / Out of Scope:** Automatsko povlačenje preko DKV API-ja (zavisi od ugovora sa DKV) je zasebna, kasnija stavka. Modul uvoza sa mapiranjem već postoji, a ovdje se dodaje DKV predložak.

**Story Points:** 8 · **Priority:** High

### F2. Obračun uzima gorivo iz uvezenih transakcija

**As a** vlasnik,
**I want to** da kolona goriva u obračunu čita uvezene DKV transakcije,
**So that** gorivo bude tačno bez odobravanja svake pojedinačno.

**Acceptance Criteria:**
- [ ] Given uvezene DKV transakcije, then se smatraju potvrđenim (ne čekaju odobrenje) i ulaze u gorivo radnika kome je vozilo dodijeljeno tog mjeseca.
- [ ] Given neraspoređene transakcije u mjesecu, then upozorenje u "Za provjeru" sa iznosom.
- [ ] Given ručno uneseni troškovi goriva iz aplikacije, then i dalje ulaze uz oznaku, s upozorenjem na moguće dupliranje ako za isto vozilo i datum postoji DKV transakcija.

**Story Points:** 5 · **Priority:** High

---

## Epic G: Kopiranje mjeseca po novim pravilima

**Goal:** Novi mjesec počinje sa onim što se stvarno ponavlja, a bez onoga što se mijenja.
**Users affected:** Super Admin.

### G1. Šta se prenosi

**As a** vlasnik,
**I want to** da kopiranje mjeseca prenese samo ono što je stalno,
**So that** ne ostane pogrešan iznos iz prošlog mjeseca.

**Acceptance Criteria:**
- [ ] Given kopiranje mjeseca, then prenose se: struktura, cijene radnika, cijene za klijenta, način obračuna gradilišta, fiksni iznosi kancelarije, regres.
- [ ] Given kopiranje mjeseca, then ne prenose se: sati, doprinosi, godišnji, akontacija, razlika/bonus, računi za paušal i aufmaß.
- [ ] Given kopiranje, then rezultat prikazuje spisak "šta nije preneseno" da vlasnik zna šta upisuje.

**Story Points:** 5 · **Priority:** High

---

## 4. Rizici i zavisnosti

- **Promjena izvora sati mijenja osnovno pravilo faze 2.** Testovi za "sati iz radnog vremena" ostaju, ali važe kad je postavka "Iz aplikacije". Treba obezbijediti da nijedan izvještaj (widgeti, izvoz) ne miješa ručne sate sa satima iz aplikacije bez oznake.
- **Kontrolna tabla troškova** računa rad iz radnog vremena, pa se rezultat može razlikovati od obračuna. Treba jasno označiti "procjena iz aplikacije" (već je predviđeno u [WIDGETI_TROSKOVA_PLAN.md](WIDGETI_TROSKOVA_PLAN.md), oznaka "procjena").
- **DKV raspored izvoda je nepoznat** dok kupac ne pošalje primjer (bez stvarnih podataka o karticama u repozitorijumu, samo skinuta zaglavlja kolona). F1 zavisi od toga.
- **Potpisane satnice sadrže lične podatke.** Prilozi (A3, C3) podliježu pravilima čuvanja iz PRIVACY.md i vide ih samo Super Admin.
- **Paušal koji zahvata više mjeseci** traži pravilo raspodjele (B3); bez toga se vlasnik vraća ručnom računu.

## 5. Nova otvorena pitanja za kupca

1. **DKV:** može li poslati jedan stvarni izvod sa zaglavljima (brojeve kartica može prekriti)? Odobrava li se automatsko povlačenje preko DKV-a ili ostaje mjesečni uvoz?
2. **Paušal:** kada paušal traje više mjeseci, kako se dijeli po mjesecima (ravnomjerno, po završenim dijelovima, po računima)?
3. **Aufmaß:** je li dovoljan zbir računa, ili treba i vezati račun za aufmaß sheet (postoji tip "Aufmass" u sedmičnim izvještajima sa gradilišta)?
4. **Potpisane satnice:** čuvaju li se i skeniraju li se danas? Ko ih potpisuje (klijent, predradnik)?
5. **Sedmice preko granice mjeseca:** sate upisuje za sedmicu u cjelini, ili dijeli po datumu?
6. **Kooperanti:** ostaju li redovi bez imena ("Kooperanti × N") i kako se za njih vode potpisani sati?
7. **Prag razlike prema aplikaciji** (A2): 4 sata u mjesecu je početna pretpostavka; koliko je prihvatljivo?

## 6. Definition of Done za sve stavke

Implementirano i pregledano, jedinični i integracioni testovi prolaze (uključujući brojne primjere iz kupčeve tabele), kriteriji prihvatanja provjereni, dokumentacija (ovaj plan i EVIDENCIJA_OBRACUN_PLAN §8) ažurirana, release note napisan, isporučeno na test server i provjereno na mjesecu sa izmišljenim podacima.
