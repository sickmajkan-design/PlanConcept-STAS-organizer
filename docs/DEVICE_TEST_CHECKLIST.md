# Test na pravom telefonu / Real-device test pass

**SR** — Odjava je bila potpuno pokvarena, a 260 automatskih testova je
prolazilo. Našlo se tek kad je neko uzeo telefon i pritisnuo dugme. Ovaj
dokument je pokušaj da se ostatak aplikacije provuče kroz istu vrstu provere,
redom i bez preskakanja — jer sve što nije ovde prošlo, nije provereno.

**EN** — Sign-out was completely broken while 260 automated tests passed. It
was found the first time somebody held a phone and pressed the button. This
document puts the rest of the app through the same kind of check, in order,
without skipping — because anything not ticked here has not been verified.

Ništa se ne popravlja u toku prolaza. Zapiši i idi dalje; popravke idu posle,
zajedno. / Fix nothing during a pass. Write it down and move on; fixes come
afterwards, together.

---

## 0. Priprema / Setup

### 0.1 APK

1. GitHub → **Actions** → poslednji zeleni run na grani
   `claude/construction-workforce-phase-1-diz0zx`
2. Dole, **Artifacts** → `construction-organizer-debug-apk`
3. Raspakuj, prebaci `app-debug.apk` na telefon, otvori iz menadžera fajlova.
   Android će tražiti dozvolu za „nepoznate izvore" — to je normalno za
   debug-potpisan build. / Unzip, copy `app-debug.apk` to the phone, open it
   from a file manager. Android will ask to allow unknown sources; that is
   expected for a debug-signed build.

> **Deinstaliraj prethodnu verziju pre instalacije.** Sesija ostaje u keystore-u
> između instalacija, a ovaj prolaz počinje od prijave. / **Uninstall the
> previous version first.** The session survives in the keystore between
> installs, and this pass starts from sign-in.

### 0.2 Server

Server mora slušati na adresi koju telefon vidi, ne samo na `localhost`:

```bash
# na mašini gde radi server / on the machine running the server
dotnet run --project src/Construction.API --urls http://0.0.0.0:5000

# proveri sa telefona, u pregledaču / check from the phone's browser
# http://<ip-mašine>:5000/health/live
```

IP mašine: `ip addr` (Linux), `ipconfig` (Windows). Telefon i mašina moraju biti
na istom Wi-Fi-ju. / The phone and the machine must be on the same Wi-Fi.

### 0.3 Nalozi / Accounts

Za pun prolaz trebaju **tri** naloga, jer se pola aplikacije ponaša drugačije po
ulozi. Napravi ih iz admin panela (`/users`): / A full pass needs **three**
accounts, because half the app behaves differently per role. Create them from
the admin panel (`/users`):

| Uloga / Role | Vidi / Sees | Za šta treba / Needed for |
|---|---|---|
| **Worker** | samo svoje / only their own | prolaz 2, i provera da direktorijum **nije** dostupan |
| **Foreman** | + direktorijum, troškovi / + directory, spending | prolaz 3 |
| **Admin** | sve / everything | prolaz 7 |

Worker i Foreman **moraju biti povezani sa zaposlenim** (`employeeId`), inače
nema ni radnog vremena ni GPS-a. / Worker and Foreman **must be linked to an
employee**, or neither time tracking nor GPS exists for them.

---

## 1. Kako zapisati grešku / How to record a failure

Za svaku stavku koja padne, zapiši ovih pet stvari. Bez njih se greška obično
ne može reprodukovati. / For every item that fails, write down these five.
Without them a failure usually cannot be reproduced.

1. **Šta si radio** — ekran, dugme, redosled. / What you did.
2. **Šta se desilo** — tačan tekst poruke, ili „ništa se nije desilo".
   / What happened — the exact message, or "nothing happened".
3. **Šta si očekivao.** / What you expected.
4. **Vreme** (na minut) — da bi se našlo u logu servera. / The time, to the
   minute, so it can be found in the server log.
5. **Mreža** — pun signal, slab, avionski režim. / Network state.

„Ništa se nije desilo" je potpuno validan nalaz i bio je tačan opis najozbiljnije
greške do sada. / "Nothing happened" is a valid finding and was the accurate
description of the worst bug so far.

### Ako hoćeš i log / If you want the log as well

```bash
# telefon povezan USB-om, USB debugging uključen
adb logcat -s flutter:V

# server, u drugom terminalu — traži correlation id iz istog minuta
# the server, in another terminal — look for the correlation id from that minute
```

Aplikacija sama prijavljuje neuhvaćene greške na `/api/v1/client-errors`, pa se
mnoge vide u logu servera i bez `adb`. / The app reports uncaught errors to
`/api/v1/client-errors` by itself, so many appear in the server log without
`adb`.

---

## 2. Prolaz 1 — prijava i temelj / Sign-in and the basics

Nalog: **bilo koji**. / Account: **any**.

- [ ] Prvo otvaranje traži adresu servera; unesi je i sačuvaj.
      *Očekivano:* prihvata `http://192.168.x.x:5000`, odbija adresu bez
      `http://`. / *Expected:* accepts the address, refuses one without a scheme.
- [ ] Pogrešna lozinka → poruka o grešci, ne prazan ekran.
- [ ] **Deset** pogrešnih pokušaja zaredom → nalog se zaključava, poruka to
      kaže. *(Otključati iz admin panela pre nastavka.)* / Ten wrong attempts
      in a row locks the account.
- [ ] Tačna lozinka → početni ekran, tvoje ime i uloga na kartici.
- [ ] **Promeni jezik** (Nalog → Jezik / Account → Language) na engleski i
      nazad na srpski. *Očekivano:* menja se ceo ekran, uključujući donju
      navigaciju, bez restarta.
- [ ] Ubij aplikaciju i otvori je ponovo. *Očekivano:* i dalje si prijavljen,
      i dalje na izabranom jeziku.
- [ ] **Odjava** (Nalog → Odjavi se). *Očekivano:* odmah, u istom trenutku,
      ekran za prijavu. Ne posle par sekundi.
- [ ] Ponovi odjavu **sa isključenim Wi-Fi-jem**. *Očekivano:* isto tako
      trenutno. Ovo je ispravka iz `C9` i vredi je proveriti baš ovako.
- [ ] Posle odjave ubij i otvori aplikaciju. *Očekivano:* ekran za prijavu,
      ne početni.

## 3. Prolaz 2 — dan radnika / A worker's day

Nalog: **Worker**. Ovo je putanja koju će pravi ljudi koristiti dva puta dnevno.

- [ ] Donja navigacija ima **samo** Početna i Obaveštenja. *Očekivano:*
      Zaposleni i Projekti se **ne vide** — API bi ih ionako odbio.
- [ ] **Radno vreme** → prijavi dolazak. *Očekivano:* ekran pokazuje da je
      smena u toku, sa vremenom početka.
- [ ] Vrati se na početnu, pa opet na Radno vreme. *Očekivano:* smena je i
      dalje u toku.
- [ ] Ubij aplikaciju, otvori je, otvori Radno vreme. *Očekivano:* i dalje u
      toku — stanje je na serveru, ne u aplikaciji.
- [ ] Odjavi smenu. *Očekivano:* otvara se list za pauzu; **dodirni izvan
      lista i povuci nadole** — *očekivano:* list se **ne zatvara**, jedino
      Otkaži i Potvrdi rade. Potvrdi; trajanje je izračunato i tačno.
      *(Ispravka `C10` — zatvaranje dodirom izvan lista je izgledalo kao da
      dugme ne radi.)*
- [ ] Pokušaj **dva puta zaredom** prijaviti dolazak. *Očekivano:* drugi put
      je odbijeno sa razumljivom porukom, ne sa greškom 500.
- [ ] **Moji zadaci** → otvori zadatak, promeni status.
- [ ] **Prijavi defekat** sa **fotografijom iz kamere**. *Očekivano:* slika se
      šalje; vidi se u admin panelu na tom zadatku.
- [ ] Isto, ali **slika iz galerije**.
- [ ] **Moj raspored** — pokazuje gde si raspoređen narednih 14 dana.
- [ ] **Odsustva** → zatraži godišnji odmor. Probaj i datum „do" **pre** datuma
      „od". *Očekivano:* odbijeno pre slanja.
- [ ] Povuci zahtev koji čeka odgovor. *Očekivano:* uspeva.
- [ ] **Skeniraj** — kamera pročita QR nalepnicu sa alata ili vozila.
      *Očekivano:* traži dozvolu za kameru prvi put; posle skeniranja prikaže
      predmet i ko ga je zadužio. / The camera reads the QR label.
- [ ] Isto, ali **ukucaj kod ručno** umesto skeniranja. *Očekivano:* isti
      rezultat — nalepnica koja se ne da pročitati ne sme da blokira posao.
- [ ] **Zaduži na mene**, pa **Razduži**. *Očekivano:* stanje se menja odmah i
      vidi se u admin panelu.
- [ ] Skeniraj nešto što **već drži neko drugi**. *Očekivano:* piše čije je, ne
      dozvoljava tiho preuzimanje.
- [ ] **Odbij dozvolu za kameru**, pa probaj ponovo. *Očekivano:* razumljiva
      poruka i ručni unos i dalje radi, aplikacija ne puca.
- [ ] **Obaveštenja** — lista se otvara; označi jedno kao pročitano; brojač na
      donjoj navigaciji se smanji.
- [ ] **Promena lozinke** (Nalog → Promeni lozinku). *Očekivano:* posle
      promene te izbaci na prijavu — sve sesije su poništene.

## 4. Prolaz 3 — poslovođa / A foreman

Nalog: **Foreman**.

- [ ] Donja navigacija sada ima i **Zaposleni** i **Projekti**.
- [ ] Zaposleni → pretraga po imenu; otvori nekoga; vide se prilozi (dokumenti).
- [ ] **Dodaj prilog** (dokument ili sliku) zaposlenom, pa ga otvori.
      *Očekivano:* otprema uspeva. Ovo je ispravka `C11` i **mora se probati na
      sveže podignutom okruženju** (`docker compose down -v` pa `up`) — greška
      je bila u vlasništvu Docker volumena pri prvom montiranju, pa je na
      ručno popravljenom okruženju nevidljiva. / Must be tried on a freshly
      created environment; the bug was in volume ownership at first mount.
- [ ] Projekti → otvori projekat; vidi se ekipa i rok.
- [ ] **Skini nekoga sa projekta** (iz admin panela), pa osveži i projekat i
      karton tog zaposlenog. *Očekivano:* nestaje sa spiska **odmah**, i
      brojač ekipe se smanji. *(Ispravka `C12` — raspored se zatvara datumom
      umesto da se briše, a spiskovi taj datum nisu gledali.)*
- [ ] **Vozila / Alat / Materijal** — liste se otvaraju, pretraga radi.
- [ ] Materijal → **izmeni stanje** (dodaj ili skini količinu). *Očekivano:*
      novo stanje je odmah tačno.
- [ ] Isto to **dva puta brzo zaredom, istim potezom**. *Očekivano:* količina
      se promeni **jednom**, ne dvaput — to je zaštita idempotentnim ključem.
- [ ] **Troškovi vozila** → unesi gorivo. *Očekivano:* upisano, vidi se u
      admin panelu pod troškovima.
- [ ] Otvori ekran koji Foreman **ne sme** da vidi tako što ćeš ga zvati iz
      obaveštenja/deep linka, ako je moguće. *Očekivano:* vraća te na početnu,
      ne prazan ekran sa greškom.

## 5. Prolaz 4 — bez signala / Offline

Ovo je gradilište. Uključi **avionski režim** za svaku stavku.

- [ ] Otvori ekran koji si već gledao dok je bilo signala (npr. Zaposleni).
      *Očekivano:* podaci se prikazuju, a **na vrhu piše kada su sačuvani** —
      „Nema veze — prikazani su podaci sačuvani u HH:MM".
- [ ] Otvori ekran koji **nikad nisi otvorio** dok je bilo signala.
      *Očekivano:* prijateljska poruka „Nema veze sa serverom", sa dugmetom
      „Pokušaj ponovo" — ne crveni ekran i ne beskrajni točkić.
- [ ] Pritisni „Pokušaj ponovo" dok je i dalje avionski režim. *Očekivano:*
      ista poruka, bez rušenja.
- [ ] Vrati mrežu, pritisni „Pokušaj ponovo". *Očekivano:* sveži podaci, traka
      o starim podacima nestaje.
- [ ] **Prijavi dolazak bez signala.** *Očekivano:* **uspeva** — kartica
      pokazuje da je smena u toku i piše „Zabeleženo na ovom telefonu. Biće
      poslato kad bude signala." *(Ovo je `M9`, novo.)*
- [ ] Zabeleži tačno vreme kad si pritisnuo. Ubij aplikaciju, vrati signal,
      otvori je ponovo. *Očekivano:* smena je otišla na server **sa tim
      vremenom**, ne sa vremenom kad si otvorio aplikaciju. Proveri u admin
      panelu.
- [ ] Isto i za **odjavu smene bez signala**, uključujući minute pauze.
- [ ] Cela smena bez signala: prijava, pa odjava, pa tek onda vrati mrežu.
      *Očekivano:* obe idu, u redosledu, i trajanje je tačno.
- [ ] Odjavi se bez signala. *Očekivano:* trenutno, kao u prolazu 1.

## 6. Prolaz 5 — GPS u pozadini / Background GPS

Traži da neko stvarno hoda ili vozi. Najbolje pola sata.

- [ ] Prvo pokretanje traži dozvolu za lokaciju. Odobri **„Uvek" / „Always"**.
- [ ] Početni ekran pokazuje da se pozicija deli.
- [ ] Zaključaj ekran i stavi telefon u džep na 10 minuta, hodajući.
      *Očekivano:* u status baru stoji trajno obaveštenje o praćenju, a na
      mapi u admin panelu se pozicija pomera.
- [ ] Prebaci se u drugu aplikaciju na 10 minuta. *Očekivano:* isto.
- [ ] **Uđi u zonu bez signala** (podrum, garaža) pa izađi. *Očekivano:*
      pozicije iz rupe stižu naknadno, u redosledu, ne nestaju.
- [ ] **Ukloni aplikaciju iz „recents".** *Očekivano:* praćenje **prestaje** —
      to je poznato ograničenje (`C3`), ne greška. Otvori aplikaciju ponovo:
      sačuvane pozicije treba da odu na server.
- [ ] **Odjavi se dok praćenje radi.** *Očekivano:* obaveštenje u status baru
      **nestaje odmah**. Ako ostane, to je greška i vredna je prijave.
- [ ] Isključi lokaciju u podešavanjima telefona dok aplikacija radi.
      *Očekivano:* ekran to kaže, aplikacija ne puca.

## 7. Prolaz 6 — sesija i deljeni telefon / Session and a shared phone

Telefon u baraci prelazi iz ruke u ruku. Ovo je putanja koja curi podatke ako
nešto nije u redu.

- [ ] Prijavi se, pa ostavi telefon **20 minuta** neotvoren (access token traje
      15). Otvori bilo koji ekran. *Očekivano:* radi bez ponovne prijave —
      token se sam osvežio.
- [ ] Prijavi se kao **Worker**, pogledaj neke ekrane, odjavi se, pa se prijavi
      kao **Foreman**. Uključi avionski režim i otvori iste ekrane.
      *Očekivano:* **ne vidiš podatke prethodnog korisnika** — keš se prazni na
      svaku promenu osobe.
- [ ] Prijavi se na telefonu, pa iz admin panela **deaktiviraj tog korisnika**.
      Na telefonu povuci listu da se osveži. *Očekivano:* najkasnije kad token
      istekne, aplikacija te vraća na prijavu — radnik koji je napustio firmu
      ne sme da nastavi da radi sa telefonom u džepu. / Deactivate the signed-in
      user from the admin panel; the phone must fall back to sign-in.
- [ ] Deinstaliraj i instaliraj ponovo. *Očekivano:* ekran za prijavu i traži
      adresu servera iznova; aplikacija **ne ostaje na splash ekranu**. Ovo
      proverava ispravku za keystore iz `C9`.

## 8. Prolaz 7 — admin panel / The admin panel

Nalog: **Admin**, u pregledaču.

- [ ] Prijava; osvežavanje stranice te ne izbacuje.
- [ ] **Mapa** — pozicije sa telefona iz prolaza 5 se vide, sa imenima.
- [ ] Zaposleni / Projekti / Vozila / Alat / Materijal — **napravi, izmeni,
      obriši** po jedan zapis u svakom. Naročito **obriši**: taj je već jednom
      umesto brisanja samo otvarao stranicu.
- [ ] Radno vreme → pregled smena iz prolaza 2; **izmeni** jednu; **zbirni
      pregled** se slaže.
- [ ] Odsustva → **odobri** zahtev iz prolaza 2. *Očekivano:* radnik to vidi na
      telefonu.
- [ ] Zadaci → vidi se defekat sa fotografijom iz prolaza 2; slika se otvara.
- [ ] Troškovi → gorivo iz prolaza 3 je tu; zbir po projektu i po vozilu ima
      smisla.
- [ ] **Izvoz u Excel** na bar dve liste. *Očekivano:* fajl se otvara, ćirilica
      i dijakritika su ispravni.
- [ ] Dokumenta koja ističu → lista radi.
- [ ] **Pošalji obaveštenje** iz panela. *Očekivano:* stiže u listu obaveštenja
      u aplikaciji. **Push u status bar neće stići** dok Firebase nije
      podešen (`C5`) — to je poznato.
- [ ] Promeni jezik panela na engleski i nazad.
- [ ] Odjava; nazad na prijavu; dugme „nazad" u pregledaču te ne vraća unutra.

---

## 9. Poznato da ne radi / Known not to work

Ne troši vreme na prijavljivanje ovoga. / Do not spend time reporting these.

| Stavka / Item | Zašto / Why |
|---|---|
| Push u status bar / Push to the status bar | Firebase projekat ne postoji (`C5`). Lista obaveštenja u aplikaciji radi. |
| Ostala pisanja bez signala — defekat, odsustvo, stanje materijala / Other offline writes | Nisu u redu čekanja. Samo prijava i odjava smene jesu (`M9`), jer su jedine kod kojih je **vreme** ono što se ne može rekonstruisati posle. |
| GPS posle uklanjanja iz „recents" ili restarta telefona | Servis je vezan za aktivnost (`C3`). Sačuvane pozicije odlaze pri sledećem pokretanju. |
| iOS | Nikad građen ni pokrenut. |
| Release (potpisan) Android build | Konfiguracija napisana, nikad izvršena sa pravim keystore-om (`C4`). |

---

## 10. Rezultat / Outcome

Kad prođeš prolaz, vrati nazad samo dve stvari: / When a pass is done, bring
back two things:

1. Koje su stavke pale, sa pet podataka iz odeljka 1. / Which items failed,
   with the five details from section 1.
2. Šta je delovalo **čudno a nije palo** — sporo, zbunjujuće, pogrešna reč.
   Te nalaze nijedan test neće naći. / What felt **wrong without failing** —
   slow, confusing, a badly worded label. No test will ever find those.
