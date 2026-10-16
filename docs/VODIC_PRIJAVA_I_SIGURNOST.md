# Vodič: prijava, lozinka i sigurnost naloga

Kako se prijaviti, promijeniti lozinku, zaštititi nalog drugim faktorom i šta platforma bilježi radi sigurnosti. Za tehničke detalje (ključevi, konfiguracija) vidi [SECURITY.md](SECURITY.md).

---

## 1. Prijava

1. Otvorite stranicu za prijavu, upišite **e-poštu** i **lozinku**.
2. Ako je za vašu ulogu uključen drugi faktor, upišite i **šestocifreni kod** (odjeljak 3).
3. **Zaboravili ste lozinku?** Izaberite tu vezu na stranici za prijavu, upišite e-poštu i stiže link za izbor nove lozinke. Link se može iskoristiti samo jednom.
4. **Novi nalog:** administrator pozove osobu, a ona preko linka iz poziva sama bira lozinku.

Nalog se **privremeno zaključava**: nakon 10 pogrešnih pokušaja zaredom 15 minuta. Poruka je ista kao za pogrešnu lozinku. Uspješan reset lozinke zaključavanje poništava. Sačekajte, ili zamolite administratora.

Jezik panela mijenjate ikonicom globusa u zaglavlju; promjena je odmah.

---

## 2. Lozinka

Promjena: izbornik naloga (ikonica s vašim slovom, gore desno) > **Promijeni lozinku**.

Pravila:

- najmanje **8 znakova** (najviše 128),
- bar jedno **veliko** slovo, jedno **malo** slovo i jedna **cifra**,
- nova mora biti **različita od trenutne**.

Savjeti: ne koristite istu lozinku kao drugdje; menadžer lozinki je najlakši način.

---

## 3. Drugi faktor (šestocifreni kod)

Uz lozinku traži se i **kod iz aplikacije za provjeru** (Google Authenticator, Microsoft Authenticator, Aegis i slične). Kod se mijenja svakih 30 sekundi, pa tuđa lozinka sama nije dovoljna.

> **Stanje na testnom serveru:** funkcija je izgrađena i postavljena, ali je **isključena** dok Super Admin ne odluči (redoslijed uključivanja je opisan u SECURITY.md). Dok je isključena, prijava je samo lozinka i meni naloga nema opciju za drugi faktor.

### 3.1 Prvo uključivanje

1. U izborniku naloga izaberite **Provjera u dva koraka** (drugi faktor).
2. Skenirajte **QR kod** aplikacijom za provjeru.
3. Upišite šestocifreni kod koji aplikacija pokaže.
4. Dobijate **kodove za oporavak**. **Sačuvajte ih na sigurno mjesto** (odštampajte ili u menadžer lozinki). Svaki se može iskoristiti jednom.

### 3.2 Svakodnevna prijava

Lozinka, pa kod iz aplikacije. Nakon lozinke imate 5 minuta i 5 pokušaja da upišete kod; isti kod se ne može iskoristiti dvaput. Aplikacija na telefonu traži kod pri prijavi, ali se **uključivanje obavlja u panelu**.

### 3.3 Izgubljen telefon

- **Imate kodove za oporavak:** prijavite se jednim od njih, pa isključite stari i uključite drugi faktor na novom telefonu.
- **Nemate kodove:** zamolite Super Admina da **resetuje** vaš drugi faktor (to ujedno završava sve vaše prijave), pa ga ponovo uključite.

### 3.4 Ko mora imati drugi faktor

Super Admin određuje uloge kojima se očekuje (preporučeno: Super Admin i Admin). Takvi nalozi **ne ostaju zaključani**: prijava uspijeva, a u panelu stoji traka dok drugi faktor nije podešen. Uloga kojoj je faktor očekivan ne može ga sama isključiti; to radi Super Admin (reset).

Deset kodova za oporavak prikazuje se **samo jednom**, pri uključivanju.

### Primjer: Admin dobija očekivan drugi faktor

1. Super Admin postavi ulogu Admin kao ulogu kojoj je drugi faktor očekivan.
2. Kad se Admin sljedeći put prijavi, prijava uspije, a u panelu stoji traka s pozivom da podesi drugi faktor.
3. Admin otvori **Provjera u dva koraka** u izborniku naloga, skenira QR kod, upiše kod i sačuva kodove za oporavak.
4. Od tada se prijavljuje lozinkom i kodom.

---

## 4. Šta platforma bilježi (trag izmjena)

Stranica **Trag izmjena** (izbornik Administracija) pokazuje ko je šta uradio: kreiranje, izmjenu, brisanje i **otvaranje osjetljivih dokumenata**.

- Svaka promjena lozinke i uloge ostavlja trag.
- Kad neko osim same osobe otvori ili izveze **ljekarski pregled, lični dokument ili vizu**, bilježi se ko, kada i odakle (radnja **Otvoreno**, vrsta zapisa **Prilog**).
- Brisanje zahtjeva za dokument i drugi osjetljivi koraci takođe.

---

## 5. Zaštita dokumenata

- Svaka otpremljena datoteka se **provjerava na viruse** prije spremanja; sadržaj mora odgovarati nazivu.
- Radnik vidi samo dokumente koje uprava **podijeli** s njim.
- Aplikacija na telefonu **ne dozvoljava snimak ekrana** osjetljivog dokumenta i briše privremene kopije.

Vidi [VODIC_DOKUMENTI.md](VODIC_DOKUMENTI.md).

---

## 6. Česta pitanja

**Kod iz aplikacije se ne prihvata.** Provjerite da su sat i datum na telefonu automatski; razlika od minute dovodi do pogrešnog koda.

**Pogriješio sam lozinku nekoliko puta i nalog je zaključan.** Sačekajte, ili zamolite administratora da vas otključa.

**Mogu li promijeniti lozinku nekome drugom?** Samo uprava, u izmjeni naloga; svaka promjena se bilježi.
