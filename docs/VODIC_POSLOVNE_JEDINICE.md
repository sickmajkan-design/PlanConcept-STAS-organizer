# Vodič: poslovne jedinice

Za administratore i Super Admina. Ovaj vodič opisuje kako se u platformi vode **poslovne jedinice**: šta su, kako ih postaviti, kako im dodijeliti ljude i gradilišta, i kako se to vidi u izvještajima, fakturama, platnom spisku, obavještenjima i aplikaciji na telefonu.

Isti sadržaj, skraćeno, nalazi se u samoj aplikaciji: ikonica pomoći gore desno, kartica **Poslovne jedinice**.

---

## 1. Šta je poslovna jedinica

**Poslovna jedinica** je dio vaše vlastite organizacije: filijala, ogranak, predstavništvo u drugoj državi ili zasebno pravno lice u okviru iste grupe. Svaka može imati svoje podatke (adresu, JIB/PIB, vlasnika, kontakt), svoja gradilišta, svoje zaposlene, svoje troškove i prihode.

> Poslovna jedinica **nije** firma klijenta. "Firma klijenta" je pravno lice vašeg naručioca kojem fakturišete. Poslovna jedinica je vaša. U aplikaciji se zato nikad ne miješaju: **jedinica** ima obojenu tačku ispred naziva, **firma klijenta** ima okvir iza naziva.

Dva tipa:

| Tip | Šta znači | Brojevi na dokumentima |
|---|---|---|
| **Zasebno pravno lice** | Ima vlastitu registraciju i poreske brojeve | Samo njeni. Ako broj nije upisan, ostaje prazan, nikad se ne posuđuje tuđi |
| **Predstavništvo** | Nije samostalno pravno lice (npr. ured u drugoj državi) | Što nema svoje (registrovani naziv, JIB/PIB, matični i PDV broj), uzima se od firme ("Podaci firme") |

## 2. Gdje se sve nalazi

**Administracija > Firma i poslovne jedinice** — jedna stranica, dvije kartice:

* **Podaci firme**: logo, naziv, adresa, poreski brojevi, kontakt. Mijenja ih samo Super Admin; Admin ih može pročitati.
* **Poslovne jedinice**: tabela svih jedinica (tip, mjesto, broj gradilišta, broj zaposlenih, status) i sve radnje nad njima.

Firma je "matični subjekt", jedinice su njeni dijelovi. Stari linkovi (`/company-settings`, `/branches`) i dalje rade i vode ovdje.

## 3. Postavljanje, redom

### 3.1 Dodajte jedinice
Kartica **Poslovne jedinice > Dodaj poslovnu jedinicu**.

1. **Naziv** (kratki, kakav ga ljudi koriste), **tip** i **boja** (tačka koja se vidi svuda).
2. **Naziv i adresa**: registrovani naziv za dokumente, adresa, grad, poštanski broj, država.
3. **Poreski i registracioni podaci** (JIB/PIB, matični broj, PDV broj): vide ih samo Super Admin i računi kojima je to pravo posebno dodijeljeno; **upisuje ih samo Super Admin**. Predstavništvo može ostaviti prazno.
4. **Kontakt**: vlasnik ili direktor, kontakt osoba, telefon, e-mail, napomena.

Jedinica koju više ne koristite **isključuje se** (prekidač "Aktivna"), ne briše: briše se samo ona koja nema gradilišta.

### 3.2 Dodijelite gradilišta
Na redu jedinice: ikona **Dodijeli gradilišta**. Označite glavna gradilišta (grupisana po kupcu; kvačica na kupcu označava sva njegova). **Podgradilišta uvijek prate glavno.** Gradilište koje je već u drugoj jedinici premješta se, uz napomenu "sada u: …". Pojedinačno se jedinica bira i u obrascu gradilišta.

### 3.3 Dodijelite zaposlene
Na redu jedinice: ikona **Dodijeli zaposlene**.

* Zaposleni su grupisani prema jedinici u kojoj su sada; grupa **"Još ni u jednoj jedinici"** se označi jednim klikom.
* Kvačica **"Od datuma zaposlenja"** (uključena) znači: svako ko još nije bio ni u jednoj jedinici počinje od svog datuma zaposlenja, pa **sve što je radio ranije također pripada toj jedinici**. Za prvo punjenje to je ono što treba. Isključena, svi počinju od datuma koji upišete.
* Pomjeraju se **samo označeni**; ostali zadržavaju svoju jedinicu.
* Radnja je **sve ili ništa**: ako se datum ne uklapa u istoriju jednog zaposlenog, ne pomjera se niko.

Pojedinačno: stranica zaposlenog, kartica **Poslovna jedinica (zaposlen u)**; novom radniku se jedinica bira već pri unosu.

### 3.4 Što je sa istorijom
Zaposlenje u jedinici ima **datum od-do**. Prebacivanje iz jedinice A u B od 1. juna zatvara A na 31. maja i otvara B od 1. juna. Zato:

* sati, plate i troškovi iz prošlosti ostaju u jedinici koja je tada zapošljavala osobu;
* nema preklapanja ni rupa u istoriji;
* datum koji se ne uklapa (prije početka trenutnog perioda, preko ranijeg perioda, u budućnosti) **platforma odbija** umjesto da tiho promijeni prošlost;
* ispravka je: obrisati pogrešan period (ikona kante u istoriji), pa unijeti ispravno. Ako se unese isti dan kada je period počeo, to se smatra ispravkom i zamjenjuje jedinicu.

### 3.5 Vozila, alati, troškovi, prihodi, smještaj
Imaju polje **Poslovna jedinica**.

* **Vozilo i alat**: prazno znači "prati gradilište na koje je dodijeljeno"; popunjeno ima prednost.
* **Ostali troškovi, ručne isplate, prihodi firme**: prazno znači "prati svoje gradilište, ako ga ima".
* **Smještaj**: jedinica kojoj se knjiži zakup.

## 4. Svakodnevni rad

### 4.1 Izbor jedinice u zaglavlju
Gore desno je izbornik **Sve poslovne jedinice**. Odabir jedinice sužava: liste (gradilišta, zaposleni, vozila, alati, satnice, nalozi, materijal, fakture, narudžbe), troškove, prihode, grafikone, upozorenja o budžetu, mapu uživo, raspored, odsustva, tablu dodjela, hijerarhiju, smještaj i platne spiskove. Izbor se pamti po računu.

To je **filter za pregled, a ne zasebna prijava**: svako ko ima pravo pristupa i dalje može otvoriti svaki zapis.

### 4.2 Sati i plate: po gradilištu ili po poslodavcu
Kad je jedinica odabrana, u istom meniju piše **"Sati i plate se računaju po"**:

* **Gradilištu**: računa se ono što je odrađeno na gradilištima te jedinice, bez obzira ko zapošljava ljude.
* **Poslodavcu**: računa se ono što su odradili ljudi koje je jedinica **tog dana** zapošljavala, gdje god su radili.

Zašto oboje: radnik zaposlen u A može raditi na gradilištu jedinice B. Gradilište B je mjesto naplate, jedinica A plaća radnika. Oba pogleda su tačna i služe različitim pitanjima. Sve što nije vezano za ljude (materijal, vozila, alati, ostali troškovi, prihod) pripada svojoj jedinici u oba slučaja. Izvještaji **po gradilištu** ostaju po gradilištu.

Primjer: osoba je do 31. maja u jedinici A, od 1. juna u B, a radi na gradilištu jedinice C. Po gradilištu je svih 16 sati kod C. Po poslodavcu je 8 sati kod A (mart) i 8 kod B (juli).

Uz ime jedinice piše **"po poslodavcu"** kad je taj pogled uključen.

### 4.3 Zbirovi se slažu
Zbir jedinica ne može biti veći od cjeline. Ono što ne pripada nijednoj jedinici vidi se samo pod **Sve poslovne jedinice**. Provjera: ukupan trošak = zbir jedinica + ono bez jedinice.

## 5. Izvještaji, izvozi i dokumenti

* **Excel izvozi** (sati, gradilišta, vozila, alati, zaposleni, troškovi po gradilištu/vozilu/alatu, promet materijala, finansijski unosi) imaju kolonu **"Poslovna jedinica"** na kraju tabele (da se ne pomjere kolone koje već koristite) i prate izbor u zaglavlju.
* **Fakture**: lista pokazuje jedinicu uz svaku fakturu. Dugme **"Pregled / štampa"** otvara dokument sa podacima jedinice kao izdavaoca (naziv, adresa, brojevi); predstavništvo bez svojih brojeva štampa brojeve firme, uz napomenu. "Štampaj / sačuvaj kao PDF" štampa samo dokument.
  * Pažnja: program **evidentira** fakture, ne izdaje ih. Dokument je izvod iz evidencije i **ne zamjenjuje fakturu koju izdaje knjigovodstvo**; to piše na dnu.
  * Poreske brojeve na dokumentu vide samo oni koji ih smiju vidjeti.
* **Platni spisak (Evidencija)**: svaki mjesec može pripadati jedinici (pri kreiranju ili na oznaci ispod naslova). Prvi nacrt jedinice sadrži ljude koje je ona zapošljavala tog mjeseca, na kojem god gradilištu da su radili. Provjera upozorava na svakoga koga jedinica nije zapošljavala, uz naziv jedinice koja jeste. Kopiran mjesec zadržava jedinicu. (Evidencija je dostupna samo Super Adminu.)

## 6. Obavještenja

* **Promjena jedinice**: zaposleni koji ima nalog dobija obavijest (push i u aplikaciji): "Zaposleni ste u jedinici X od datuma" ili "Od datuma niste raspoređeni ni u jednu poslovnu jedinicu". Obavijest ide samo onima čija se jedinica zaista promijenila, i tek nakon što je sve sačuvano.
* **Obavijest jedinici**: u obrascu opšte obavijesti postoji izbor **Poslovna jedinica**; poruka ide ljudima koje ta jedinica trenutno zapošljava (može se kombinovati sa ulogom i gradilištem).
* **Lista "Još treba podesiti"** (početna, administratori): kad postoji bar jedna jedinica, javlja koliko je radnika i gradilišta bez jedinice, a link vodi na dodjelu.

## 7. Telefon (aplikacija 1.1.15)

* Na kartici zaposlenog i na gradilištu piše **Poslovna jedinica**.
* Radnik dobija obavijest o promjeni jedinice, na svom jeziku.
* Stara verzija aplikacije nastavlja raditi; samo ne prikazuje jedinicu.

## 8. Ko šta smije

| Radnja | Super Admin | Admin | Voditelj, poslovođa |
|---|---|---|---|
| Vidi nazive i boje jedinica (za filter) | da | da | da |
| Vidi adresu, kontakt, vlasnika jedinice | da | da | ne |
| Vidi JIB/PIB, matični i PDV broj | da | samo uz dodijeljeno pravo | ne |
| Upisuje JIB/PIB, matični i PDV broj | da | ne (ostaju kakvi jesu) | ne |
| Dodaje, mijenja, isključuje, briše jedinice | da | da | ne |
| Dodjeljuje gradilišta i zaposlene | da | da | ne |
| Mijenja podatke firme (logo, brojevi) | da | ne (samo čita) | ne |
| Platni spisak (Evidencija) | da | ne | ne |

## 9. Česta pitanja

**Radnik radi na gradilištu jedinice B, a zaposlen je u A. Gdje se računa?**
Zavisi od izbora u zaglavlju: po gradilištu je u B, po poslodavcu u A. Oba su tačna.

**Greškom sam prebacio radnika. Šta sada?**
Na stranici zaposlenog obrišite pogrešan period u istoriji i unesite ispravno. Ako je prebacivanje bilo danas, dovoljno je ponovo ga prebaciti istim datumom.

**Zašto platforma ne dopušta datum u prošlosti?**
Jer bi preko ranijeg perioda prepisala koja je jedinica platila te sate. Obrišite period koji smeta, pa unesite ispravan datum.

**Gdje je radnik koji nije ni u jednoj jedinici?**
Vidi se samo pod "Sve poslovne jedinice", a na početnoj stranici u listi "Još treba podesiti". Dodijelite ga preko "Dodijeli zaposlene".

**Moram li imati jedinice?**
Ne. Bez jedinica sve radi kao do sada. Čim ih dodate, lista "Još treba podesiti" pokazuje šta je ostalo bez jedinice.

**Predstavništvo nema JIB/PIB. Kako se štampa faktura?**
Uzimaju se brojevi firme iz "Podaci firme", uz napomenu na dokumentu. Zato prvo upišite podatke firme.

---

*Verzija vodiča: 2026-10-02. Platforma: poslovne jedinice, uključujući zaposlene, sate i platne spiskove. Aplikacija na telefonu: 1.1.15.*
