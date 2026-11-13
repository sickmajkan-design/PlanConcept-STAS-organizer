# Testiranje uploada dokumenata i slika — 2026-11-12

Samo lokalno (API `127.0.0.1:5000`, prazna baza, ClamAV 1.4 u lokalnom Dockeru). Nijedan fajl nije slan na internet. Za provjeru antivirusa korišten je EICAR, standardni bezopasni testni niz, a ne stvarni zlonamjerni kod.

## Sažetak

Glavni upload (`/api/Attachments`) je dobro zaštićen. Najveća praznina je **sedmični izvještaj gradilišta**, koji zaobilazi i provjeru sadržaja i antivirus. Pored toga, "dokument" je samo provjeren po prvih nekoliko bajtova, pa lažni Word/Excel, zip bomba i PDF s JavaScriptom prolaze.

## Nalazi

### U1 — Sedmični izvještaj prima bilo šta, bez antivirusa i bez provjere sadržaja (SREDNJE)

**Gdje:** `POST /api/WeeklySiteReports`, `CreateWeeklySiteReportCommand.cs` (stranice 322-327).

**Dokaz** (u istoj sesiji, isti fajlovi, različite rute):

| Fajl | `/api/Attachments` | `/api/WeeklySiteReports` |
|---|---|---|
| Tačan EICAR kao `.txt` | **409** "virus scan found something" | **201 prihvaćeno** |
| Program (`MZ...`) pod imenom `.pdf` | 409 "content does not match" | **201 prihvaćeno** |
| HTML pod imenom `.png` | 409 | **201 prihvaćeno** |
| ClamAV ugašen, čist PDF | **503** "files cannot be checked" | **201 prihvaćeno** |

Prihvaćeni fajlovi se čuvaju i kasnije vraćaju osoblju (`GET .../pages/{id}/content`) kao `application/pdf` ili `image/png`, sa `attachment` i `nosniff`. Prvi bajtovi koje je server vratio nazad: `MZ\x90\x00 this is a program` (kao `junk.pdf`), `<html><script>alert(...)` (kao `page.png`).

**Ko može:** predradnik i PM (201). Radnik i kupac ne mogu (403).

**Uzrok.** Jedini put koji zove `UploadAttachmentCommandHandler.VerifySignatureAsync` i `IVirusScanner.EnsureCleanAsync` su prilozi i odgovor na zahtjev za dokument. Sedmični izvještaj provjerava samo ekstenziju i veličinu. Pored ovoga, nijedna druga ruta koja prima fajlove (uvozi, logotipi) ne pokreće antivirus, ali ih server parsira i ne čuva kao dokument.

**Uticaj.** Predradnik s ugroženim nalogom ili telefonom može unijeti zaražen fajl koji osoblje u kancelariji otvara kao "pregled izvještaja". Zaštita `attachment` + `nosniff` sprječava izvršenje u pregledniku, ali ne i u Wordu, Excelu ili čitaču PDF-a na računaru osoblja.

**Popravak.**
1. U petlju kod `CreateWeeklySiteReportCommand` dodati poziv `VerifySignatureAsync` i `EnsureCleanAsync` prije `_storage.SaveAsync` (dvije linije po fajlu).
2. Bolje: jedna zajednička "kapija za upload" (potpis, antivirus, dubinska provjera iz U2) koju svaka ruta mora proći, i arhitekturni test koji pada ako kontroler s `IFormFile` ne ide kroz nju.
3. Test: W-1/W-3 iz ove sesije treba da vraćaju 409, a pri ugašenom ClamAV-u 503.

### U2 — Provjera sadržaja je samo "prvih 4-12 bajtova" (SREDNJE/NISKO)

`AttachmentRules.MatchesSignature` provjerava samo početak fajla. Proveravano i prošlo (201):

| Napad | Rezultat |
|---|---|
| **Zip bomba:** `bomb.xlsx`, 204 KB, raspakuje se u 200 MB | prihvaćeno kao prilog; ClamAV je ne prijavljuje |
| `PK` + izvršni kod pod imenom `.docx` (nije pravi dokument) | prihvaćeno |
| PDF s `/OpenAction` + `/JavaScript` | prihvaćeno |
| JPEG zaglavlje + HTML tijelo | prihvaćeno |
| HEIC zaglavlje + smeće | prihvaćeno |
| PNG 12000 × 12000 (136 KB, oko 144 MP) kao prilog i kao logotip | prihvaćeno (server ga ne dekodira, pa nema pada servera) |
| CSV s `=cmd|...` i `=HYPERLINK(...)` | prihvaćeno kao tekst (opasno tek ako ga neko otvori u Excelu) |

**Uticaj.** Server ne pada (fajlovi se čuvaju, ne obrađuju). Rizik je na strani osoblja: pregled dokumenta u panelu raspakuje zip bombu u pregledniku, pa se kartica može zamrznuti; dijagonalni PNG od 144 MP troši oko 576 MB po kartici (logotip se prikazuje svima). Stvarni zlonamjerni dokument s makroima otvoren u Wordu prolazi svaku provjeru osim ako ga ClamAV već poznaje.

**Popravak** (u istoj kapiji kao U1):
- **OOXML (`.docx/.xlsx`):** otvoriti kao zip, tražiti `[Content_Types].xml` i `word/` odnosno `xl/`, ograničiti broj stavki (npr. 10.000), ukupnu raspakovanu veličinu (npr. 100-200 MB) i omjer (npr. 100:1), odbiti `vbaProject.bin`, ActiveX i ugrađene izvršne fajlove.
- **PDF:** odbiti tokene `/JavaScript`, `/JS`, `/OpenAction`, `/Launch`, `/EmbeddedFile`, ili ga "spljoštiti" (rasterizirati) pri uvozu.
- **Slike:** pročitati dimenzije iz zaglavlja i ograničiti (npr. 12.000 px po strani i 100 MP); idealno ponovo kodirati (odstranjuje i sve skriveno i EXIF lokaciju ako to ne treba).
- **Tekst/CSV:** nema promjene na serveru; pri izvozu već nema formula (provjereno ranije), a upozorenje je dovoljno za preuzimanje.

### U3 — Uvoz Excela: bez gornje granice redova i loše greške (NISKO)

Uvozi (`material-movements`, `Accommodations`, `fuel-*`) dozvoljavaju 10 MB (zapakovano), a redova ne ograničavaju.

- **Mjerenje:** valjan `.xlsx` s 300.000 redova je zapakovan samo 3,6 MB; server je za jedan zahtjev potrošio 112 → **545 MB memorije** i 13-21 sekunda, a odgovor ima 299.999 redova s problemima. Pri granici od 10 MB to je oko 830.000 redova, odnosno više od 1,5 GB. Više paralelnih zahtjeva ruši mali server. Treba nalog s pravom uvoza, pa je rizik od unutrašnjeg korisnika ili ukradenog naloga.
- **XXE je blokiran:** `XmlException: DTD is prohibited` za `file:///C:/Windows/win.ini`, nijedan sadržaj fajla nije procurio. Isto za "billion laughs".
- **Ali** nevažeći, zlonamjerni i XXE fajlovi vraćaju **500** (`NullReferenceException` u ClosedXML-u, `XmlException`), umjesto 400 s porukom.

**Popravak:** ograničiti redove (npr. 20.000) i raspakovanu veličinu; uhvatiti izuzetke čitanja radne knjige i vratiti 400; odgovor ograničiti na prvih N problema.

### U4 — NUL bajt u polju forme daje 500 (NISKO)

`Title` s `\x00` u upload formi: `PostgresException 22021: invalid byte sequence 0x00`, zahtjev završi s 500. JSON tijela su već zaštićena (`NoNulCharacterStringConverter`, fuzz 0 grešaka), ali multipart polja zaobilaze taj konverter. Popravak: ukloniti ili odbiti `\0` u svim poljima forme.

### U5 — Pitanje za pravila: predradnik i PM mogu dodati fajl uz bilo kojeg zaposlenika (INFO)

Predradnik i PM su uspješno učitali prilog kategorije `Contract` uz tuđi zapis zaposlenika (201). Čitanje je i dalje samo Admin i više (radnik/predradnik ne mogu preuzeti tuđi prilog: 404/403). Radnik ne može učitavati ni uz svoj zapis (403, koristi zahtjev za dokument). Pitati Darija da li je to namjera.

### U6 — Nema ograničenja po zapisu ni ukupnog prostora (INFO)

Uz jedan zapis zaposlenika prihvaćeno je 60 dodatnih fajlova bez prigovora. Ograničenje broja uploada po nalogu (`UploadPermitLimit`, 60 u prozoru) nisam testirao s podrazumijevanom vrijednošću jer sam je za ovaj test podigao. Preporuka: gornja granica fajlova po zapisu (npr. 200), dnevna kvota po nalogu i alarm kad disk dostigne 80%.

## Šta je izdržalo

| Provjera | Rezultat |
|---|---|
| Lista dozvoljenih ekstenzija (12 tipova); `.html`, `.svg`, `.exe`, `.php`, `.pdf.exe`, bez ekstenzije, razmak/tačka na kraju | odbijeno (400) |
| Tip sadržaja izvodi se iz ekstenzije, ne iz zahtjeva klijenta | da |
| Zaglavlje ne odgovara (program kao `.pdf` ili `.png`) na `/Attachments` i na odgovoru na zahtjev | 409 |
| Tačan EICAR, i zipovan unutar `.docx` | blokiran (409); ClamAV 1.4 radi |
| ClamAV ugašen: `/Attachments` | **503, zatvara se** (ne propušta nepregledano) |
| Veličina: tačno 25 MB prolazi, 25 MB + 1 bajt 400, 40 MB 400, 150 MB prekinuta veza | ispravno, bez rasta memorije |
| 12 istovremenih ~24 MB uploada | svi 201, bez vidljivog rasta memorije |
| `../../evil.pdf` | ime očišćeno; ključ pohrane se generiše (GUID) i provjerava da ostane unutar korijena |
| CRLF u imenu (ubacivanje zaglavlja) | 400; preuzimanje `Content-Disposition: attachment` + `nosniff`, nema ubačenog zaglavlja |
| RTL znak u imenu | zaglavlje ga očisti (`invoice_gpj.pdf`) |
| Ime od 5000 znakova | 400 (granica 512) |
| SVG logotip sa `<script>`, `onload`, `javascript:`, XXE, entitetskom bombom | očišćen ili 409; servira se s `sandbox` CSP i `nosniff` |
| Anonimni upload | 401 |
| Kupac uploadira zaposleniku | 403 |
| Tuđi prilog preuzimanje (radnik 2, kupac, predradnik) | 404/403 |
| Pogrešan tip vlasnika (`Project` s ID-om zaposlenika) | 404 |

## Ograničenja ovog testa

- EICAR s dodatnim prefiksom (npr. `%PDF-1.4` ispred) ClamAV ne prijavljuje: njegov potpis je za cijeli fajl. Zato nisam mogao dokazati antivirus na putu odgovora za zahtjev za dokument (dozvoljeni su samo PDF i slike), nego samo na prilozima i unutar zipa. Kod ga poziva (provjereno čitanjem).
- Ponašanje u pregledniku za zip bombu i PNG od 144 MP je izračun, ne mjerenje.
- Stvarni zlonamjerni kod nisam koristio. ClamAV ne hvata nepoznate makroe.
- Ograničenje broja uploada (60/min) nije provjereno uživo.

## Redoslijed popravaka

1. **U1** (dvije linije + test): najveći dobitak.
2. **U2** (zajednička kapija i dubinska provjera zipa/PDF-a/slike).
3. **U3** (ograničenje redova i 400 umjesto 500) i **U4** (NUL).
4. Odluka o **U5** i kvote za **U6**.

## Stanje poslije popravaka (2026-11-12, lokalno, nije komitirano)

| Nalaz | Popravak | Dokaz |
|---|---|---|
| **U1** sedmični izvještaj bez provjera | `CreateWeeklySiteReportCommand`: ista provjera sadržaja i antivirus prije spremanja, za svaki fajl, a ništa se ne sprema ako jedan padne | 5 integracionih testova (nevažeći sadržaj, EICAR, antivirus ugašen = 503, ništa spremljeno); živo: program kao `.pdf` = 409 |
| **U2** provjera samo prvih bajtova | Novi `UploadContentInspector` u `VerifySignatureAsync` (dolazi do priloga, zahtjeva za dokument i sedmičnog izvještaja): Office je pravi zip (`[Content_Types].xml` + `word/` ili `xl/`, najviše 10.000 dijelova, 200 MB raspakovano, omjer 200:1, bez `vbaProject`, ActiveX, izvršnih), PDF bez `/JavaScript`, `/JS`, `/Launch`, `/EmbeddedFile`, slika s čitljivom veličinom do 120 MP | 24 jedinična testa; živo na obje rute: zip bomba 409, PDF s JavaScriptom 409, JPEG + HTML 409, PNG 12000 × 12000 409, fajl koji samo počinje kao zip 409, a pravi mali JPEG, PDF i docx prolaze (201) |
| **U3** uvoz Excela | `ClosedXmlFuelStatementParser`: granica 20 MB raspakovano prije otvaranja, 50.000 redova, sve greške čitanja u 409 s porukom | 5 jediničnih testova; živo: 300.000 redova = 409 za 0,0 s bez rasta memorije (prije 545 MB); XXE/oštećen fajl = 409 umjesto 500 |
| **U4** NUL u polju forme | `NoNulStringModelBinderProvider` (forme i upiti), isti 400 kao za JSON | integracioni test; živo: 400 |
| **U5**, **U6** | nisu mijenjani (odluka o pravilima i kvote) | |

Ograničenja: PDF provjera čita obične oznake (sakrivene u tokovima objekata neće se uhvatiti, vidi `shortcut:` u kodu), a nepoznati makro u ispravnom Office fajlu prolazi osim ako ga ClamAV već poznaje.
Pri prvom pokretanju sam našao jedan vlastiti propust (fajl `PK` + program je davao 500 zbog drugačijeg izuzetka iz biblioteke); popravljen i pokriven testom.
Testovi: 777 jediničnih prolazi, integracioni 1.397 od 1.414 (17 neuspjelih: 14 `ScheduleTests` koji koriste fiksni datum 3. 8. 2026, pa ih pravilo "najviše N dana unazad" odbija danas, i 3 moja koja su bila pogrešno napisana i sada prolaze). Pet dodatnih paketa (Upload*, Attachment*, DocumentRequest*) ponovo prolazi: 79 od 79.
