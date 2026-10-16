# Plan: iOS aplikacija

Status: PLAN (ništa nije izgrađeno za iOS). Datum: 2026-10-16.
Odluka vlasnika proizvoda (2026-10-15): **iOS aplikacija je potrebna**, jer dosta radnika ima iPhone.

## 1. Gdje smo

| Što | Stanje |
|---|---|
| Flutter projekt | Jedan Dart kod za Android i iOS; `src/construction_mobile/ios/` (Runner, Xcode projekt) postoji |
| `Info.plist` | Već ima tekstove dozvola: lokacija (u radu i u pozadini), kamera, galerija, pozadinski načini rada |
| Paketi | Firebase (push), kamera/galerija, QR skener, lokacija, sigurna pohrana, datoteke, URL launcher: svi imaju iOS podršku |
| Nove funkcije (Moji dokumenti, zahtjevi) | Čisti Dart; rade na iOS-u bez dorade, osim posebnog dijela ispod |
| Što nedostaje | Apple Developer račun, Firebase iOS aplikacija (`GoogleService-Info.plist`), APNs ključ, `Podfile` (nastaje pri prvom buildu na Macu), potpisivanje, TestFlight |
| Mac | Nemamo; simulator radi samo na macOS-u |

## 2. Blokada i redoslijed

1. **Apple Developer račun (99 USD/god)**: prvi korak, na ime koje će biti u App Storeu; potvrda ponekad traje dan-dva.
2. **Build u oblaku** (bez kupovine Maca): GitHub Actions macOS runner (provjera da se projekt kompajlira, bez potpisivanja) i/ili Codemagic (potpisivanje i TestFlight).
3. **Firebase**: dodati iOS aplikaciju (bundle ID), preuzeti `GoogleService-Info.plist`, učitati APNs ključ iz Apple računa.
4. **Potpisivanje**: certifikati i provisioning profili (automatski preko Codemagica).
5. **TestFlight**: prvi build na pravom iPhoneu, pa interni testeri.
6. **App Store**: opis, politika privatnosti (`docs/PRIVACY.md` je osnova), snimke ekrana, obrazloženje pozadinske lokacije.

Do tada iPhone korisnici mogu otvoriti panel u Safariju (privremeno rješenje; PWA ekrani su u backlogu).

## 3. Što treba prilagoditi za iOS (poznato unaprijed)

| Područje | Napomena |
|---|---|
| **Zaštita ekrana osjetljivih dokumenata** | Na Androidu je `FLAG_SECURE`; iOS nema isti prekidač. Opcije: prekrivanje sadržaja kad aplikacija ide u pozadinu (zaštita sličice u izborniku aplikacija) i otkrivanje snimanja ekrana (`UIScreen.isCaptured`). Treba posebna implementacija (MethodChannel u `AppDelegate`) |
| **Pregled dokumenata** | `open_filex` na iOS-u otvara iOS pregled; HEIC fotografije iPhonea su uobičajene: server ih prima, ali pretvorba za pregled je u backlogu |
| **Push** | Preko APNs-a; tekst obavijesti ostaje generički (kao danas) |
| **Lokacija u pozadini** | Apple traži jasno obrazloženje i ograničava korištenje; tekst u `Info.plist` je već prilagođen, ali pregled u App Storeu može tražiti dodatne dokaze |
| **Privremene datoteke** | `OpenedDocuments` radi i na iOS-u (cache folder); brisanje pri odjavi je već u kodu |
| **Kamera i galerija** | `NSCameraUsageDescription` i `NSPhotoLibraryUsageDescription` su dodani; za spremanje u galeriju treba dodati `NSPhotoLibraryAddUsageDescription` ako se ikad bude radilo |
| **Skener QR** | `mobile_scanner` traži kameru; provjeriti na pravom uređaju |

## 4. CI korak (kad se odobri)

Mali posao, ne traži Apple račun: posao `ios` u GitHub Actions na `macos-latest` koji pokreće `flutter pub get`, `flutter build ios --no-codesign` i javlja ako se projekt ne kompajlira. Plaća se deset puta skuplje po minuti od Linuxa, pa ide samo na promjene u `src/construction_mobile/**`.

## 5. Procjena napora (kad račun postoji)

| Korak | Napor |
|---|---|
| CI korak (kompajliranje) | oko 1 sat |
| Firebase iOS + APNs | oko pola dana, uz pristup konzoli |
| Codemagic potpisivanje i TestFlight | oko 1 dan |
| Zaštita ekrana na iOS-u | oko pola dana |
| Testiranje na pravom iPhoneu i ispravke | 1 do 2 dana |
| App Store objava | 1 do 3 dana (pregled kod Applea) |

## 6. Pitanja za kupca

1. Na čije ime ide Apple Developer račun (firma ili osoba)?
2. Koliko radnika ima iPhone, i koja verzija iOS-a je najstarija koju treba podržati?
3. Je li dovoljna privatna distribucija (TestFlight, do 10 000 testera), ili je potrebna javna objava u App Storeu?
