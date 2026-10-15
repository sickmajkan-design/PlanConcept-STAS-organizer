# Plan: Moji dokumenti i zahtjevi za dokument (web + mobilna aplikacija)

Status: PLAN (ništa nije implementirano). Datum: 2026-10-15.
Prethodi: `improvement-backlog-2026-10-13` (stavka "My documents for every linked account").

## 1. Cilj u jednoj rečenici

Svaki radnik u aplikaciji vidi i otvara dokumente koje je uprava vezala uz njega, a uprava može
od radnika zatražiti dokument koji fali (npr. ljekarski pregled), te radnik taj dokument sam
pošalje iz aplikacije (kamera / galerija / datoteka) bez ikakvog drugog dodira sa sustavom.

## 2. Što već postoji (provjereno u kodu)

| Što | Gdje | Posljedica za plan |
|---|---|---|
| Dokument je `Attachment` s vlasnikom `Employee` | `AttachmentOwner.cs`, `Attachment.cs` | Nema nove tablice za sam dokument |
| Radnik smije čitati liste i sadržaj datoteka čiji je `EmployeeId` njegov | `GetAttachmentsQuery`, `GetAttachmentContentQuery`, `AttachmentRules.CanReadOwnEmployeeFiles` | Backend za "pregled" gotovo postoji |
| Radnik NE smije uploadati na `Employee` (samo foto na Project/WorkItem/VehicleExpense) | `AttachmentRules.CanUpload` | Treba uski, kontrolirani put (samo kroz zahtjev) |
| Sadržaj, 25 MB, provjera potpisa datoteke, naslov, tagovi, rok važenja, `RetainUntil` | već isporučeno 2026-10-13 | Ponovno koristimo, ne pišemo ispočetka |
| Obavijesti + push (`Notification`, `NotificationType`, device token) | `Features/Notifications` | Dodajemo nove tipove |
| Mobilni `attachments` modul (preview, form sheet) | `construction_mobile/lib/features/attachments` | Proširujemo, ne gradimo novo |
| Podsjetnici na istek dokumenata | `AttachmentExpiryReminder` | Radnik dobiva i svoj istek |

### Dvije rupe koje treba zatvoriti (sigurnosno)

1. **Nema oznake "vidljivo radniku".** Danas API radniku dopušta čitanje SVIH dokumenata na
   njegovom zapisu (ugovor, interne bilješke, sve). Dok UI to ne prikazuje nije problem; čim
   napravimo "Moji dokumenti", sve što je ikad ubačeno postaje vidljivo. Treba eksplicitna oznaka.
2. **Radnik nema legalan način slanja dokumenta**, a ne želimo mu otvoriti opći upload na
   zapis zaposlenika (mogao bi puniti tuđe/svoje kartone proizvoljnim datotekama).

## 3. Ključne odluke (preporuka u zagradi)

| # | Odluka | Preporuka |
|---|---|---|
| D1 | Što radnik vidi? | Samo dokumente označene "Podijeljeno s radnikom". Novi stupac `SharedWithEmployee`, **default false**, postojeći ostaju nevidljivi dok uprava ne podijeli (jedna akcija "Podijeli sve ugovore/…"). Privacy by default. |
| D2 | Tko može tražiti dokument? | Admin i Super Admin (Foreman danas ne smije ni čitati dokumente zaposlenika). Voditelj projekta/predradnik: kasnije, ako kupac traži. |
| D3 | Treba li pregled nakon slanja? | Da: statusi Otvoren → Poslan → Prihvaćen / Vraćen (s razlogom). Slika doktorskog nalaza koja je mutna mora imati put natrag. |
| D4 | Može li radnik poslati dokument bez zahtjeva? | Ne u fazi 1. Faza 3 opcionalno ("Pošalji upravi"), uz isti uski put. |
| D5 | Što vidi radnik od dokumenata vezanih uz plaću (`EmployeeRate`, `FinanceEntry`)? | Ništa u fazi 1 (drugi owner type, drugi sloj osjetljivosti). Odluka s kupcem. |
| D6 | Tekst push obavijesti | Generičan: "Uprava ti je poslala zahtjev" — nikad vrsta dokumenta/zdravlje na zaključanom ekranu. |
| D7 | Rok i podsjetnici | Opcionalan rok; podsjetnik radniku dan prije i na dan roka; uprava vidi "kasni". |
| D8 | Više radnika odjednom? | Da: jedan zahtjev → N pojedinačnih zahtjeva (svaki svoj status), npr. "Svi na projektu X: osiguranje". |

## 4. Podatkovni model

```
Attachment (postojeće) + SharedWithEmployee bool NOT NULL default false
                       + DocumentRequestId Guid? (koji zahtjev je ovaj dokument ispunio)

DocumentRequest (novo)
  Id, EmployeeId (FK, cascade), RequestedByUserId
  Category (AttachmentCategory)        -- što se traži
  Title (npr. "Ljekarski pregled")     -- vidi radnik
  Note? (max 500)                      -- vidi radnik; NE za zdravstvene detalje (UI upozorenje)
  DueDate?
  Status: Open | Submitted | Accepted | Returned | Cancelled
  SubmittedAttachmentId? , SubmittedAt?
  ReviewedByUserId?, ReviewedAt?, ReturnReason?
  CreatedAt/By (IAuditable), ISoftDeletable
```

Indeksi: `(EmployeeId, Status)`, `(Status, DueDate)`. Migracija: samo dodavanje (roll-forward, vidi
`local-dev-test-and-migration-setup`).

## 5. API (nove rute)

Radnik (identitet UVIJEK iz tokena, `EmployeeId` se nikad ne prima u zahtjevu):

| Ruta | Opis |
|---|---|
| `GET /me/documents` | Moji podijeljeni dokumenti, grupirani po kategoriji, s rokom važenja |
| `GET /me/document-requests?status=` | Moji zahtjevi |
| `POST /me/document-requests/{id}/submit` (multipart) | Ispuni zahtjev; samo svoj, samo Open/Returned |
| `GET /attachments/{id}/content` | Postojeće, uz novu provjeru `SharedWithEmployee` |

Uprava:

| Ruta | Opis |
|---|---|
| `POST /document-requests` | Kreiraj za 1..N zaposlenika |
| `GET /document-requests?status=&employeeId=&overdue=` | Pregled, paginirano |
| `POST /document-requests/{id}/accept` · `/return` (razlog) · `/cancel` | Pregled i zatvaranje |
| `PATCH /attachments/{id}/sharing` | Podijeli/ukloni dijeljenje s radnikom (+ bulk) |

## 6. Sigurnosni zahtjevi (obavezni, ne "lijepo imati")

**Autorizacija i IDOR**
- S1. Svaka radnikova ruta filtrira po `EmployeeId` iz tokena; id iz URL-a se samo uspoređuje, ne
  vjeruje se. Tuđi/nepostojeći id → **404**, ne 403 (već praksa u `GetAttachmentContentQuery`).
- S2. `CanReadOwnEmployeeFiles` dobiva uvjet `SharedWithEmployee == true` — vrijedi i za listu i
  za sadržaj i za izvoz (`ExportAttachmentsQuery`). To je i sužavanje postojeće rupe.
- S3. Radnik ne dobiva opći `CanUpload` na `Employee`. Upload ide samo kroz `submit` koji: provjerava
  vlasništvo zahtjeva, status, i **kategoriju fiksira na onu iz zahtjeva** (klijent je ne bira).
- S4. Nakon `Accepted` radnik ne može mijenjati ni brisati dokument. Brisanje radnik uopće nema.
- S5. Svaki test: matrica uloga × (svoj/tuđi) × (podijeljeno/nepodijeljeno) kao integracijski test
  (kao `RoleAdministrationTests`).

**Datoteke**
- S6. Isti lanac kao danas: 25 MB, dopušteni tipovi, provjera potpisa (`MatchesSignature`),
  generirani `StorageKey` (nikad ime od klijenta). Za radnički upload dodati strožu listu: PDF/JPG/PNG/HEIC.
- S7. Provjeriti ima li antivirus/skeniranje na uploadu; ako ne, upisati kao poznati rizik i odlučiti
  (ClamAV sidecar) prije LIVE — upload od radnika je prvi put da *ne-uprava* šalje proizvoljan sadržaj u kartone.
- S8. Rate limit na `submit` (npr. 10/min po korisniku) i max broj otvorenih datoteka po zahtjevu (1 aktivna).
- S9. Odgovor sadržaja: `Cache-Control: no-store`, `Content-Disposition` i `X-Content-Type-Options: nosniff`
  (provjeriti da već postoje); preview blob rade pod postojećim CSP-om (`frame-src blob:`).

**Osjetljivi podaci (GDPR čl. 9 — zdravstveni podaci)**
- S10. Ljekarski nalaz = posebna kategorija. Dodati u `docs/PRIVACY.md` i DPIA listu (MUST-DO prije LIVE
  već postoji u `security-hardening`). Pravna osnova: zakonska obveza poslodavca (medicina rada) — potvrditi s kupcem.
- S11. Audit log: tko je i kada otvorio dokument kategorije `MedicalCheck` (postoji `Features/Audit`).
- S12. Push/e-mail tekst bez vrste dokumenta (D6). Polje `Note` ima upozorenje "ne upisuj zdravstvene detalje".
- S13. Mobilno: preuzeti bajtovi idu u privremenu mapu aplikacije, brišu se pri zatvaranju previewa i
  pri odjavi; na Androidu `FLAG_SECURE` na previewu medicinskih dokumenata (spriječiti screenshot) — odluka s kupcem.
- S14. Radnik napusti firmu / račun deaktiviran → pristup prestaje odmah (token); kaskadno brisanje
  dokumenata već postoji (FK cascade), `RetainUntil` poštujemo.
- S15. Mobilna pohrana zahtjeva (offline): samo metapodaci, nikad sadržaj dokumenata u trajnoj pohrani.

**Zlouporaba**
- S16. Uprava ne smije zahtjevima spamati: max N otvorenih po radniku (npr. 5), duplikat istog
  (radnik+kategorija+Open) se odbija.
- S17. Sve akcije uprave nad zahtjevom idu u audit (tko je zatražio/vratio/prihvatio).

## 7. Tokovi (flows)

### 7.1 Uprava traži dokument

```
Zaposlenik > kartica "Dokumenti"            ili   Dokumenti > "Zahtjevi"
 ┌──────────────────────────────────────────────┐
 │ Zatraži dokument                             │
 │ Radnik(ci):  [ Marko Horvat  × ] [+ dodaj]   │
 │ Vrsta:       (•) Ljekarski pregled           │
 │              ( ) Certifikat  ( ) Licenca ... │
 │ Naslov:      [Ljekarski pregled medicine rada]│
 │ Napomena:    [..............] ⚠ bez zdr. detalja│
 │ Rok:         [ 30.11.2026 ] (opcionalno)     │
 │                         [Odustani] [Pošalji] │
 └──────────────────────────────────────────────┘
 → status "Otvoren" → radnik dobiva generičnu push obavijest
```

### 7.2 Radnik (mobitel) — pregled i slanje

```
Početna → "Moji dokumenti" (značka: 2 zahtjeva)
 ┌─ Moji dokumenti ──────────────┐   ┌─ Ljekarski pregled ───────────┐
 │ ZAHTJEVI (2)                  │   │ Traži: Uprava (A. Admin)       │
 │ ┃ Ljekarski pregled  do 30.11.│ → │ Rok: 30.11.2026                │
 │ ┃ Osiguranje        kasni 3 d.│   │ Napomena: ...                  │
 │ DOKUMENTI                     │   │                                │
 │  Ugovor o radu         PDF  › │   │ [ 📷 Slikaj ]                  │
 │  Certifikat – visine   važi › │   │ [ 🖼 Iz galerije ]             │
 │  Licenca        ističe 12/26 ›│   │ [ 📄 Odaberi datoteku ]        │
 └───────────────────────────────┘   │ ── pregled, više stranica ──   │
        tap dokument → preview       │ [Pošalji]                      │
        (zaslon kao postojeći        └────────────────────────────────┘
         attachment_preview_screen)    nakon slanja: "Poslano, čeka provjeru"
```

Stanja kartice zahtjeva: Otvoren · Poslano (čeka provjeru) · Vraćeno ("Nečitljivo – pošalji ponovno",
razlog istaknut) · Prihvaćeno (prelazi u listu dokumenata).

### 7.3 Uprava — provjera

```
Dokumenti > Zahtjevi (filter: Poslano / Kasni / Otvoreno)
 Radnik | Traženo | Rok | Status | Poslano
 klik → preview dokumenta + [Prihvati] [Vrati s razlogom]
 Prihvati: dokument se veže na karton, SharedWithEmployee = true, opcionalno rok važenja
           (ExpiresAt) se unosi odmah → uključuje postojeće podsjetnike na istek
```

### 7.4 Rubni slučajevi (acceptance)

- Radnik bez povezanog računa/EmployeeId: "Moji dokumenti" prazno s objašnjenjem, ne greška.
- Slanje bez mreže: datoteka ostaje u lokalnom redu (postojeći `outbox`), vidljivo "Čeka slanje".
  Zahtjev je u međuvremenu otkazan: server vraća 409, korisnik dobiva jasnu poruku.
- Dvostruko slanje (dupli tap): idempotentno preko `ClientRequestId` (obrazac iz Evidencije).
- Prevelika/pogrešna datoteka: poruke kao u `rejectionText` (već postoje).
- HEIC s iPhonea: server ga prima; preview ide kroz postojeći backlog (pretvorba) — do tada "preuzmi".
- Uprava otkaže zahtjev dok je radnik već poslao: dokument ostaje u kartonu, nepodijeljen.
- Isti dokument vrijedi za više zahtjeva: ne, jedan zahtjev = jedna datoteka (više stranica = jedan PDF ili do 5 slika spojenih u PDF na klijentu — odluka u fazi 2).

### 7.5 Obavijesti i statusi "odmah nakon akcije" (dopuna 2026-10-15)

Načelo: **svaka promjena statusa zahtjeva = jedan događaj** koji u istoj transakciji (a) mijenja
status, (b) upisuje zapis u povijest zahtjeva, (c) kreira `Notification` za drugu stranu i šalje push.
Nema stanja bez obavijesti i nema obavijesti bez stanja.

| Akcija | Tko je radi | Obavijest prima | Što vidi druga strana odmah |
|---|---|---|---|
| Uprava kreira zahtjev | Uprava | **Radnik** (push + in-app): "Uprava ti je poslala zahtjev za dokument" | Kartica zahtjeva "Otvoren" s rokom i napomenom |
| Radnik pošalje dokument | Radnik | **Uprava** (zahtjevatelj + ostali admini s pravom): "Marko H. je poslao dokument" | Zahtjev "Poslano — čeka provjeru", dokument za preview |
| Uprava prihvati | Uprava | **Radnik**: "Dokument je prihvaćen" | "Prihvaćeno", dokument u listi "Moji dokumenti" |
| Uprava vrati s razlogom | Uprava | **Radnik**: "Dokument je vraćen — pošalji ponovno" | "Vraćeno" + **razlog istaknut** + gumb "Pošalji novi" |
| Radnik ponovno pošalje | Radnik | **Uprava**: "Novi dokument poslan" | "Poslano" (ponovno), prethodna verzija ostaje u povijesti |
| Uprava otkaže | Uprava | **Radnik**: "Zahtjev je otkazan" | "Otkazano" |
| Rok blizu / prošao | Sustav | Radnik (dan prije, na dan); uprava (kasni) | Oznaka "Kasni N dana" |

**Potvrda akciji u upravi (izravno nakon klika):** toast s pravim statusom
("Zahtjev vraćen radniku — razlog poslan") + red u tablici odmah dobiva novi status bez ručnog osvježavanja
(optimistično ažuriranje, pa usklađivanje s odgovorom servera; kod greške vraća staro i prikazuje poruku).
Uprava ne dobiva push za vlastitu akciju, nego **potvrdu u UI-u** — a druga uprava (kolega) dobiva obavijest
da je netko već obradio, da dvoje ne rade isti posao (status "Obrađuje X" nije potreban; dovoljno je da
drugi vidi novi status, a drugi klik na zastarjelo stanje daje 409 "Već obrađeno").

**Kako je to "odmah" bez WebSocket-a:** u kodu nema realtime kanala, postoji FCM push + spremljena obavijest.
- Mobitel: push (data poruka) dolazi odmah; na dolazak ili povratak u aplikaciju osvježi se lista zahtjeva i
  brojač na ikoni. Uz to kratki polling (npr. 30 s) samo dok je ekran "Moji dokumenti" otvoren.
- Web: zvonce već postoji; ekran zahtjeva osvježava se pollingom (30 s) i pri fokusu taba.
- Radnikov ekran detalja zahtjeva uvijek povlači svježi status pri otvaranju (ne vjeruje se starom cacheu).
- Ako realtime kasnije zatreba: SSE kanal za samo ovaj ekran, ne mijenja model.

**Povijest zahtjeva (vidljiva obje strane, bez internih bilješki uprave):** nova tablica
`DocumentRequestEvent` (RequestId, Type: Created/Submitted/Accepted/Returned/Resubmitted/Cancelled,
ActorUserId, At, Reason?, AttachmentId?). Prikaz kao vremenska crta:

```
Ljekarski pregled
 ● 15.10. 09:12  Zahtjev poslan  (Uprava)
 ● 15.10. 14:40  Dokument poslan  (ti)           [nalaz_1.jpg]
 ● 16.10. 08:05  Vraćeno: "Slika je mutna, slikaj ponovno s boljim svjetlom"  (Uprava)
 ○               Čeka tvoj novi dokument  →  [ Pošalji novi ]
```

**Odbijanje zbog nečitljivosti — pravila:**
- Razlog je **obavezan** (min. 5 znakova) i bira se iz brzih opcija uz slobodan tekst:
  "Nečitljivo / mutno", "Nepotpuno (fale stranice)", "Pogrešan dokument", "Istekao", "Drugo".
- Vraćanje ne briše poslanu datoteku: ostaje u povijesti i kartonu kao *nepodijeljena, neprihvaćena* verzija
  (revizijski trag), ali se ne prikazuje u "Moji dokumenti" niti kao važeća.
- Radnik može poslati novi dokument neograničeno puta dok je zahtjev Vraćen; uprava vidi broj pokušaja
  (npr. "3. pokušaj") — iznad praga (5) zahtjev se označi za ručni dogovor.
- Razlog vidi radnik (zato isti princip: bez zdravstvenih detalja, upozorenje u polju).
- Push za vraćanje je generičan (D6); razlog je tek unutar aplikacije.

**E-mail kao opcija (odluka 2026-10-15):** uz push i in-app, obavijest se može poslati i e-mailom.
- *Infrastruktura postoji:* `IEmailSender` (SMTP) i `Outbox` (pouzdana isporuka s ponavljanjem) — nema novog servisa.
- *Opt-in po korisniku:* postavka "Obavijesti o dokumentima e-mailom" (default **isključeno**); radnik je
  uključuje u profilu, uprava također. Uz to uprava pri kreiranju zahtjeva može označiti
  "Pošalji i e-mailom" za radnika koji ne koristi push — tada se radnikov pristanak bilježi (tko/kada).
- *Što se šalje:* samo generička poruka + poveznica: "Imaš novi zahtjev za dokument. Otvori aplikaciju."
  **Nikad** vrsta dokumenta, razlog vraćanja ni privitak (e-mail nije siguran kanal, S12).
  Poveznica vodi na prijavu pa na zahtjev (deep link, bez tokena u URL-u).
- *Adresa:* `Employee.Email`/`User.Email`; bez adrese opcija je neaktivna uz objašnjenje.
  Promjena adrese ne šalje se na staru bez potvrde (postojeće pravilo računa).
- *Okidači:* isti kao u tablici 7.5 (zahtjev, vraćeno, prihvaćeno, rok); podsjetnik roka max 1 e-mail/dan.
- *Zaštita:* rate limit po primatelju, odjava jednim klikom (poveznica za gašenje), zapis u outbox
  s ishodom (poslano/neuspjelo), SPF/DKIM na domeni pošiljatelja prije LIVE.

Novi `NotificationType`: `DocumentRequested`, `DocumentSubmitted`, `DocumentAccepted`, `DocumentReturned`,
`DocumentRequestCancelled`, `DocumentRequestDue` — s tekstovima u `PushTextResolver` (generički) i
`DataJson` s `requestId` za deep link ravno na zahtjev.

Sigurnosno: obavijest i deep link nose samo `requestId`; otvaranje opet prolazi provjeru vlasništva (S1).
Radnik ne može preko obavijesti saznati ništa o tuđem zahtjevu. Zahtjevatelj prima obavijest samo ako i
dalje ima pravo pregleda (uloga se ponovno provjerava pri slanju).

## 8. Faze i priče (MoSCoW, bodovi)

**Faza 0 — Odluke i mockup (obavezno prije koda, vidi `follow-the-approved-mockup`)**
- Odgovori kupca (poglavlje 10), mockup 4 zaslona (mobilni lista, mobilni slanje, web zahtjev, web provjera).

**Faza 1 — "Moji dokumenti" samo za čitanje (Must)** · ~13 b
1. Migracija `SharedWithEmployee` + sužavanje `CanReadOwnEmployeeFiles` + testovi matrice — 5 b, Critical
2. `GET /me/documents` + web ekran "Moji dokumenti" (radnik s računom) — 3 b
3. Mobilni ekran + preview (reuse `attachment_preview_screen`) — 5 b
4. Uprava: oznaka "Vidljivo radniku" na dokumentu + bulk "Podijeli" — 3 b

**Faza 2 — Zahtjevi (Must)** · ~21 b
5. Entitet + migracija + `POST /document-requests` + validacije/limiti (S16) — 5 b
6. Radnik: `GET /me/document-requests` + `submit` (S3, S6, S8) + testovi sigurnosti — 8 b
7. Uprava: pregled/prihvati/vrati/otkaži + audit — 5 b
8. Mobilno: lista zahtjeva, slanje kamera/galerija/datoteka, offline red — 8 b (preko faze)
9. Obavijesti u oba smjera (tablica 7.5), povijest zahtjeva (`DocumentRequestEvent`), polling/osvježavanje
   ekrana, deep link, podsjetnik roka — 5 b, High
11. E-mail obavijesti kao opt-in (postavka, generički tekst, outbox, odjava) — 3 b, Medium (može i u fazi 3)
10. Vraćanje s obaveznim razlogom + brze opcije + brojač pokušaja + ponovno slanje — 3 b, High

**Faza 3 — Dorada (Should/Could)**
- Slanje bez zahtjeva ("Pošalji upravi"), više slika → PDF, in-app pdf.js preview (već u backlogu),
  e-mail radniku bez aplikacije, predlošci zahtjeva ("Standardni paket novog radnika"), Foreman kao zahtjevatelj.

Svaka priča ide po utvrđenoj metodi: implement → test → `npx tsc -b` → push + deploy →
FirstUseHint + release note (RELEASE_ID, zadnjih 5).

## 9. Plan istraživanja (kratak, pokriva /design:user-research)

Cilj: provjeriti pretpostavke prije izrade mockupa; 5 radnika + 2 osobe iz uprave, 20–30 min, uživo/telefon.

Pretpostavke za provjeriti:
1. Radnici imaju pametni telefon i znaju fotografirati dokument (jezik, pismenost, starost).
2. Radnici *žele* biti u aplikaciji za dokumente (ili je WhatsApp tamo gdje se danas šalje).
3. Danas se dokumenti traže usmeno/WhatsApp-om i gube; uprava ne zna što fali.
4. Što uprava mora vidjeti da bi povjerovala da je dokument "u redu".

Pitanja radnicima: Kad ste zadnji put morali poslati dokument poslodavcu? Kako? Što je bilo teško?
Gdje držite svoje dokumente? Biste li htjeli vidjeti svoj ugovor/certifikate na mobitelu? Što vas brine
(tko vidi, zdravlje)? Pokažite kako biste poslali sliku nalaza (promatrati, ne objašnjavati).
Pitanja upravi: Kako danas pratite što fali? Tko i koliko često traži? Što se desi kad nalaz ne valja?
Koji su dokumenti obavezni po zakonu i tko je odgovoran?

Mjera uspjeha: ≥80% zahtjeva ispunjeno u roku bez ručnog podsjećanja; medijan vremena
zahtjev→prihvaćeno < 3 dana; 0 sigurnosnih incidenata (IDOR testovi zeleni).

## 10. Otvorena pitanja za kupca (Darijo)

1. Smije li radnik vidjeti sve svoje dokumente, ili samo ono što uprava podijeli? (D1)
2. Tko u upravi treba moći tražiti dokumente? Predradnici? (D2)
3. Treba li provjera (prihvati/vrati) ili je dovoljno "poslano"? (D3)
4. Plaćeni dokumenti (platne liste, ugovor o plaći) — da ili ne u aplikaciji radnika? (D5)
5. Zdravstveni nalazi: tko ih smije vidjeti uz upravu? Treba li zabrana screenshota? Koliko ih čuvati?
6. Hoće li svi radnici imati račun u aplikaciji, ili dio radi samo preko papira?
7. Na kojem jeziku radnici rade (HR/BS/RO/…)? Treba li lokalizacija zahtjeva?

## 11. Rizici

| Rizik | Mitigacija |
|---|---|
| Radnici bez računa — funkcija koristi samo dijelu | Pitanje 6; faza 3 e-mail/SMS veza |
| Zdravstveni podaci izlaze na zaključani ekran / u cache | S10–S13 |
| Upload od neprovjerenih korisnika (malware, trovanje) | S6–S8 |
| Uprava podijeli krivi dokument radniku | Potvrda pri dijeljenju, audit, opoziv dijeljenja |
| Opseg raste (plaće, potpisivanje, e-potpis) | Eksplicitno van opsega faze 1–2 |

## 12. Odluke vlasnika proizvoda (2026-10-15) — vrijede umjesto preporuka gore

| # | Odluka |
|---|---|
| O1 | Radnik vidi **samo dokumente koji vrijede za njega**, a uprava funkcijom "Podijeli s radnikom" bira koji (po dokumentu i skupno). Ništa se ne dijeli automatski. |
| O2 | Radniku su dostupni i dokumenti **zaduženog vozila** (`Vehicle.AssignedEmployeeId == radnik`): registracija, tehnički, osiguranje, vinjete/tuneli/prolazi (`VehicleToll`: država, dionica, vrijedi do, status), plus priloženi dokumenti vozila koje je uprava podijelila. Vrijedi dok je vozilo zaduženo; čim se zaduženje skine, pristup prestaje. |
| O3 | Radnici imaju **samo mobitel**; web ostaje za upravu. Nema web ekrana "Moji dokumenti". |
| O4 | Scope zahtjeva = scope pregleda zaposlenika (poslovne jedinice). |
| O5 | Obavijest o poslanom dokumentu: zahtjevatelju (zamjena ako je neaktivan). |
| O6 | Više slika po zahtjevu, server ih spaja u jedan PDF. |
| O7 | Uprava unosi rok važenja pri prihvaćanju. |
| O8 | Paket zahtjeva ("Novi radnik", ~5 dokumenata) — prijedlog ispod. |
| O9 | Antivirus (ClamAV) prije LIVE — preporuka prihvaćena. |
| O10 | Rokovi čuvanja i pravna osnova po zakonu države u kojoj firma posluje; konkretne rokove potvrđuje kupac. |
| O11 | Jezici: samo oni koje aplikacija već podržava. |
| O12 | Redoslijed: Faza 1, pa Faza 2. **Ništa se ne objavljuje na test server dok nije provjereno lokalno (API + emulator), svaka implementacija pregledana i greške ispravljene.** |

### Korisnik vozila — što radnik vidi (O2)

```
Moji dokumenti
 ▸ Zahtjevi (2)
 ▸ Moji dokumenti        Ugovor, certifikati, licence …
 ▸ Moje vozilo  ZG-1234-AB  Ford Transit
     Registracija   do 12.03.2027   ✔
     Tehnički       do 30.11.2026   ⚠ za 46 dana
     Osiguranje     do 01.01.2027   ✔
     Vinjete        SLO do 31.12.2026 ✔ · AT neplaćena ✖
     Tuneli         Karavanke do …
     Dokumenti vozila (podijeljeno)  Polica, prometna dozvola …
```

Sigurnosno: provjera je dvostruka — vozilo mora biti zaduženo baš ovom radniku **i** dokument mora
biti označen kao podijeljen. Radnik ne dobiva ni troškove vozila (gorivo, servis, najam) ni cijene.
Samo datumi valjanosti i status plaćanja. 404 za svako tuđe vozilo.

### Paket "Novi radnik" i vrste dokumenata (po odgovoru vlasnika, 2026-10-15)

Dokumenti koje uprava stvarno veže uz radnika: ugovor, vize, osobni dokumenti, pasoš, lična karta,
vozačka, diploma, certifikati i slično.

Postojeće kategorije (Contract, Certificate, MedicalCheck, Licence, Insurance, SiteDocument, Photo, Other)
ne razlikuju osobne isprave, vizu ni diplomu: sve bi završilo u "Other" i izgubilo rok i podsjetnike.
Prijedlog (nove vrijednosti enuma, 8–10, `Other` ostaje 99):

| Nova kategorija | Pokriva | Rok važenja | Podsjetnik na istek |
|---|---|---|---|
| `Identity` (8) | lična karta, pasoš, osobna iskaznica | da | da (npr. 90/30 dana) |
| `Visa` (9) | viza, boravišna i radna dozvola | da | da (npr. 90/30 dana) |
| `Diploma` (10) | diploma, svjedodžba | ne | ne |

Ostalo ide u postojeće: ugovor → `Contract`, vozačka → `Licence`, certifikati → `Certificate`,
ljekarski → `MedicalCheck`.

Paket "Novi radnik" (jedan klik = N zahtjeva, svaki sa svojim statusom, uprava može urediti):

1. Lična karta / osobna iskaznica — `Identity`, rok
2. Pasoš — `Identity`, rok
3. Viza / boravišna i radna dozvola (strani radnici) — `Visa`, rok
4. Ugovor o radu — `Contract` (obično ga ubacuje uprava, ne radnik)
5. Vozačka dozvola — `Licence`, rok; **standardno za sve**, jer svaki radnik može zadužiti vozilo
6. Diploma / certifikati — `Diploma` / `Certificate`

Napomena: paket nije izveden iz podataka test servera nego iz ovog popisa; zahtjevi koji ne vrijede za
radnika (npr. viza za domaćeg) skidaju se prije slanja.

**Posljedica (prijedlog, čeka odluku):** kad radnik zaduži vozilo (`check-out-to-me`) ili ga uprava dodijeli, sustav
provjerava ima li radnik vozačku s važećim rokom. Opcije: (a) samo upozorenje upravi i radniku,
(b) blokada samostalnog zaduživanja bez važeće vozačke (uprava može dodijeliti ručno). **ODLUČENO: (a) upozorenje** (2026-10-15). (b) tek kad se potvrdi s kupcem, jer blokada na terenu može zaustaviti rad.
