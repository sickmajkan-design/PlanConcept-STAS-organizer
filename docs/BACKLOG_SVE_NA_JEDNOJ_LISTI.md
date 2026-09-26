# Sve na jednoj listi

Stanje 2026-09-26. Objedinjuje: plan obračuna iz odgovora kupca ([PLAN_OBRACUN_ODGOVORI_KUPCA.md](PLAN_OBRACUN_ODGOVORI_KUPCA.md)), nove zahtjeve ([PLAN_NOVI_ZAHTJEVI_2026-09-26.md](PLAN_NOVI_ZAHTJEVI_2026-09-26.md)), nalaze QA prolaza kroz Android aplikaciju ([QA_ANDROID_2026-09-26.md](QA_ANDROID_2026-09-26.md)) i ono što čeka prije pravih podataka ([ODLUKE_PRIJE_PRAVIH_PODATAKA.md](ODLUKE_PRIJE_PRAVIH_PODATAKA.md), [PRODUCTION_READINESS_AUDIT.md](PRODUCTION_READINESS_AUDIT.md)).

Oznake: **Must** obavezno · **Should** važno · **Could** ako ostane vremena. Bodovi su procjena obima (1, 2, 3, 5, 8, 13). Stanje: Novo · Čeka kupca · Čeka vlasnika · Urađeno.

## A. Obračun mjeseca (odgovori kupca)

| # | Stavka | Prioritet | Bodovi | Zavisi od |
|---|---|---|---|---|
| A1 | Ručni unos sati po sedmici kao zadani izvor ("Izvor sati": Ručno) | Must | 5 | |
| A2 | Sati iz aplikacije samo kao kontrola pored upisanih | Must | 8 | A1 |
| A3 | Potpisana satnica kao prilog (sken po gradilištu i sedmici) | Should | 8 | A1 |
| A4 | Godišnji (+) i akontacija (−) kao dvije jasne kolone | Must | 5 | |
| A5 | Kopiranje mjeseca: ne prenosi sate, doprinose, godišnji, akontaciju, račune | Must | 5 | A1, A6 |
| A6 | Doprinosi se upisuju svaki mjesec iz platne liste | Must | 3 | |
| A7 | Prijedlog doprinosa iz prošlog mjeseca (samo pomoć) | Could | 2 | A6 |
| A8 | Prilog platne liste uz radnika i mjesec | Could | 3 | A6 |
| A9 | Način obračuna na gradilištu (satnica / paušal / aufmaß) | Must | 5 | |
| A10 | Naplata za satnicu ostaje formula sati × cijena | Must | 2 | A9 |
| A11 | Naplata za paušal i aufmaß = zbir izdanih računa | Must | 13 | A9 |
| A12 | Provjere prilagođene načinu obračuna ("Za provjeru") | Should | 6 | A9, A11 |
| A13 | Kancelarija kao fiksni red (vrsta reda) | Should | 8 | |
| A14 | DKV: uvoz izvoda sa predloškom za DKV | Should | 8 | DKV primjer izvoda |
| A15 | Gorivo u obračunu čita uvezene DKV transakcije | Should | 5 | A14 |

## B. Novi zahtjevi kupca

| # | Stavka | Prioritet | Bodovi | Zavisi od |
|---|---|---|---|---|
| B1 | Widget "Zahtjevi za odsustvo" sa dugmadima Odobri / Odbij | Must | 5 | |
| B2 | Widget "Aktivni projekti" (po statusu, uskoro završavaju, bez ekipe) | Should | 5 | |
| B3 | Obavještenja i brojači na widgetima (odsustva, narudžbe, refundacije) | Should | 3 | B1 |
| B4 | Refundacija: zahtjev sa računom i obrazloženjem (radi i bez signala) | Must | 8 | |
| B5 | Refundacija: pregled i odluka (odobri, odbij, djelimično) | Must | 5 | B4 |
| B6 | Refundacija: odobreni iznos u obračunu ("Refundacija (+)") | Must | 8 | B5, A1 |
| B7 | Refundacija: pregled statusa za radnika | Should | 5 | B4 |
| B8 | Godišnji: pravila (radni dani, praznici, srazmjerno pravo, prenos, pola dana) | Must | 8 | |
| B9 | Godišnji: stanje i istorija po radniku, korekcije | Must | 8 | B8 |
| B10 | Godišnji u obračunu plate (dani × sati × cijena) | Should | 5 | B8, A4 |
| B11 | Godišnji: izvještaj i podsjetnici | Could | 5 | B9 |
| B12 | Firme ispod klijenta | Should | 5 | |
| B13 | Raspored radnika u firmu | Should | 8 | B12 |
| B14 | Naziv firme uz ime radnika (panel, aplikacija, izvoz) | Should | 5 | B13 |
| B15 | Obračun i naplata po firmi klijenta | Could | 3 | B13, A11 |
| B16 | Narudžbe: zahtjev za artikle (web i aplikacija) | Should | 8 | |
| B17 | Narudžbe: odobravanje i statusi do dostave, obavještenja upravi | Should | 8 | B16 |
| B18 | Narudžbe: widget na kontrolnoj tabli | Could | 5 | B17 |
| B19 | Narudžbe: kupljena stavka postaje trošak | Could | 5 | B17 |
| B20 | Narudžbe: pregled statusa za radnika | Could | 5 | B16 |

## C. Aplikacija na telefonu (nalazi QA prolaza)

| # | Stavka | Prioritet | Bodovi |
|---|---|---|---|
| C1 | Ekran gradilišta: sakriti prazna polja, dodati "Otvori u mapama" | Should | 3 |
| C2 | Zadaci: prikaz prioriteta, otvaranje detalja, potvrda promjene stanja | Should | 5 |
| C3 | Raspored: redovi se mogu otvoriti, ispravan tekst "nastavlja se" | Could | 2 |
| C4 | Odjava sa smjene: pokazati da se traži položaj (traje do 10 s) | Should | 2 |
| C5 | Prevod: ekavica i ijekavica ujednačiti, poruke servera na srpskom | Should | 5 |
| C6 | Materijal ispod minimuma: oznaka u listi i u "Traži pažnju" (treba podatak iz API-ja) | Should | 5 |
| C7 | Prijava nedostatka: fotografija kao prvi korak, provjera na telefonu | Should | 3 |
| C8 | Ikone bez naziva (zvonce uz člana posade, megafon), ime autora umjesto e-pošte na oglasnoj ploči | Could | 3 |
| C9 | Nalog bez zaposlenika: ne prikazivati e-poštu dva puta | Could | 1 |
| C10 | Predradnik vidi radnika koji je prijavljen na tuđem gradilištu kao "bez prijave" | Could | 3 |
| C11 | Detaljna provjera lažnog signala (captive portal) u admin panelu | Could | 3 |

## D. Prije pravih podataka (čeka vlasnika)

| # | Stavka | Stanje |
|---|---|---|
| D1 | Pravni osnov za praćenje lokacije, obavještenje radnicima, procjena uticaja (DPIA), rok čuvanja, kontakt osoba | Čeka vlasnika i pravnika |
| D2 | Ugovori s obrađivačima (Google Firebase, hosting, S3 ako se uključi) | Čeka vlasnika |
| D3 | Firebase: `google-services.json` i servisni ključ za push (traži novi APK) | Čeka vlasnika |
| D4 | SMTP podaci za e-poštu (promjena lozinke, izvještaji) | Čeka vlasnika |
| D5 | Pravi keystore za potpisivanje APK-a (sada debug ključ; mijenja potpisnika, radnici moraju ponovo instalirati) | Čeka vlasnika |
| D6 | Predradnik i ostalo ograničenje pristupa: ostali podaci (resursi, odsustva) još nisu sužena po gradilištu | Odluka |
| D7 | Test na pravom telefonu: cijela smjena u džepu, GPS u pozadini, kamera na prijavi nedostatka | Novo |
| D8 | Backup: prvi pravi prijenos na S3 i vraćanje na drugoj mašini | Novo |
| D9 | Pilot: jedna ekipa, jedno gradilište, dvije sedmice | Novo |
| D10 | Nadzor: agregator logova, alarmi, staging | Novo |

## E. Tehnika

| # | Stavka | Prioritet |
|---|---|---|
| E1 | Testovi ekrana admin panela (5 pada po isteku vremena na sporoj mašini) | Could |
| E2 | Predložak DKV izvoda (čeka stvarni primjer) | Should |
| E3 | Dokumentacija: objediniti planove u ROADMAP nakon odluka kupca | Could |

## F. Pitanja za kupca (jedna lista)

1. **DKV:** može li poslati jedan izvod sa zaglavljima? Ostaje li mjesečni uvoz ili API?
2. **Paušal koji traje više mjeseci:** kako se dijeli po mjesecima?
3. **Aufmaß:** je li dovoljan zbir računa, ili treba vezati račun za aufmaß izvještaj?
4. **Potpisane satnice:** skeniraju li se? ~~Ko ih potpisuje~~ — **odgovor (2026-09-26): klijent potpisuje.**
5. ~~**Sedmica preko granice mjeseca**~~ — **odgovor: dijele se po datumu.** Tako već radi (`MonthWeeks` reže sedmicu na granici mjeseca), bez izmjene.
6. **Kooperanti:** ostaju li redovi bez imena, kako za njih potpisani sati?
7. **Prag razlike sati prema aplikaciji:** 4 sata u mjesecu?
8. **"Uprava"** = Voditelj, Admin, Super Admin?
9. ~~**Narudžbe:**~~ **odgovor: svaka uloga može naručivati. Naručuju se artikli koje radnik treba za posao (radne hlače, cipele, šljem, alat koji fali), ne građevinski materijal. Uprava naruči i pošalje; kad primalac dobije, sam pritisne „Dostavljeno“. Vidljivi statusi: Naručeno, U dostavi, Dostavljeno.**
10. **Refundacija:** gornja granica, valuta, uvijek kroz platu?
11. **Godišnji:** radni ili kalendarski dani, za koje države, koliko dana, srazmjerno pri zaposlenju usred godine, prenos i rok, iznos za dan.
12. **Klijent sa više firmi:** faktura svakoj posebno? Isti radnik u dvije firme u mjesecu?
13. **Odobravanje odsustava:** smije li Voditelj, ili samo Admin i Super Admin?
14. **Aktivni projekti widget:** šta uz broj po statusu (rok, budžet, broj radnika, nedostaci)?

## G. Predloženi redoslijed

1. **Sprint 1 (obračun, mala pravila):** A1, A2, A4, A5, A6.
2. **Sprint 2:** A9–A12 (način obračuna i naplata), B1, B2 (widgeti).
3. **Sprint 3:** B4–B7 (refundacije), B8–B10 (godišnji).
4. **Sprint 4:** B12–B15 (firme klijenta), A13, A14–A15 (kancelarija i DKV).
5. **Sprint 5:** B16–B20 (narudžbe).
6. **Paralelno, čim vlasnik odluči:** D1–D5, D7, D8. Aplikacija: C1–C7.

Ukupno u ovoj listi: 15 stavki obračuna (oko 88 bodova), 20 novih zahtjeva (oko 122), 11 stavki aplikacije (oko 35), 10 stavki za vlasnika i 3 tehničke.
