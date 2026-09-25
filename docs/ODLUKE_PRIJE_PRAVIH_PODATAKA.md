# Prije nego počnu pravi podaci — dvije stvari koje kod ne može da uradi

Za vlasnika firme. Sastavljeno 2026-09-25. Uz ovaj dokument idu [PRIVACY.md](PRIVACY.md) (šta se čuva i koliko) i [PROVISIONING.md](PROVISIONING.md) (tehnički koraci).

**Važno:** dijelovi pod A su **nacrt za pravnika**, ne pravni savjet. Firma posluje u više zemalja (u obračunu se pominju SLO, DE, HR), pa se najvjerovatnije primjenjuje GDPR; pravnik treba da potvrdi osnov i tekst obavještenja za svaku zemlju u kojoj radnici rade.

---

## A. Praćenje lokacije radnika

### A1. Šta sistem tačno radi danas (provjereno u kodu)

- Telefon šalje položaj **svaki minut**, i kad je aplikacija zatvorena.
- **Praćenje traje dok je radnik prijavljen u aplikaciju**, ne samo dok je prijavljen na smjenu. Aplikacija ne poznaje radno vrijeme. Radnik koji ostane prijavljen poslije posla, preko noći ili vikendom i dalje šalje lokaciju.
- Ista lokacija se koristi za mapu uživo i kao dokaz prisustva. Čuva se **180 dana**, pa se briše sama (može se skratiti).
- Uz to se bilježe koordinate prijave i odjave smjene, uz svaku smjenu.
- Lokaciju vide: Super Admin, Admin i rukovodioci; predradnik samo ekipe s gradilišta na kojima je i sam.

**Odluka koja je najveća:** hoće li se praćenje ograničiti na vrijeme dok je radnik prijavljen na smjenu (preporučujem). To **jeste izmjena u aplikaciji**, ne podešavanje. Kad je odlučite, mogu je uraditi: praćenje se pali pri prijavi na smjenu i gasi pri odjavi. Posljedica: mapa uživo pokazuje samo one koji su trenutno na smjeni, što je i ono što je potrebno.

### A2. Pravni osnov — preporuka za pravnika

- **Saglasnost radnika** nije dobar osnov u radnom odnosu: odnos nije ravnopravan, pa se saglasnost teško smatra slobodno datom.
- **Legitimni interes poslodavca** je uobičajen put, ali traži **zapisan test odmjeravanja**: zašto je praćenje potrebno (dokaz prisustva na gradilištu, sigurnost radnika, obračun), da li postoji blaža mogućnost, i zašto ne narušava prava radnika previše. Praćenje samo za vrijeme smjene mnogo olakšava taj test; praćenje van smjene ga gotovo onemogućava.
- Pitanje za pravnika: da li lokalno radno pravo (svaka zemlja u kojoj radnici rade) traži i nešto dodatno, npr. dogovor s radničkim predstavnicima.

### A3. Obavještenje radnicima — nacrt teksta

> **Obavještenje o evidenciji lokacije**
>
> Firma [NAZIV FIRME, adresa] koristi aplikaciju za evidenciju radnog vremena i prisustva na gradilištu.
>
> **Šta se bilježi:** položaj vašeg telefona (geografska širina i dužina, tačnost) otprilike jednom u minuti, dok ste prijavljeni na radnu smjenu, te položaj u trenutku prijave i odjave smjene.
>
> **Zašto:** dokaz da ste bili na gradilištu, obračun radnih sati i zarade, sigurnost na terenu.
>
> **Osnov:** [legitimni interes poslodavca — tačan tekst po savjetu pravnika].
>
> **Ko vidi:** direktor i administratori firme; vaš predradnik vidi samo ekipu s gradilišta na kojima i sam radi.
>
> **Koliko se čuva:** podaci o kretanju [180 / kraći broj] dana, zatim se automatski brišu. Koordinate smjene čuvaju se uz obračun sati [rok].
>
> **Van radnog vremena** položaj se ne prati. [Ovo važi tek nakon izmjene aplikacije — vidi A1.]
>
> **Vaša prava:** možete tražiti uvid u svoje podatke, ispravku i brisanje, te podnijeti prigovor. Obratite se: [ime i kontakt osobe / e-pošta].
>
> Podaci se obrađuju uz pomoć pružaoca usluge [ime hosting kompanije] i, za obavještenja na telefon, Google (Firebase).

Rečenica o "van radnog vremena" ne smije stajati u obavještenju dok aplikacija to zaista ne radi.

### A4. Procjena uticaja (DPIA) — šta je već popunjeno

Sistematsko praćenje zaposlenih po pravilu traži procjenu uticaja. Tehnički ulazi su gotovi i nalaze se u [PRIVACY.md](PRIVACY.md) §1–§5 (tabela podataka, rokovi, ko šta vidi, brisanje). Preostaje samo ono što piše vlasnik/pravnik:

1. **Svrha i nužnost** — jedna do dvije rečenice zašto praćenje treba (iz testa odmjeravanja, A2).
2. **Rizici za radnika** — npr. praćenje van posla, pogrešno tumačenje kretanja, curenje podataka.
3. **Mjere** — praćenje samo tokom smjene, rok čuvanja, pristup po ulozi, brisanje na zahtjev (sve već postoji ili je gore predloženo).
4. **Zaključak i potpis** — ko je pregledao i kada.

### A5. Ugovori s obrađivačima

Potreban je ugovor o obradi podataka s: **Google (Firebase)** za push i s **pružaocem servera** i, ako se uključi vanjski backup, s pružaocem S3 skladišta. Većina ih nudi standardni ugovor koji se prihvati u nalogu.

### A6. Šta treba da odlučite (odgovorite mi ili pravniku)

1. Praćenje samo za vrijeme smjene: **da / ne**? (Preporuka: da.)
2. Rok čuvanja lokacije: ostaje **180 dana** ili kraće (npr. 30–90)?
3. Ko je kontakt osoba za zahtjeve radnika (ime i e-pošta)?
4. Ko je pravnik/savjetnik koji potvrđuje osnov i tekst?

---

## B. Push obavještenja i e-pošta na serveru

Na testnom serveru trenutno **nije podešeno ni jedno ni drugo**. Super Admin to vidi na početnoj stranici. Posljedica: telefon nikad ne dobija obavještenja, a poruke o promjeni lozinke i izvještaji mailom se ne šalju.

### B1. Push (Firebase) — šta vi radite

1. Prijavite se na https://console.firebase.google.com **Google nalogom firme** i napravite projekat (Analytics nije potreban).
2. **Add app → Android**, ime paketa tačno: `com.planconcept.construction_mobile`. Preuzmite `google-services.json`.
3. Ako radnici imaju iPhone: **Add app → iOS**, preuzmite `GoogleService-Info.plist`, te iz Apple Developer naloga napravite APNs ključ (`.p8`) i dodajte ga u Firebase → Cloud Messaging.
4. **Project settings → Service accounts → Generate new private key**: preuzmite JSON.
5. Pošaljite mi (ili čuvajte na sigurnom) dva fajla: `google-services.json` i servisni ključ. **Servisni ključ može slati poruke svim uređajima; ne šaljite ga javno i ne stavljajte u repozitorijum.**

### B2. Push — šta radim ja kad dobijem fajlove

- Upisujem ključ na server (`FIREBASE_CREDENTIALS_JSON` u `.env`, u jednom redu) i restartujem API.
- Ubacujem `google-services.json` u Android projekat i pravim **novi APK**. Bez novog APK-a telefon i dalje ne prima poruke, jer se konfiguracija ugrađuje u aplikaciju; radnici moraju instalirati novi.
- Provjera: prijava na telefonu, slanje obavještenja iz panela.

### B3. E-pošta — šta vi radite

Treba SMTP nalog s kojeg sistem šalje poštu. Najlakše je ono što firma već ima (poslovna e-pošta, Google Workspace, Microsoft 365, ili servis kao Brevo/Mailjet). Trebaju mi:

- adresa SMTP servera (npr. `smtp.gmail.com`) i port (najčešće `587`)
- korisničko ime i lozinka (za Gmail/Google Workspace: **lozinka aplikacije**, ne obična lozinka)
- adresa pošiljaoca (npr. `obavjestenja@vasafirma.com`)

Ja ih upisujem u `.env` (`SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_FROM`) i restartujem. Provjera: zahtjev za promjenu lozinke stiže na e-poštu.

### B4. Šta je bezbjedno uraditi odmah

Ništa od toga ne traži prekid rada ni gubitak podataka. Kad imate fajlove i SMTP podatke, kažite mi "imam" i postavljanje traje oko pola sata, uz novi APK.
