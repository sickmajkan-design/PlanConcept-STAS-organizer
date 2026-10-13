# Plan: povuci-i-otpusti (drag & drop) za sve dokumente

Napisano: 2026-10-13. Uloge: Product Owner (zahtjevi, priče), Software arhitekta (tehničko rješenje), UI/UX dizajner (ponašanje i tekstovi), Frontend dizajner (izgled u temi panela).
Referenca: slika dijaloga "Kreiraj – Dokumenti" (zona "Prevucite i otpustite datoteke ovdje ili kliknite za pretraživanje", ograničenje veličine, popis formata, sklopivi odjeljak "Datumi i podsjetnici – neobavezno").

## 1. Šta danas imamo (provjereno u kodu)

| Mjesto | Šta radi | Povuci-i-otpusti |
| --- | --- | --- |
| `AttachmentList` + `UploadAttachmentDialog` | Dokumenti na ~19 stranica (radnik, projekat, vozilo, alat, trošak, najam, smještaj, povrat...) | Ne, samo dugme "Izaberi datoteke" |
| `UploadDocumentDialog` | Hub "Dokumenti koji ističu": prvo se bira vlasnik pa datoteke | Ne |
| `InvoiceFilePicker` | Račun uz materijal i kretanje zaliha (1 datoteka) | Ne |
| `NewRefundDialog` | Povrat troškova (1 datoteka) | Ne |
| Uvozi (Excel/CSV): Knjiga uvoz, gorivo, isporuke materijala, smještaj, radnici, potpisane sedmične liste, podešavanja firme, QR fotografija | Svako ima svoj `<input type=file>` | Samo Evidencija/uvoz (`BookImportPage`) |

Ostalo što je bitno:
- Ograničenja su i na serveru (`AttachmentRules`): 20 MB po datoteci, dozvoljeno 11 nastavaka (pdf, jpg, jpeg, png, webp, heic, doc, docx, xls, xlsx, txt). Tip se određuje po nastavku, ne po sadržaju.
- API prima jednu datoteku po zahtjevu (multipart); dijalog ih šalje jednu po jednu, bez napretka po datoteci i bez ponovnog pokušaja.
- Ograničenja se ponavljaju (`MAX_ATTACHMENT_BYTES`, `ACCEPTED_EXTENSIONS` u `api/attachments.ts`, još jedan `MAX_BYTES` u uvozu) i poruke o greškama su razbacane po dijalozima.
- Slika ima polja Naslov, Tip i Oznake. Mi imamo Kategoriju i Opis; naslova i oznaka nema (vidi pitanja, tačka 7).

## 2. Cilj i uspjeh

**Cilj:** na svakom mjestu gdje se dodaje dokument može se datoteka (ili više njih) prevući, zalijepiti ili izabrati, uz isti izgled, ista pravila i jasne poruke.
**Korisnici:** Super Admin, Admin, voditelj projekta, poslovođa (kancelarija i teren); na telefonu "Izaberi / slikaj".
**Mjerilo uspjeha:** sva mjesta za dokumente koriste isti sastavni dio; dodavanje 5 datoteka traje jedan postupak (prevuci, potvrdi); nijedna datoteka se ne gubi tiho kad jedna zakaže; e2e test pokriva povlačenje.

## 3. Tehničko rješenje (arhitekta)

**Jedan zajednički sastavni dio, ne devet kopija.**

1. `components/files/FileDropzone.tsx` (samo izgled i ponašanje, bez poziva API-ja)
   - Props: `accept`, `maxBytes`, `maxFiles`, `multiple`, `disabled`, `onFiles(accepted)`, `onReject(rejections)`, `compact` (mala varijanta za jednu datoteku u formi), `helper` (tekst ispod).
   - Obrađuje: dragenter/leave sa brojačem (da ne treperi nad djecom), `drop`, klik, tastaturu (Enter/Space otvara izbor), lijepljenje slike (`paste`) kad je u fokusu, odbijanje mapa i nepodržanih tipova, duplikate (isto ime i veličina).
   - Ne oslanja se na biblioteku (`react-dropzone` bi donio ~10 KB i svoje navike); logika je mala i već postoji u `BookImportPage`. Odluka za reviziju: ako tim želi biblioteku, zamjena je unutar jedne datoteke.
2. `features/files/validateFiles.ts` (čista funkcija, testirana): `(files, rules) → { accepted, rejected: [{file, reason: 'size'|'type'|'duplicate'|'count'}] }`. Pravila dolaze iz jednog izvora, `api/attachments.ts`.
3. `features/files/useUploadQueue.ts`
   - Red datoteka sa stanjima `queued | uploading | done | error | cancelled`, napredak po datoteci (`onUploadProgress` na `apiClient`), najviše 3 istovremeno, `AbortController` za otkazivanje, ponovni pokušaj po datoteci.
   - Djelimičan neuspjeh ne ruši ostale; dijalog ostaje otvoren dok nešto nije gotovo ili otkazano.
   - Nakon uspjeha invalidira iste React Query ključeve kao današnji `useUploadAttachment` (liste, brojači, "ističu").
4. `FileQueueList` (red s minijaturama, veličinom, statusom, "ukloni/ponovi").
5. Ugradnja u postojeće dijaloge, bez promjene API-ja:
   - `UploadAttachmentDialog` i `UploadDocumentDialog` dobijaju zonu na vrhu (kao na slici), red ispod, polja Kategorija/Opis/Datumi ispod.
   - `AttachmentList`: cijela kartica je cilj otpuštanja. Otpustiš datoteke na listu, otvara se dijalog već popunjen tim datotekama.
   - `InvoiceFilePicker` i `NewRefundDialog` koriste `compact` varijantu (jedna datoteka).
   - Stranice za uvoz koriste isti sastavni dio s `accept=".xlsx,.xls"` ili `.csv` i jednom datotekom; `BookImportPage` prelazi na njega.
6. Server (mala izmjena, može u istom izdanju):
   - Provjera sadržaja (magic bytes) uz nastavak, da `.pdf` koji nije PDF bude odbijen. Danas se vjeruje nastavku.
   - Novi `GET /api/v1/attachments/limits` (maksimalna veličina, dozvoljeni nastavci) kako web ne bi dupliciran držao pravila; do tada ostaje jedna konstanta na webu i test koji je poredi sa serverom.
   - Pitanje opterećenja: 3 istovremena otpremanja po korisniku su ispod `RequestSizeLimit` i ne traže izmjenu infrastrukture; veće veličine (tačka 7) traže provjeru ograničenja obrnutog proxyja (Caddy) i diska.
7. Bez promjene modela podataka u prvom izdanju. Naslov i oznake su zasebna odluka (tačka 7).

**Sigurnost i privatnost:** dozvoljena lista ostaje (ne crna lista); naziv datoteke se čisti kao i do sada; nema pretpregleda `heic` u pregledniku (ikonica); minijature se prave lokalno (`URL.createObjectURL`) i oslobađaju (`revokeObjectURL`) pri zatvaranju; ništa se ne šalje prije potvrde. Antivirusno skeniranje nije u obimu (zasebna odluka prije živog servera).

## 4. Ponašanje i tekstovi (UI/UX)

**Stanja zone**

| Stanje | Izgled | Tekst (sr) |
| --- | --- | --- |
| Mirovanje | isprekidani okvir, ikona oblaka sa strelicom | "Prevucite i otpustite datoteke ovdje" / "ili kliknite za pretraživanje" |
| Povlačenje nad zonom (ispravno) | okvir u glavnoj boji, blaga pozadina, ikona se podigne | "Otpustite da dodate" |
| Povlačenje (neispravno: samo mape ili nepodržano) | okvir crven, bez pozadine | "Ovaj tip se ne može dodati" |
| Dodato | zona ostaje, ispod red datoteka | "Dodano 3 datoteke" (čitač ekrana: `aria-live`) |
| Slanje | traka napretka po datoteci, "Dodaj" onemogućeno | "Šaljem 2 od 5" |
| Greška | crvena linija u redu te datoteke i "Pokušaj ponovo" | "Nije poslano: veza je prekinuta" |
| Odbijena | ne ulazi u red, jedna zbirna poruka iznad | "2 datoteke preskočene: veće od 20 MB" |

**Pravila ponašanja**
- Zona je prva stvar u dijalogu (kao na slici); polja koja su za sve datoteke odjednom ispod. Odjeljak "Datumi i podsjetnici – neobavezno" je sklopljen dok se ne otvori (kao na slici), osim kad kategorija zahtijeva rok (npr. dozvola).
- Svaka datoteka u redu pokazuje: minijaturu ili ikonu tipa, ime, veličinu, status, "ukloni". Kategorija i opis važe za sve; promjena kategorije po datoteci je mogućnost za kasnije.
- Odbijanja se objašnjavaju razlogom, nikad tiho. Poruka ostaje dok korisnik ne doda nešto ili je ne zatvori.
- Povuci bilo gdje na detaljnoj stranici: kratki prekrivač "Otpustite da dodate u [Ime zapisa]" (samo kad je povučeno nešto što izgleda kao datoteka), da se ne smeta običnom povlačenju teksta ili redova.
- Telefon: umjesto zone velika dva dugmeta, "Izaberi datoteke" i "Slikaj" (`capture`); nema teksta o povlačenju.
- Dostupnost: zona je dugme (`role="button"`, fokus vidljiv, Enter/Space), povlačenje nije jedini put, boja nije jedini znak stanja, `prefers-reduced-motion` gasi podizanje ikone, kontrast okvira najmanje 3:1.
- Otkazivanje: Esc ili "Otkaži" prekida slanje, pita samo ako ima poslanih a neposlanih datoteka.

## 5. Izgled (frontend dizajner, tema panela)

- Koristi postojeće tokene panela (amber/narandžasta glavna boja, `divider`, `action.hover`), ne zelenu iz mockupa i ne tamnu iz slike.
- Zona: `border: 2px dashed` u `divider`; visina 160 px u dijalogu (96 px `compact`); radijus 12; ikona `CloudUploadOutlined` 40 px u glavnoj boji; unutrašnji razmak 24 px.
- Povlačenje: okvir `primary.main`, pozadina `alpha(primary, 0.08)`, prijelaz 120 ms.
- Neispravno: okvir `error.main`, pozadina `alpha(error, 0.06)`.
- Red datoteka: visina reda 48 px, minijatura 32 px (zaobljena 6), ime u jednom redu sa skraćivanjem na sredini (da se vidi nastavak), veličina u `text.secondary`, tanka traka napretka u dnu reda.
- Pomoćni tekst ispod zone: najveća veličina i dozvoljeni nastavci, sitnim slovima, kao na slici, ali skraćeno ("PDF, slike, Word, Excel, do 20 MB") sa punim popisom u opisu za čitač ekrana.
- Svijetla i tamna tema obje, i uski ekrani (zona preko cijele širine, dugmad jedno ispod drugog).

## 6. Priče i faze

### Faza 1 (Must): zajednički dio + dokumenti na zapisima (pokriva ~19 stranica)

**Priča 1.1: Povući datoteke u dijalog za dokument**
**As a** administrator ili voditelj projekta, **I want to** prevući jednu ili više datoteka u dijalog za dodavanje dokumenta, **So that** ne tražim dugme i dodajem dokumente brže.
- [ ] Given otvoren dijalog, when prevučem 3 ispravne datoteke, then se pojave u redu s imenom, veličinom i minijaturom i dugme "Dodaj" je aktivno.
- [ ] Given datoteka veća od limita ili nepodržanog tipa, when je otpustim, then se ne doda i vidim razlog po datoteci, a ispravne se ipak dodaju.
- [ ] Given klik na zonu ili Enter/Space u fokusu, then se otvara izbor datoteka.
- [ ] Edge: ista datoteka dvaput, mapa umjesto datoteke, više od dozvoljenog broja, 0 B datoteka: svaki ima jasnu poruku.
**Bodovi:** 8 · **Prioritet:** Critical

**Priča 1.2: Red slanja s napretkom i ponovnim pokušajem**
**As a** korisnik, **I want to** vidjeti napredak svake datoteke i ponoviti samo onu koja je zakazala, **So that** se ništa ne gubi kad veza zakaže.
- [ ] Given 5 datoteka, when jedna zakaže, then ostale završe, ta ima "Pokušaj ponovo" i dijalog ostaje otvoren.
- [ ] Given slanje u toku, when otkažem, then se aktivne prekidaju i nije napravljen zapis za prekinute.
- [ ] Edge: gubitak mreže usred slanja: greška na toj datoteci, ne na cijelom dijalogu.
**Bodovi:** 8 · **Prioritet:** Critical

**Priča 1.3: Otpustiti datoteke na listu dokumenata zapisa**
**As a** korisnik na detaljnoj stranici (radnik, vozilo, projekat...), **I want to** otpustiti datoteke direktno na karticu "Dokumenti", **So that** ne otvaram dijalog prije.
- [ ] Given kartica dokumenata, when povučem datoteku nad nju, then se označi kao cilj; when otpustim, then se otvara dijalog popunjen datotekama za taj zapis.
- [ ] Given korisnik bez prava dodavanja, then zona se ne nudi.
**Bodovi:** 5 · **Prioritet:** High

### Faza 2 (Should): ostala mjesta za dokumente
- **2.1** Hub "Dokumenti koji ističu": otpusti datoteke prvo, zatim izaberi vlasnika (prijedlog vlasnika po imenu datoteke je kasnija ideja). **5 bodova, High**
- **2.2** `InvoiceFilePicker` i povrat troškova na `compact` varijantu. **3 boda, Medium**
- **2.3** Potpisane sedmične liste i podešavanja firme (logo, dokumenti): isti sastavni dio. **3 boda, Medium**

### Faza 3 (Should): uvozi
- **3.1** Uvozi Excel/CSV (Evidencija, gorivo, isporuke, smještaj, radnici) prelaze na isti sastavni dio s ograničenjem tipa i jedne datoteke. **5 bodova, Medium**

### Faza 4 (Could): udobnost
- **4.1** Prekrivač "otpusti bilo gdje" na detaljnim stranicama. **3 boda**
- **4.2** Lijepljenje slike iz međuspremnika (snimak ekrana). **2 boda**
- **4.3** Kategorija po datoteci u redu. **3 boda**

### Faza 5 (Should, server): sigurnost i jedinstvena pravila
- **5.1** Provjera sadržaja datoteke (magic bytes) uz nastavak. **5 bodova, High**
- **5.2** `GET /api/v1/attachments/limits` da web ne duplira pravila. **2 boda, Medium**

### Faza 6 (Could, ako klijent traži): naslov i oznake kao na slici
- **6.1** Polja Naslov i Oznake na dokumentu (migracija, pretraga i filter po oznakama, prevodi). **8 bodova, Medium.** Traži odluku klijenta.

## 7. Testovi i definicija gotovog

- **Jedinični (vitest):** `validateFiles` (veličina, tip, duplikat, broj); `useUploadQueue` (istovremenost 3, ponovni pokušaj, otkazivanje); `FileDropzone` (dragenter/leave bez treperenja, drop, tastatura, odbijanje mape).
- **Ekran (vitest, jsdom):** `UploadAttachmentDialog` i `AttachmentList` sa simuliranim otpuštanjem.
- **Integracioni (backend):** zaglavlje i sadržaj ne odgovaraju (`.pdf` koji je izvršna datoteka je odbijen), limit na tačno 20 MB i 20 MB + 1 B.
- **Playwright (stvarni preglednik, stvarni API):** otpustiti 2 datoteke na karticu dokumenata radnika, vidjeti ih na listi; nepodržan tip daje poruku; telefonski prikaz ima dugmad umjesto zone.
- **Pristupačnost:** provjera tastaturom, čitačem ekrana (`aria-live`), kontrast, `prefers-reduced-motion`.
- **Gotovo kad:** svako mjesto za dokumente koristi `FileDropzone`; nema ponovljenih konstanti limita; testovi prolaze; obavijest o izdanju napisana (`RELEASE_ID` povećan); isprobano na testnom serveru.

## 8. Rizici

- **Veliki fajlovi i spor mobilni internet:** red sa ponovnim pokušajem i napretkom ublažava; veći limit traži provjeru proxyja i prostora.
- **Povlačenje preko cijele stranice smeta drugim povlačenjima (kanban, raspored):** prekrivač samo za stvarne datoteke (`dataTransfer.types` sadrži `Files`).
- **HEIC bez pretpregleda:** ikona i ime; ne pokušavati dekodirati u pregledniku.
- **Obim:** ~25 mjesta; zato faze, a ne jedno veliko izdanje.

## 9. Pitanja za odluku (jedno po jedno, po važnosti)

1. **Limit i formati:** ostajemo na 20 MB i trenutnih 11 nastavaka, ili kao na slici (25 MB i širi popis: avif, bmp, gif, tiff, rtf, odt, ods, csv...)? Preporuka: ostati dok klijent ne traži, dodati samo `csv`.
2. **Naslov i oznake** kao na slici: da ili ne (Faza 6)? Preporuka: ne u prvom izdanju.
3. **Redoslijed:** kreće se od Faze 1 i 5.1 (sigurnost) zajedno? Preporuka: da.
4. **Mobilni:** na telefonu samo "Izaberi" i "Slikaj", bez zone? Preporuka: da.
