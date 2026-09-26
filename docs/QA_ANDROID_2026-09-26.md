# QA prolaz kroz Android aplikaciju, 2026-09-26

Ko je testirao: Claude, kao QA inženjer, na emulatoru (Pixel, Android, jezik uređaja engleski) protiv lokalnog API-ja i čiste baze.
Verzija: 1.1.8+10 (plus ispravke ispod). Uloge: Radnik, Predradnik, Voditelj projekta, Admin (SuperAdmin ima isti ekran kao Admin na mobilnom). Kupac (Customer) nema mobilnu aplikaciju, samo portal.

Ono što nije pokriveno: pravi telefon, GPS u pokretu, kamera (fotografija uz prijavu nedostatka), push (nije podešen), iOS.

## Ispravljeno u ovom prolazu

| # | Nalaz | Težina | Stanje |
|---|---|---|---|
| 1 | Dugme "Report" u prijavi nedostatka se nije crtalo u pravoj aplikaciji (tema daje filled dugmetu beskonačnu širinu, u `Row` ga to skriva). Prijava nedostatka nije mogla biti poslana. Testovi su prolazili jer koriste zadanu temu. | Kritično | Ispravljeno, test sada koristi temu aplikacije i pada bez ispravke |
| 2 | Radnik nije imao nikakav put do prijave nedostatka: dugme je bilo samo na ekranu gradilišta, a radnik nema karticu Projekti, a redovi rasporeda se ne mogu otvoriti. Funkcija koju samo radnik smije da napravi bila je nedostupna radniku. | Visoko | Ispravljeno: stavka "Prijavi nedostatak" na početnom ekranu, bira se gradilište iz današnjeg rasporeda |
| 3 | Polje za pauzu pri odjavi: nula ostaje pa "30" postane "030" | Nisko | Ispravljeno, nula je odabrana pri otvaranju |
| 4 | Traka "nema veze / N čeka slanje" nije bila vidljiva na ekranima izvan glavne navigacije (npr. Odsustva), a na ostalim je ostavljala prazan razmak ispod trake | Srednje | Ispravljeno, traka je iznad cijele aplikacije |
| 5 | Fotografija uz prijavu snimljena bez veze ostajala je u kešu kamere, koji Android briše | Srednje | Ispravljeno, kopira se u trajni folder i briše nakon slanja |

Provjereno na emulatoru poslije ispravki: prijava nedostatka s početnog ekrana stigla je na server sa tačnim gradilištem i položajem.

## Otvoreno, po težini

### Visoko
- **Početni ekran je lista od 15 do 17 linkova**, ne nadzorna ploča. Radnik na početnom ekranu ne vidi ni tekuću smjenu ni današnje zadatke. Predlog: gornja kartica "Danas" (smjena, dugme prijava/odjava, gradilište, broj otvorenih zadataka) pa tek ispod meni.
- **Tehnička poruka svima**: "Push delivery is not configured in this build" prikazuje se radnicima i predradnicima. To je poruka za administratora. Treba je sakriti od svih osim SuperAdmin/Admin.
- **Predradnik vidi sva gradilišta i sve zaposlene**, ne samo svoja (Marko je raspoređen samo na Zgradu A, vidi i Zgradu B). To je otvorena odluka iz audita (H11); lokacija je već ograničena, ostalo nije.
- **Prijava nedostatka bez fotografije prolazi bez upozorenja**, a tekst na ekranu kaže da je slika obično cijela prijava. Razmisliti da fotografija bude prvi korak.

### Srednje
- **Ekran gradilišta**: pet redova sa "-" (klijent, adresa, datumi) zauzimaju cijeli ekran prije nego što se stigne do posade i prijave nedostatka. Sakriti prazna polja. Nedostaje "Otvori u mapama" / navigacija do koordinata, što je prva potreba radnika koji prvi put dolazi na gradilište.
- **Zadaci ("Moje")**: prioritet se ne vidi (zadatak "Visok" izgleda isto kao "Normalan"), kartica se ne može otvoriti, stanje ("Open") i radnja ("In progress") izgledaju isto kao dva čipa. Nema potvrde nakon promjene stanja.
- **Raspored**: redovi se ne mogu otvoriti; tekst "26.09.2026. – 09.10.2026. · Runs on" ostaje obješen ("Runs on" bez čega). Bolje: "Nastavlja se poslije".
- **Odjava sa smjene traje do 10 sekundi bez indikatora** (čeka GPS 8 sekundi, dugme je samo sivo). Treba spinner ili tekst "Tražim položaj".
- **Poruka servera na engleskom** unutar srpskog ekrana ("The break is as long as the shift...") jer API ne poznaje jezik.
- **Srpski**: miješaju se ekavica i ijekavica ("Deljenje lokacije", ali "smjena", "vrijeme", "obavještenja"). Odabrati jedno.
- **Nalog bez zaposlenika prikazuje e-poštu dva puta** (ime i e-pošta su isti tekst) na kartici profila (Voditelj projekta, Admin).
- **Materijal ispod minimuma nema oznaku u listi** (Armaturna mreža 8 kom, minimum 10).
- **Pomoćne ikone bez naziva**: zvonce pored svakog člana posade, megafon u obavještenjima (samo Admin). Treba tekst ili opis.

### Nisko
- Kartice nepročitanih obavještenja su tamnije od pročitanih (obično je obrnuto) i djeluju kao onemogućene.
- Filter čipovi nemaju stanje "Sve" niti izabrano stanje po zadatku; "Terminated" se odsijeca (klizanje postoji, ali se ne vidi da postoji).
- Na Voditelju projekta / Adminu nema "Radno vrijeme" jer nisu zaposlenici; ispravno, ali nigdje ne piše zašto.
- Tekuća smjena se prikazuje dvaput (kartica smjene i prvi red istorije).
- "Prijava van lokacije gradilišta" prikazano je crvenom oznakom radniku; vjerovatno treba blaži ton za radnika, a jasan za voditelja.
- Prijava: kada se uđe s greškom, polja se pomjere pa je teže ispraviti unos na malom ekranu.

## Šta radi dobro
- Tok bez veze radi kako je zamišljeno na emulatoru: izbor gradilišta iz keša, snimanje smjene s trenutkom pritiska, zahtjev za odsustvo, slanje po povratku veze, ispravno gradilište i vrijeme na serveru.
- Jasan ton boja (topla smeđa na krem podlozi), dobar kontrast teksta, dugmad visine 52 dp za rukavice, ravnomjerni razmaci, prazni ekrani imaju ikonu i objašnjenje.
- Prevod na srpski je potpun; oblici množine su ispravni.
- Uloge vide prave tabove: Radnik tri, Predradnik pet, Voditelj i Admin četiri; Admin ima "+" za gradilište i slanje oglasa.

## Napomena o "taste" pristupu
Tasteskill je pisan za web (landing stranice), a sam u odjeljku 13 kaže da nije za nativne mobilne aplikacije i ekrane s gustim podacima. Zato su primijenjeni samo opći principi koji vrijede i ovdje (hijerarhija, kontrast, stanja praznog/učitavanja/greške, dosljednost oblika, ciljevi dodira), uz okvir za kritiku dizajna. Sekcije o hero-u, GSAP-u, eyebrow-ima i slično ne važe.
