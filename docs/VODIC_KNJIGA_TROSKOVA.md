# Vodič: Knjiga troškova (Evidencija novca)

Knjiga zamjenjuje Excel listu u kojoj se novac vodio po mjesecima. Ideja u tri rečenice:

1. **Svaki prihod, trošak, uplata i isplata upiše se samo jednom**, kao jedan red u Knjizi.
2. Sve ostalo (zbirovi, dobit, ko nam duguje, obračun mjeseca) program **računa sam** iz tih redova.
3. Zato nema formula koje se pokvare i sekcija koja izostane iz zbira.

**Ko je vidi:** samo **Super Admin**, dok on ne uključi pristup nekom nalogu (Administracija > Korisnici > izmjena naloga). Pravo na finansije (iznosi) samo po sebi ne otvara Knjigu.

**Šta program ne radi:** ne vodi porez, PDV ni doprinose i nije zamjena za knjigovođu. Vaša je evidencija novca; knjigovođa i dalje radi zvanično knjigovodstvo, a od vas dobija uredan izvoz.

U aplikaciji: izbornik **Knjiga troškova**, stranica **Kako radi** (ista objašnjenja stoje i na samim ekranima, u rubrici „Kako ovo radi“) i uputstvo „Knjiga troškova“ (Ctrl+K).

---

## 1. Kako izgleda jedan mjesec

| Korak | Gdje | Šta se dešava |
|---|---|---|
| 1. **Unesi** | Knjiga zapisa | brzi unos; gorivo, smještaj i vozila dolaze sami |
| 2. **Program računa** | Pregled, Obračun | prihodi, troškovi, dobit i obračun po gradilištima |
| 3. **Provjeri** | Obračun > Za provjeru | lista šta ne štima |
| 4. **Zaključaj** | Obračun | mjesec se više ne mijenja; ispravka ostavlja trag |
| 5. **Izvezi** | Knjiga zapisa | Excel i ZIP s dokumentima za knjigovođu |

---

## 2. Pregled novca i dobiti

Naslovni ekran: **ulaz, izlaz i dobit** za izabrani period, s prikazom „od sati do dobiti“, grafikonom po mjesecima i statistikom troškova.

- Period birate dugmadima (npr. *Zadnja 3 mjeseca*) ili ručno **Od** i **Do**. Svi brojači i grafikoni prate izabrani period; klik na mjesec u grafikonu bira baš taj mjesec.
- **Tok novca u narednim danima:** redoslijed očekivanih uplata i isplata. Pokazuje kad plata radnika izlazi prije naplate klijenta i da li stanje pada ispod nule. Upišite koliko imate na računu i vidite najnižu tačku.
- **Procjena sljedećeg mjeseca:** predviđa ulaz, izlaz i dobit iz prosjeka zadnja tri zaključana mjeseca. Sate možete mijenjati: „Šta ako u novembru radimo 3.500 sati?“

---

## 3. Knjiga zapisa i brzi unos

Jedan red na vrhu Knjige: **datum, vrsta, opis, gradilište, druga strana, iznos**. Zbirovi se odmah promijene.

### Vrste zapisa

| Vrsta | Mijenja dobit? | Šta radi |
|---|---|---|
| **Prihod** (izdata faktura) | da | povećava ulaz; ostaje otvoreno dok se ne naplati |
| **Trošak** | da | povećava izlaz |
| **Uplata** | ne | klijent je platio: smanjuje otvorena potraživanja |
| **Isplata** | ne | plaćen je dobavljač ili radnik: smanjuje otvorene obaveze |

### Izvor zapisa

Oznaka odakle red dolazi: **ručno**, **DKV izvod**, **smještaj**, **vozila**, **šablon** (ponavljajući trošak). Gorivo s DKV izvoda i kirija stana nastaju sami; ručno upisujete samo ostalo. Zapisi koji nastaju sami mijenjaju se tamo gdje nastaju (strelica umjesto olovke).

### Filteri i pretraga

Prikaz samo prihoda, troškova, uplata ili isplata; pretraga po opisu, gradilištu ili drugoj strani. Iz **Ctrl+K** pretraga zapisa vodi ovdje, već filtrirana.

### Primjer

Upišite: *Gorivo sedmica 41*, kategorija *Gorivo*, gradilište *Hala B*, iznos **1.980 €**. Zbir troškova i dobit se odmah promijene, a obračun Hale B pokazuje novu maržu.

### Kategorije i pravila

**Kategorija** kaže o kakvom je novcu riječ i u koji dio obračuna ide (naplata klijentu, plata radnika, troškovi gradilišta, fiksni troškovi). **Pravila** pogađaju kategoriju iz opisa: upišite „Gorivo, DKV izvod“ i program predloži kategoriju. Kategorije na koje se sistem oslanja mogu se preimenovati, ali ne isključiti.

---

## 4. Ko nam duguje, kome dugujemo

Otvorene fakture klijentima, plate radnika i računi dobavljača, s **rokovima i kašnjenjem**. **Označi plaćeno** prebacuje fakturu u plaćene i mijenja zbirove. Odavde se priprema i tekst podsjetnika klijentu.

---

## 5. Obračun mjeseca

Tabela po gradilištima kao u Excelu, ali **izračunata iz Knjige**. Sate upisujete iz potpisanih satnica.

- **Način naplate po gradilištu:** *satnica* (sati × cijena), *paušal* ili *mjerenje* (iznos izdatih faktura). Program pokaže i koliko bi bilo po satnici.
- **Za provjeru:** previše sati, paušal bez fakture, kašnjenje. Označite riješeno ono što jeste. Obračun upozorava i na troškove proknjižene bez dokumenta (samo podsjetnik).
- **Zaključaj mjesec:** **tabela obračuna** (po gradilištima, s ukupnim zbirom) se zamrzava; ispravka traži otključavanje i upisuje ko je i kada. Zaključan mjesec se ne može obrisati. Pažnja: zaključavanje ne zabranjuje nove redove u Knjizi. Red dodat poslije vidi se u Knjizi zapisa i u listama ispod tabele (npr. upozorenje o troškovima bez dokumenta), ali ne mijenja zaključanu tabelu. Zato prije zaključavanja završite unose.

### Primjer

*Zgrada D* je paušal. Prebacite je na satnicu i odmah vidite razliku u marži. Kad su sve kućice u *Za provjeru* označene, zaključajte oktobar.

---

## 6. Isplate radnicima

- **Akontacija** može biti više puta u mjesecu, bilo kome, **gotovinom ili prenosom**. Upisuje se kad je isplaćena, pa ne može imati datum u budućnosti.
- **Konačna isplata** = zarada minus akontacije, s datumom i načinom po grupi (radnici, kooperanti, kancelarija).

Primjer: dodajte dvije akontacije radnicima i vidite šta ostaje za konačnu isplatu.

---

## 7. Ponavljajući troškovi

Šablon za trošak koji se ponavlja (kirija, fiksni troškovi, zakup, osiguranje, pretplate). Program ga **sam upisuje u Knjigu svakog mjeseca**. Dodajte šablon i pogledajte šta nastaje u sljedećem mjesecu. Zapisi se prave za tekući ili raniji mjesec, ne za mjesec koji tek dolazi.

---

## 8. Uvoz starih mjeseci iz Excela

Prebacuje stare mjesece iz tabele **u četiri koraka**, s provjerom svakog reda prije upisa.

1. **Datoteka.** Prevucite Excel u okvir ili **Izaberi datoteku** (isprobati možete s *Učitaj primjer*). Datoteka se čita u pregledniku i ne čuva.
2. **Kolone.** Program pogodi koja kolona je šta (sati po sedmicama, cijene, akontacija…); ispravite gdje pogriješi.
3. **Provjera.** *Upozorenje* (žuto) se uvozi, ali ga treba pogledati; *Greška* (crveno) se ne uvozi.
4. **Potvrda.** Uvoz se može **poništiti**, osim ako je mjesec poslije ručno mijenjan ili je zaključan.

Pravila: uvozi samo **Super Admin**. Mjesec koji već ima zapise uvozi se samo u prazna polja (ili izaberite drugi mjesec). Najviše je dozvoljen ograničen broj redova po datoteci.

### Primjer

Tabela za septembar ima gradilišta u redovima i četiri sedmice u kolonama. Učitajte je, kolone sedmica označite kao *Sati (sedmica)*, ispravite gradilište koje je pogrešno prepoznato i potvrdite. Septembar se pojavi kao obračun s istim brojkama.

---

## 9. Izvoz za knjigovođu

Na stranici **Knjiga zapisa**:

- **Izvoz** preuzima Excel tabelu zapisa koje ste filtrirali, iste brojke kao na ekranu.
- **Izvoz > Izvoz s dokumentima (ZIP)** preuzima tabelu **i dokumente priložene uz zapise**, po jedna mapa za svaki zapis. Kratka napomena u ZIP-u navodi koliko troškova nije imalo priložen dokument.

Dokumente uz zapis dodajete isto kao i drugdje ([vodič o dokumentima](VODIC_DOKUMENTI.md)); na listi se vidi broj dokumenata uz zapis.

### Primjer: kraj oktobra

1. *Obračun mjeseca* > riješite listu *Za provjeru* > **Zaključaj oktobar**.
2. *Knjiga zapisa* > period 01.10.–31.10. > **Izvoz > Izvoz s dokumentima (ZIP)**.
3. Pošaljite ZIP knjigovođi.

---

## 10. Česta pitanja

**Zašto ne vidim Knjigu?** Zatvorena je za sve osim Super Admina dok ne dobijete pristup.

**Greškom sam upisao trošak.** Olovkom ga izmijenite, ili obrišite; u zaključanom mjesecu prvo otključajte (upisuje se ko i zašto).

**Gorivo se pojavilo samo od sebe.** Dolazi iz DKV izvoda (izvor *DKV izvod*); mijenja se tamo gdje nastaje.

**Uplata klijenta nije promijenila dobit.** Tako i treba: dobit mijenjaju prihod (faktura) i trošak, a uplata samo smanjuje otvorena potraživanja.
