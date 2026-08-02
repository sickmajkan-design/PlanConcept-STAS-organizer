# Provisioning — koraci koje mora uraditi vlasnik / Owner-only setup steps

**SR** — Ovo su koraci koje kod ne može da uradi umesto vas, jer traže naloge
i tajne koje pripadaju vašoj organizaciji. Sve ostalo u Fazi 0 je odrađeno i
čeka samo ove vrednosti.

**EN** — These are the steps code cannot do for you: they need accounts and
secrets that belong to your organisation. Everything else in Phase 0 is done
and waiting on these values.

---

## 1. Firebase / Push notifikacije

Bez ovoga aplikacija radi, ali nijedna push poruka ne izlazi — mobilna
prijavljuje `unconfigured`, a API loguje umesto da šalje.
Without this the app runs, but no push is delivered — the mobile app reports
`unconfigured`, and the API logs instead of sending.

### 1.1 Napravi projekat / Create the project

1. https://console.firebase.google.com → **Add project**
2. Google Analytics nije potreban. / Google Analytics is not required.

### 1.2 Android klijent / Android client

1. **Add app → Android**
2. Package name mora biti tačno / must be exactly:
   `com.planconcept.construction_mobile`
3. Preuzmi `google-services.json` i stavi ga u / Download `google-services.json` into:
   `src/construction_mobile/android/app/google-services.json`

Fajl je git-ignorisan. Build ga sam detektuje: ako postoji, primenjuje se
Google Services Gradle plugin; ako ne postoji, build i dalje prolazi i ispisuje
upozorenje. / The file is git-ignored. The build detects it: present means the
Google Services Gradle plugin is applied, absent means the build still succeeds
with a warning.

### 1.3 iOS klijent / iOS client

1. **Add app → iOS**, bundle ID iz Xcode projekta / bundle ID from the Xcode project
2. `GoogleService-Info.plist` → `src/construction_mobile/ios/Runner/`
3. APNs ključ (`.p8`) iz Apple Developer naloga → Firebase → **Cloud Messaging →
   APNs Authentication Key**. Bez ovoga iOS push ne radi ni sa ispravnim plist-om.
   / Without the APNs key iOS push does not work even with a correct plist.

### 1.4 Server / API

1. Firebase → **Project settings → Service accounts → Generate new private key**
2. Spljošti u jedan red / flatten to one line: `jq -c . service-account.json`
3. Upiši u `.env` / put it in `.env`:

   ```
   FIREBASE_CREDENTIALS_JSON={"type":"service_account",...}
   ```

Ovaj ključ može da pošalje push svakom instaliranom uređaju — u produkciji ide
u secret manager, nikad u fajl koji se komituje. / This key can push to every
installed device — in production it belongs in a secret manager, never in a
committed file.

### 1.5 Provera / Verify

```
docker compose up -d
# prijavi se na telefonu, pa iz admin panela pošalji obaveštenje
# sign in on the phone, then send a notification from the admin panel
```

Ako i dalje ne stiže, pogledaj log API-ja za
`Firebase is not configured` — to znači da `FIREBASE_CREDENTIALS_JSON` nije
stigao do kontejnera. / If nothing arrives, check the API log for
`Firebase is not configured` — that means the variable never reached the
container.

---

## 2. Potpisivanje Android builda / Android release signing

### 2.1 Napravi keystore — jednom, zauvek / Create the keystore — once, for good

```
keytool -genkey -v -keystore ~/stas-organizer-upload.jks \
  -keyalg RSA -keysize 2048 -validity 10000 -alias upload
```

> **Ako izgubite ovaj keystore, aplikacija na Play-u se više nikada ne može
> ažurirati.** Google ne prihvata build potpisan drugim ključem; jedini izlaz
> je novi listing i molba svim korisnicima da instaliraju ispočetka. Ide u
> password manager organizacije, ne na jedan laptop.
>
> **Lose this keystore and the app on Play can never be updated again.** Google
> will not accept a rebuild signed with a different key; the only remedy is a
> new listing and asking every user to reinstall. It belongs in the
> organisation's password manager, not on one laptop.

### 2.2 Poveži ga / Wire it up

```
cd src/construction_mobile
cp android/key.properties.example android/key.properties
# popuni storeFile, storePassword, keyAlias, keyPassword
```

I `key.properties` i `.jks` su git-ignorisani. / Both are git-ignored.

### 2.3 Build

```
flutter build appbundle --release
```

Bez `key.properties` build i dalje prolazi, ali je potpisan debug ključem —
Play će odbiti upload. To je namerno: greška se javlja pri slanju, a ne tiho na
uređajima korisnika. / Without `key.properties` the build still succeeds but is
debug-signed — Play rejects the upload. That is deliberate: the failure happens
at submission, not silently on users' devices.

---

## 3. Šta i dalje ne radi posle ovoga / What still does not work after this

Pošteno, da ne bude iznenađenja. / Stated plainly so there are no surprises.

| Stavka / Item | Stanje / State |
|---|---|
| GPS dok je aplikacija u pozadini ili ekran ugašen | **radi** / works |
| GPS kad korisnik ukloni aplikaciju iz „recents" | **ne radi** — Android uništava aktivnost, a sa njom i servis. Fiksevi u redu čekanja se čuvaju i šalju pri sledećem pokretanju. / does not work — the activity is destroyed and the service with it. Queued fixes survive and go out on next launch. |
| GPS posle restarta telefona | **ne radi** dok se aplikacija ne otvori / does not work until the app is opened |
| iOS build | nije građen ni potpisan — traži Apple Developer nalog i Mac / not built or signed — needs an Apple Developer account and a Mac |

Trajno rešenje za prva dva je zaseban background-service paket koji pokreće
drugi Flutter engine, nezavisan od aktivnosti. To je nova zavisnost i zaseban
posao — namerno nije uzeto u Fazi 0. / The permanent fix for the first two is a
separate background-service package running a second Flutter engine,
independent of the activity. That is a new dependency and its own piece of
work — deliberately not taken on in Phase 0.
