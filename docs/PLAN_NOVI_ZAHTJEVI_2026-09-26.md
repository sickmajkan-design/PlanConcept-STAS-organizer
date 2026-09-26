# Novi zahtjevi kupca: kontrolna tabla, narudžbe, klijent sa više firmi, godišnji odmori, refundacije

Sastavljeno 2026-09-26 (Product Owner). Ulaz: šest novih zahtjeva kupca (El Plan Concept GmbH). Vezano uz [PLAN_OBRACUN_ODGOVORI_KUPCA.md](PLAN_OBRACUN_ODGOVORI_KUPCA.md): dva zahtjeva (godišnji i refundacije) završavaju u obračunu mjeseca.

## 1. Šta je već u sistemu, a šta je novo

| Zahtjev | Postoji | Novo |
|---|---|---|
| Widget: zahtjevi za odsustvo sa dugmetom za odobravanje | Odsustva sa tokom zahtjev, odobravanje, prijedlog izmjene; widget "Stanje odsustava"; widget "Traži pažnju" | Widget sa listom zahtjeva i dugmadima Odobri / Odbij direktno na kontrolnoj tabli |
| Widget: aktivni projekti i slično | Widgeti "Projekat u fokusu", "Realizacija projekata", "Projekti van budžeta" | Widget "Aktivni projekti" (broj po statusu, uskoro završavaju, bez ekipe) |
| Naručivanje artikala | Ništa (materijali su stanje magacina, ne narudžba) | Cijeli tok: zahtjev za nabavku, odobravanje, kupovina, dostava radniku, obavještenja |
| Klijent sa više firmi, radnik u posebnoj firmi | Klijent (`Customer`), projekat pripada klijentu, radnik se raspoređuje na projekat | Firme ispod klijenta; raspored radnika u firmu; naziv firme pored imena radnika |
| Godišnji odmori | Odsustva tipa godišnji, godišnje pravo po radniku (zadano 20), stanje = odobreni dani iz kalendara | Pravila obračuna (radni dani, praznici, prenos, srazmjerno pravo), istorija, korekcije, isplata u obračunu |
| Refundacije | Ništa | Zahtjev sa računom, odobravanje, povećanje plate u obračunu za odobreni iznos |

## 2. Uloge i pretpostavke

Uloge u sistemu: Super Admin, Admin, Voditelj projekta, Predradnik, Radnik, Kupac (portal). Kupčev pojam **"uprava"** tumačim kao **Voditelja projekta i iznad** (Voditelj, Admin, Super Admin). Ovo treba potvrditi (pitanje P-1). Obavještenja "uprava, admin i super admin" idu Voditelju, Adminu i Super Adminu.

Kupac je firma koja **posuđuje radnike** klijentima (njihovi obračuni su po klijentu i firmi). Zato "firma ispod klijenta" znači pravno lice klijenta kod kojeg radnik stvarno radi, sa posljedicama po fakturisanje.

## 3. Redoslijed isporuke

| Redoslijed | Epic | MoSCoW | Bodovi | Zašto ovim redom |
|---|---|---|---|---|
| 1 | K. Kontrolna tabla: odsustva i projekti | Must | 13 | Male stavke na postojećem; odmah vidljiva vrijednost |
| 2 | R. Refundacije | Must | 26 | Ima direktan efekat na platu; zavisi od obračuna (Sprint 1 obračuna) |
| 3 | G. Godišnji odmori | Must | 26 | Pravila utiču na platu; zavisi od praznika i obračuna |
| 4 | F. Klijent sa više firmi | Should | 21 | Mijenja model podataka; treba prije nego što ima puno radnika |
| 5 | N. Narudžbe artikala | Should | 34 | Najveća nova površina (web, aplikacija, obavještenja) |

Preporuka: K prvi, zatim R i G paralelno sa obračunom, F prije ozbiljnog unosa radnika po firmama, N kao zaseban projekat sa vlastitim planom testiranja.

---

## Epic K: Kontrolna tabla

**Goal:** Uprava rješava odsustva i vidi stanje projekata bez odlaska na druge stranice.
**Users affected:** Super Admin, Admin, Voditelj projekta.
**Success metric:** Zahtjev za odsustvo se odobrava u dva dodira sa kontrolne table; nijedan zahtjev ne ostaje neviđen duže od jednog dana.

### K1. Widget "Zahtjevi za odsustvo"

**As a** Voditelj / Admin / Super Admin,
**I want to** vidjeti zahtjeve za odsustvo koji čekaju i odobriti ili odbiti ih iz widgeta,
**So that** ne moram otvarati stranicu odsustava.

**Acceptance Criteria:**
- [ ] Given zahtjevi u stanju "Traženo", then widget lista radnika, vrstu, datume, broj dana i preklapanje sa drugim odsustvima na istom gradilištu.
- [ ] Given dugme Odobri, when ga pritisnem, then zahtjev je odobren, radnik dobija obavještenje, a stavka nestaje iz liste bez ponovnog učitavanja stranice.
- [ ] Given dugme Odbij, then traži kratko obrazloženje (obavezno) i šalje ga radniku.
- [ ] Given odobrenje bi premašilo godišnje pravo radnika, then prije potvrde piše upozorenje sa brojem preostalih dana i traži se potvrda.
- [ ] Given dva korisnika istovremeno obrade isti zahtjev, then drugi dobija poruku "već obrađeno" i lista se osvježi.
- [ ] Edge: sopstveni zahtjev ne može odobriti isti korisnik (postojeće pravilo); dugmad su onemogućena uz objašnjenje.
- [ ] Uloga: vidi samo Voditelj i iznad; Predradnik ne odobrava.

**Notes / Out of Scope:** Prijedlog izmjene datuma ostaje na stranici odsustava.

**Story Points:** 5 · **Priority:** High

### K2. Widget "Aktivni projekti"

**As a** Voditelj / Admin / Super Admin,
**I want to** pregled projekata po statusu,
**So that** vidim šta je aktivno, šta uskoro završava i šta je bez ekipe.

**Acceptance Criteria:**
- [ ] Given projekti, then widget prikazuje broj po statusu (planirano, aktivno, na čekanju, završeno) i listu aktivnih sa brojem raspoređenih.
- [ ] Given aktivni projekat bez raspoređenih radnika, then je označen "bez ekipe".
- [ ] Given projekat kojem ističe krajnji datum u narednih 14 dana (podesivo), then je označen.
- [ ] Given dodir na projekat, then otvara se njegova stranica.
- [ ] Widget ima postavke (koliko redova, koji statusi), kao i ostali widgeti.

**Story Points:** 5 · **Priority:** Medium

### K3. Obavještenja i brojači na widgetima

**As a** uprava,
**I want to** da zvonce i widgeti pokažu nove zahtjeve (odsustva, narudžbe, refundacije) odmah,
**So that** ništa ne čeka neopaženo.

**Acceptance Criteria:**
- [ ] Given novi zahtjev bilo koje vrste, then stiže obavještenje odgovarajućim ulogama i brojač na widgetu se ažurira.
- [ ] Given zahtjev je obrađen, then obavještenja se označe pročitanim za sve primaoce.

**Story Points:** 3 · **Priority:** Medium

---

## Epic R: Refundacije

**Goal:** Radnik koji je platio iz svog džepa dobija novac natrag kroz platu, uz dokaz i odobrenje.
**Users affected:** Radnik, Predradnik (podnose), uprava (odobrava), Super Admin (obračun).
**Success metric:** Svaka odobrena refundacija se pojavljuje tačno jednom u obračunu za mjesec isplate; nijedna neodobrena ne ulazi.

### R1. Podnošenje zahtjeva sa računom

**As a** radnik,
**I want to** poslati zahtjev za refundaciju sa slikom računa i obrazloženjem,
**So that** mi se vrati novac koji sam potrošio za posao.

**Acceptance Criteria:**
- [ ] Given aplikacija, when biram "Refundacija", then unosim iznos, datum troška, obrazloženje (obavezno), gradilište (opciono) i najmanje jednu fotografiju računa.
- [ ] Given nema signala, then se zahtjev čuva na telefonu i šalje kasnije (isti mehanizam kao prijava nedostatka), sa ključem koji sprječava dupli zahtjev.
- [ ] Given iznos ≤ 0 ili bez računa, then se zahtjev ne šalje i piše zašto.
- [ ] Given isti račun (isti iznos, datum i slika) već poslan, then sistem upozorava na moguće dupliranje.
- [ ] Given zahtjev poslan, then uprava dobija obavještenje (Voditelj, Admin, Super Admin) sa iznosom i radnikom.
- [ ] Edge: iznos iznad praga (podesivo) traži dodatno obrazloženje.
- [ ] Edge: račun stariji od roka (zadano 60 dana) se prihvata sa upozorenjem.

**Notes / Out of Scope:** Iznos u stranoj valuti u prvoj verziji nije podržan (sistem čuva jednu valutu). Prepoznavanje iznosa sa slike nije u opsegu.

**Story Points:** 8 · **Priority:** Critical

### R2. Pregled i odluka

**As a** Voditelj / Admin / Super Admin,
**I want to** pregledati račun i odobriti, odbiti ili djelimično odobriti refundaciju,
**So that** plaćamo samo opravdane troškove.

**Acceptance Criteria:**
- [ ] Given zahtjev, then vidim iznos, obrazloženje, sliku računa u punoj veličini i historiju zahtjeva tog radnika.
- [ ] Given Odobri, then bira se odobreni iznos (zadano puni) i mjesec isplate (zadano tekući), a radnik dobija obavještenje.
- [ ] Given Odbij, then je obrazloženje obavezno i radnik ga vidi.
- [ ] Given djelimično odobrenje, then se piše razlog razlike.
- [ ] Given zahtjev odobren, then ga nije moguće izmijeniti bez opoziva (opoziv se bilježi u revizijski zapis).
- [ ] Given sopstveni zahtjev, then ga ne može odobriti isti korisnik.

**Story Points:** 5 · **Priority:** Critical

### R3. Refundacija u obračunu plate

**As a** vlasnik,
**I want to** da se odobrena refundacija sama doda platnoj stavci radnika za mjesec isplate,
**So that** ne upisujem je ručno.

**Acceptance Criteria:**
- [ ] Given odobrena refundacija za mjesec, then obračun ima posebnu kolonu "Refundacija (+)" na redu tog radnika i sažetak je sabira odvojeno od zarade.
- [ ] Given odobrena a neisplaćena refundacija za zaključen mjesec, then ulazi u prvi otvoreni mjesec sa oznakom.
- [ ] Given refundacija, then ne ulazi u sate, cijenu rada ni maržu klijentu, ali ulazi u trošak firme (gradilišta ako je navedeno, inače opšti trošak).
- [ ] Given radnik sa refundacijom a bez reda u obračunu tog mjeseca, then "Za provjeru" javlja "refundacija bez reda".
- [ ] Given ručna izmjena iznosa u obračunu, then se označava kao ručna korekcija.
- [ ] Edge: negativna vrijednost (povrat) je moguća samo kao korekcija koju Super Admin unosi sa razlogom.

**Notes / Out of Scope:** Poreski tretman refundacije određuje knjigovođa; sistem samo prikazuje iznos odvojeno.

**Story Points:** 8 · **Priority:** Critical

### R4. Pregled statusa za radnika

**As a** radnik,
**I want to** vidjeti status svojih zahtjeva,
**So that** znam hoće li i kada dobiti novac.

**Acceptance Criteria:**
- [ ] Given moje zahtjeve, then vidim status (poslano, odobreno, odbijeno, isplaćeno u mjesecu X) i razlog odluke.
- [ ] Given odluka, then stiže obavještenje u aplikaciji.

**Story Points:** 5 · **Priority:** High

---

## Epic G: Godišnji odmori

**Goal:** Evidencija i računanje godišnjih odmora prema pravilima države, sa istorijom i vezom na platu.
**Users affected:** Radnik (pregled), Voditelj/Admin (evidencija), Super Admin (obračun).
**Success metric:** Preostali dani svakog radnika se poklapaju sa ručnim proračunom knjigovođe za uzorak od 10 radnika.

### G1. Pravila godišnjeg odmora

**As a** vlasnik,
**I want to** postaviti pravila računanja godišnjeg,
**So that** stanje odgovara stvarnom pravu radnika.

**Acceptance Criteria:**
- [ ] Given postavke firme, then biram računanje u **radnim danima** (bez vikenda i državnih praznika radnikove države) ili u kalendarskim danima.
- [ ] Given radnik zaposlen usred godine, then pravo za tu godinu se računa srazmjerno mjesecima zaposlenja (pravilo podesivo).
- [ ] Given neiskorišteni dani iz prošle godine, then se prenose do datuma isteka (podesivo, npr. 30. juni), poslije čega propadaju.
- [ ] Given praznik koji pada usred odsustva, then se ne troši dan godišnjeg.
- [ ] Given pola dana odsustva, then je moguće upisati 0,5 dana.
- [ ] Edge: država radnika bez unesenih praznika daje upozorenje "praznici nisu uneseni za državu X" umjesto tihog pogrešnog računa.

**Notes / Out of Scope:** Pravila po državama (Njemačka, Slovenija, Hrvatska, BiH) treba potvrditi (pitanje P-4). Prve verzije daju samo podesive parametre.

**Story Points:** 8 · **Priority:** Critical

### G2. Stanje i istorija po radniku

**As a** Voditelj / Admin,
**I want to** vidjeti za radnika pravo, prenos, iskorišteno, planirano i preostalo, sa istorijom,
**So that** odgovaram radniku i knjigovođi.

**Acceptance Criteria:**
- [ ] Given radnik, then stanje po godini: pravo, prenos iz prošle godine, iskorišteno (odobreno i proteklo), planirano (odobreno u budućnosti), na čekanju, preostalo.
- [ ] Given ručna korekcija stanja (npr. dogovoreni dodatni dani), then je unose Admin i iznad sa razlogom, a bilježi se u revizijski zapis.
- [ ] Given radnik u aplikaciji, then vidi svoje stanje i istoriju, ne tuđe.
- [ ] Given kraj godine, then se prenos računa i prikazuje kao stavka za pregled prije potvrde.
- [ ] Edge: odsustvo preko granice godine dijeli dane po godinama.

**Story Points:** 8 · **Priority:** High

### G3. Godišnji u obračunu plate

**As a** vlasnik,
**I want to** da se iskorišteni godišnji odmor sam pretvori u iznos u koloni "Godišnji (+)",
**So that** ne računam ga ručno.

**Acceptance Criteria:**
- [ ] Given odobreni dani godišnjeg u mjesecu, then iznos = dani × dnevni sati (podesivo, zadano 8) × cijena radnika za taj mjesec.
- [ ] Given ručni upis, then ima prednost i označen je kao ručna korekcija.
- [ ] Given radnik bez cijene rada, then polje ostaje prazno i ulazi u "Za provjeru".
- [ ] Given isplata neiskorištenog godišnjeg pri prestanku rada, then je posebna stavka koju unosi Super Admin uz broj dana.

**Notes / Out of Scope:** Pravilo o plaćanju godišnjeg (kupac: "godišnji se dodaje na platu") potvrđeno u odgovorima; način računanja iznosa treba potvrditi (P-5).

**Story Points:** 5 · **Priority:** High

### G4. Izvještaj i podsjetnici

**As a** uprava,
**I want to** izvještaj o godišnjim odmorima i podsjetnike,
**So that** neiskorišteni dani ne propadnu neopaženo.

**Acceptance Criteria:**
- [ ] Given izvještaj, then lista radnika sa preostalim danima i datumom isteka prenosa, uz izvoz u Excel.
- [ ] Given prenos ističe za 30 dana, then radnik i Voditelj dobijaju podsjetnik.

**Story Points:** 5 · **Priority:** Low

---

## Epic F: Klijent sa više firmi

**Goal:** Jedan klijent može imati više firmi, a svaki radnik je raspoređen u tačno jednu, uz vidljiv naziv firme.
**Users affected:** Super Admin, Admin (podešavanje); Voditelj, Predradnik (vide naziv); Kupac (portal).
**Success metric:** Uz svakog raspoređenog radnika u panelu, aplikaciji i izvozu piše firma; obračun se može razdvojiti po firmi.

### F1. Firme ispod klijenta

**As a** Super Admin / Admin,
**I want to** klijentu dodati više firmi,
**So that** vodim posebne pravne subjekte istog klijenta.

**Acceptance Criteria:**
- [ ] Given klijent, then dodajem firme sa nazivom, poreskim i registarskim brojem i adresom fakturisanja (polja klijenta se ponašaju kao zadana vrijednost firme).
- [ ] Given klijent bez dodatnih firmi, then sve radi kao dosad (jedna implicitna firma).
- [ ] Given firma sa raspoređenim radnicima, then se ne može obrisati, samo označiti neaktivnom.
- [ ] Given uloga ispod Admina, then ne mijenja firme.

**Story Points:** 5 · **Priority:** High

### F2. Raspored radnika u firmu

**As a** Admin,
**I want to** pri rasporedu radnika na projekat izabrati firmu,
**So that** se zna kod koga stvarno radi.

**Acceptance Criteria:**
- [ ] Given projekat klijenta sa više firmi, when raspoređujem radnika, then je izbor firme obavezan; sa jednom firmom se bira sama.
- [ ] Given radnik raspoređen bez firme (stari zapisi), then ostaje važeći, a lista "Za provjeru" ih navodi za dopunu.
- [ ] Given promjena firme usred mjeseca, then se čuva datum promjene, a sati se dijele po datumu.
- [ ] Given radnik na dva projekta, then može biti u različitim firmama na svakom.

**Story Points:** 8 · **Priority:** High

### F3. Naziv firme uz ime radnika

**As a** Voditelj / Predradnik,
**I want to** vidjeti u kojoj firmi radi svaki radnik,
**So that** ne pogrešim pri dnevnom radu.

**Acceptance Criteria:**
- [ ] Given liste radnika (panel, aplikacija "Ekipa danas", ekipa gradilišta, tabla rasporeda), then uz ime piše naziv firme (skraćeno, sa punim nazivom u detalju).
- [ ] Given klijent sa jednom firmom, then se naziv ne ponavlja uz svako ime.
- [ ] Given izvoz u Excel, then postoji kolona "Firma".

**Story Points:** 5 · **Priority:** High

### F4. Obračun i naplata po firmi

**As a** vlasnik,
**I want to** da obračun i izdani računi mogu biti razdvojeni po firmi klijenta,
**So that** fakturišem pravom pravnom licu.

**Acceptance Criteria:**
- [ ] Given sekcija obračuna, then je vezana za klijenta i firmu; "OBRT FIRMA" iz kupčeve tabele odgovara toj firmi.
- [ ] Given računi (vidi epic B obračuna), then se pripisuju firmi.

**Notes / Out of Scope:** Zavisi od epica B iz plana obračuna. Portal za klijenta po firmi je zasebna stavka.

**Story Points:** 3 · **Priority:** Medium

---

## Epic N: Narudžbe artikala

**Goal:** Svaka uloga može zatražiti artikle, a uprava vidi tok od zahtjeva do dostave radniku.
**Users affected:** Svi zaposleni (podnose i potvrđuju prijem), uprava, Admin, Super Admin (odobravaju i nabavljaju).
**Success metric:** Svaki zahtjev ima vidljiv status i istoriju; uprava dobija obavještenje pri svakoj promjeni stanja.

### N1. Zahtjev za artikle

**As a** zaposleni (bilo koje uloge),
**I want to** zatražiti jedan ili više artikala,
**So that** nabavka zna šta mi treba i za koje gradilište.

**Acceptance Criteria:**
- [ ] Given aplikacija ili panel, then unosim stavke (naziv, količina, jedinica, napomena, fotografija opciono), gradilište i hitnost (obično / hitno).
- [ ] Given nema signala, then se zahtjev čuva i šalje kasnije bez duplikata.
- [ ] Given zahtjev poslan, then uprava, Admin i Super Admin dobijaju obavještenje.
- [ ] Given radnik bez raspoređenog gradilišta, then gradilište nije obavezno.
- [ ] Edge: prazna lista stavki ili količina ≤ 0 se odbija sa porukom.

**Story Points:** 8 · **Priority:** High

### N2. Odobravanje i status

**As a** uprava / Admin / Super Admin,
**I want to** odobriti ili odbiti zahtjev i voditi ga kroz statuse,
**So that** se zna ko šta nabavlja.

**Statusi:** Zatraženo → Odobreno (ili Odbijeno) → Naručeno → Kupljeno → Dostavljeno radniku. Nakon Dostavljeno, radnik potvrđuje prijem.

**Acceptance Criteria:**
- [ ] Given zahtjev, then je moguće odobriti, odbiti (sa razlogom), djelimično odobriti stavke i mijenjati status po redoslijedu.
- [ ] Given promjena statusa, then podnosilac i uprava/Admin/Super Admin dobijaju obavještenje sa novim statusom.
- [ ] Given status "Kupljeno", then se bilježi ko je kupio, kada i iznos sa slikom računa (opciono).
- [ ] Given status "Dostavljeno", then radnik u aplikaciji potvrđuje "primio sam"; bez potvrde ostaje "čeka potvrdu" i podsjeća se poslije dva dana.
- [ ] Given istovremena obrada, then drugi dobija poruku "već promijenjeno".
- [ ] Edge: nije moguće preskakati statuse; opoziv unatrag samo Admin i iznad, u revizijskom zapisu.

**Story Points:** 8 · **Priority:** High

### N3. Widget "Narudžbe" na kontrolnoj tabli

**As a** uprava,
**I want to** widget sa zahtjevima po statusu,
**So that** vidim šta čeka odluku ili nabavku.

**Acceptance Criteria:**
- [ ] Given zahtjevi, then widget prikazuje broj po statusu i listu onih koji čekaju odluku, sa dugmadima Odobri / Odbij.
- [ ] Given dodir na zahtjev, then se otvara detalj sa istorijom.

**Story Points:** 5 · **Priority:** Medium

### N4. Trošak nabavke

**As a** vlasnik,
**I want to** da kupljena stavka postane trošak gradilišta ili opšti trošak,
**So that** troškovi budu tačni bez dvostrukog unosa.

**Acceptance Criteria:**
- [ ] Given status "Kupljeno" sa iznosom, then se nudi da se zapiše kao trošak gradilišta ili opšti trošak jednim dodirom.
- [ ] Given isti iznos već upisan kao trošak, then upozorenje na dupliranje.

**Notes / Out of Scope:** Katalog artikala i dobavljači nisu u opsegu prve verzije.

**Story Points:** 5 · **Priority:** Low

### N5. Pregled za radnika

**As a** radnik,
**I want to** vidjeti status svojih zahtjeva,
**So that** znam da li stiže ono što sam tražio.

**Acceptance Criteria:**
- [ ] Given moji zahtjevi, then vidim status i istoriju; obavještenje stiže pri svakoj promjeni.

**Story Points:** 5 · **Priority:** Medium

---

## 4. Matrica prava (predlog)

| Radnja | Radnik | Predradnik | Voditelj | Admin | Super Admin |
|---|---|---|---|---|---|
| Podnijeti zahtjev za artikle / refundaciju | Da | Da | Da | Da | Da |
| Vidjeti svoje zahtjeve | Da | Da | Da | Da | Da |
| Vidjeti tuđe zahtjeve | Ne | Svoja gradilišta | Da | Da | Da |
| Odobriti odsustvo | Ne | Ne | Da | Da | Da |
| Odobriti narudžbu / refundaciju | Ne | Ne | Da | Da | Da |
| Voditi nabavku (status kupljeno, dostavljeno) | Ne | Ne | Da | Da | Da |
| Refundacija u obračunu (iznosi) | Ne | Ne | Ne | Ne | Da |
| Firme klijenta, korekcije godišnjeg | Ne | Ne | Ne | Da | Da |

## 5. Rizici

- **Novac:** refundacija i godišnji mijenjaju platu, pa svaka promjena mora imati revizijski zapis i mogućnost provjere iz izvoza.
- **Lični podaci:** slike računa i zahtjevi za odsustvo su lični podaci; čuvanje po pravilima iz PRIVACY.md, vide ih samo ovlaštene uloge.
- **Model podataka klijent-firma-projekat-radnik** je najosjetljiviji dio; raditi ga prije nego što se uneše mnogo radnika, uz migraciju koja zatečene raspoređene stavi u implicitnu firmu.
- **Praznici po državama:** bez potpunog kalendara praznika godišnji se računa pogrešno; potrebna potvrda država i unos za više godina.
- **Obim narudžbi:** najveća nova površina; isporučivati po dijelovima (N1 i N2 prvo).

## 6. Pitanja za kupca

- **P-1.** Ko je "uprava"? Voditelj projekta, Admin i Super Admin, ili neko drugi?
- **P-2.** Narudžbe: šta znači "svaku rolu"? Da li svaka **uloga** može zatražiti artikle (radnik, predradnik, voditelj...), ili svaka **rola** (kao artikal, rola materijala)? Plan pretpostavlja prvo.
- **P-3.** Refundacija: postoji li gornja granica bez dodatnog odobrenja, i u kojoj valuti se troši (radnici rade u više zemalja)? Isplaćuje li se uvijek kroz platu, ili ponekad gotovinom?
- **P-4.** Godišnji: računa li se u radnim ili kalendarskim danima, za koje države (SLO, DE, HR, BiH), koliko dana pravo (zadano 20), srazmjerno pri zaposlenju usred godine, prenos u sljedeću godinu i do kada?
- **P-5.** Godišnji u plati: iznos za dan godišnjeg je cijena radnika × 8 h, ili drugačije (prosjek prethodnih mjeseci)?
- **P-6.** Klijent sa više firmi: fakturišete li svakoj firmi zasebno? Može li radnik u istom mjesecu raditi za dvije firme istog klijenta?
- **P-7.** Zahtjevi za odsustvo na kontrolnoj tabli: smije li Voditelj odobravati, ili samo Admin i Super Admin?
- **P-8.** Aktivni projekti: šta uz broj po statusu želite vidjeti na prvi pogled (rok, budžet, broj radnika, nedostaci)?

## 7. Definition of Done

Implementirano i pregledano; jedinični i integracioni testovi prolaze, uključujući provjere prava po ulozi i revizijski zapis za sve izmjene novca; kriteriji prihvatanja provjereni; dokumentacija i release note napisani; isporučeno na test server i provjereno na izmišljenim podacima.
