# Dizajnerska pravila platforme

Kratak spisak onoga što je dogovoreno i provjereno pregledom dizajna od 16. oktobra 2026. (panel na računaru i telefonu, aplikacija na emulatoru). Svaka nova stranica ili ekran prati ih. Dopunjuje [UX_CONVENTIONS.md](UX_CONVENTIONS.md).

Pregled je rađen alatima iz paketa `emilkowalski/skills` (`emil-design-eng`, `mobile-native`, `break-ui`), koji se koriste za svaku izmjenu izgleda.

---

## 1. Boja i ton

- **Narandžasta (#E65100)** je boja radnje: glavno dugme, aktivna stavka menija, ikona aplikacije. U aplikaciji je to #C4510A (tamnija nijansa koja nosi bijeli tekst, kontrast oko 5:1); toplo smeđa je samo za "karticu dana" na početnom ekranu.
- **Sivoplava (#546E7A)** je boja obavijesti (`info`): trake s objašnjenjima i savjetima. Zadano plava iz biblioteke ne smije se pojaviti nigdje.
- **Jedno primarno dugme po stranici.** Ostale radnje su obrubljene ili u izborniku "Više".
- **Obraćanje s "vi"** u cijelom panelu, uključujući Evidenciju. Radniku u aplikaciji se obraća kratko i bez titula.
- **Jezik:** srpski, ijekavica (vidi [VODICI.md](VODICI.md)).

## 2. Prazna stanja i prvi dojam

- Prazna lista **uvijek kaže šta dalje**: dugme koje vodi na sljedeći korak ("Prikaži sve dokumente", "Otpremi"), ne samo "ništa nema".
- **Kartica za podešavanje** na početnoj stranici se sklapa u jednu liniju čim je urađen bar jedan korak; broj koraka je u naslovu, a izbor se pamti.
- Naslov stranice govori šta stranica **jeste** ("Dokumenti"), ne jedan njen filter.

## 3. Telefon (panel)

- Nijedna stranica ne smije biti šira od ekrana. Red dugmadi koji se ne prelama (`flexWrap: wrap`) ili tabela bez `overflowX: auto` pomjera cijelu stranicu u stranu. Provjerava ga test "nothing scrolls sideways" u `phone.spec.ts`, koji obuhvata i Evidenciju.
- Ciljevi dodira su najmanje 44 px (tema to radi sama za `pointer: coarse`).
- Nema sivog bljeska pri dodiru (`-webkit-tap-highlight-color`), dugmad se na pritisak blago utisnu (`scale(0.97)`, 140 ms, bez animacije za `prefers-reduced-motion`), a kontrole nisu označive dugim pritiskom.
- Široka tabela unutar kartice ima sjenku na rubu kad ima još sadržaja (`className="scroll-hint"`).
- Prije objave: stranicu pogledati na širini telefona **sa podacima**, ne samo prazno. Najgori realan slučaj: ime od 40 znakova, e-mail od 60.

## 4. Aplikacija na telefonu

- **Ikona i početni ekran** su u brendu (narandžasto "C"), ne zadani logotip okvira. Izvor su ikone panela u `construction_admin/public/icons`; adaptivna ikona (pozadina + bijelo slovo) i splash su u `android/app/src/main/res`.
- **Kratka vibracija** (`HapticFeedback`) pri prijavi i odjavi sa smjene i pri promjeni kartice; radnik s rukavicama zna da je dodir primljen i bez gledanja.
- Dugmad su visine 52 px. Kad je neki podatak još u toku (npr. "Tražim lokaciju…"), dugme to kaže, a ne samo onemogući se.
- Isti podatak se ne prikazuje dvaput u jednoj kartici (npr. status i isti status kao oznaka).
- Prije objave: release izdanje (ne debug) na emulatoru, jer debug izdanje na ovom računaru javlja "ne odgovara" dok traži lokaciju.

## 5. Šta još nije urađeno (odluke)

- Obilazak preostalih "ti" tekstova (dašbord vidžeti, obavijesti radniku): treba odlučiti ide li cijela platforma na "vi".
- Zaglavlja stranica s previše kontrola (lista zaposlenih ima deset): jedno primarno dugme, uvoz i izvoz u izbornik "Više".
- Tamna tema panela (aplikacija je već ima).
- Brend blok u zaglavlju panela stoji sredinom trake; vidjeti da li ga treba pomjeriti ulijevo.
- Datumska polja pokazuju format preglednika (npr. 10/16/2026), ne naš (16.10.2026.).
