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
- **Stranica bez podataka za period** (Obračun mjeseca bez obračuna) ne prikazuje prazne tabele s nulama: jedna traka kaže šta nedostaje, uz jedan glavni korak (Novi mjesec) i jedan sporedni (Uvoz iz Excela, samo Super adminu). Oznaka stanja koja nije dugme (Mjesec otvoren) je neutralna, a boja upozorenja ostaje za zaključan mjesec.
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
- **Početni ekran:** slika gradilišta ide do vrha ekrana (prozirna statusna traka, ikone prate temu), pozdrav ima datum ispod, a **jedino puno narandžasto dugme** je prijava na smjenu (`AppTheme.heroAction`). Odjava je tiha (prozirno svijetla), da se ne pritisne slučajno. Dok se čeka lokacija, dugme zadrži boju i pokaže obrtač.
- Lokacija van smjene je normalno stanje, pa je jedna siva linija; kartica se pokazuje samo kad nešto treba uraditi (dozvola, isključena usluga).
- Prazno stanje (npr. nema zadataka) ima ikonu i okvir, ne goli tekst.
- **Brze radnje** (`QuickActionTile`): četiri najčešća posla (nedostatak, dokumenti, raspored, odsustva) kao pločice u redu, s blagim utiskivanjem pri pritisku. **Dugme koje prati**: dok kartica smjene nije vidljiva, a radnik nije na smjeni, "Prijavi se na smjenu" stoji iznad donje trake (prati se stvarni položaj kartice, ne unaprijed zadan pomak).
- **Donja traka lebdi** (`_FloatingBar` u `app_shell.dart`): zaobljena, malo prozirna (zamućenje), sadržaj klizi ispod nje. Ekrani u traci koriste `SafeArea(bottom: false)`, a liste dodaju donji razmak koji je jednak visini trake (`PagedListView` to radi sam). Uz pojačan kontrast traka je puna boja. Novi ekran u traci mora ostaviti taj razmak, inače se zadnji red ne može dohvatiti.
- **Prijava i odjava se povlače** (`SwipeToConfirm`): klizač do kraja, ne dodir; pusti li se ranije, vraća se oprugom s brzinom prsta. Čitač ekrana ga aktivira jednim dodirom. Obični dugmići (Sačuvaj, Pošalji) ostaju dodir.
- **Donji listovi** koriste `sheetMotion(context)` (kratak odskok na kraju, zatvaranje brže); pri smanjenom kretanju bez putanje. Svaki novi `showModalBottomSheet` prima `sheetAnimationStyle: sheetMotion(context)`.
- Prije objave: release izdanje (ne debug) na emulatoru, jer debug izdanje na ovom računaru javlja "ne odgovara" dok traži lokaciju.

## 5. Izgled: svijetao i taman

- Panel prati uređaj dok osoba ne izabere; dugme **Izgled** u zaglavlju (na telefonu u meniju naloga) kruži prema uređaju, svijetao, taman. Izbor se pamti u pregledniku (`ui.colorMode`), a `theme-color` i `color-scheme` se mijenjaju s njim.
- Boje se uzimaju iz teme (`divider`, `background.paper`, `primary`), nikad kao `#e0e0e0` ili `white`. Jedine tvrde boje su one koje moraju ostati iste u oba izgleda: QR kod na bijeloj podlozi, oznake poslovnih jedinica i kartice grafikona.
- Taman izgled je topao, skoro crn (#12100e), a narandžasta je svjetlija (#ff8a3d) da zadrži kontrast.

## 6. Smirivanje i telefon u panelu

- **Narandžasta je za ono što se može pritisnuti** (glavno dugme, aktivna stavka u meniju, veze). Kartice nadzorne ploče nemaju narandžastu traku ni sjaj: tanka linija (`divider`) i blaga neutralna sjenka (u tamnoj temi samo linija).
- Naslovi kartica i stranice su težine 600, brojevi u pokazateljima 700.
- Grafikon bez ijednog podatka zamjenjuje jedna rečenica ("Još nema evidentiranog prihoda ove godine."), ne prazna osa.
- **Ispod 600 px** zaglavlje ima samo meni, izbor poslovne jedinice, zvono i avatar; pomoć i jezik su u meniju naloga. Naziv jedinice smije zauzeti sav slobodan prostor. Naslov početne je "Danas" s datumom.
- Pokazatelji (`StatTile`) su dva u redu, nazivi se prelamaju u dva reda, nula je prigušena. Redovi prečica ispod njih se pomjeraju u stranu umjesto da se prelome i budu odsječeni.
- **Početna na telefonu** počinje pregledom "Danas" (`TodaySummary`): koliko je ljudi na smjeni i do tri vrste posla koje čekaju (isti izvor kao widget "Zahtijeva pažnju"), svaki red je veza na listu gdje se rješava.
- Izbor poslovne jedinice na telefonu je dijalog koji se diže odozdo (kao ostali dijalozi), ne padajući meni.
- **Pokazatelji (`StatTile`)** imaju istu neutralnu pozadinu; boja je samo u maloj ikoni, nule su prigušene. Sporedna dugmad na nadzornoj ploči ("Dodaj widget", prvi korak u praznom widgetu) su neutralna s tankim okvirom.
- Widget čiji bi grafikon bio prazan (prihod, vozila, alati) kaže šta nedostaje i nudi prvi korak.
- **Meni (odobreno 17.10.):** natpis ispod svake ikone na lijevoj traci; Dokumenti i Zahtjevi za dokumente su vlastita grupa; Finansije imaju tri naslova (Svaki dan, Troškovi po vrsti, Obračun i postavke). Raspored ikona nije mijenjan.
- **Poništi umjesto pitanja:** što se lako pravi ponovo (zadatak, oglas, grupa za notifikacije, zakazani izvještaj) briše se bez "Jeste li sigurni?" (`useDeleteWithConfirm(..., { immediate: true })`): red odmah nestane (`hiddenIds`), a Poništi stoji šest sekundi. Novac, zaposleni, vozila i dokumenti zadržavaju pitanje.
- **Učitavanje:** oblik onoga što dolazi (skeleton) umjesto obrtača u karticama panela i listama aplikacije (`SkeletonList`); sa smanjenim kretanjem stoji mirno.
- **Evidencija u Finansijama (17.10.):** jedna grupa na traci; stavke Evidencije su ispod stavki Finansija pod naslovom "Evidencija" (i "Evidencija: podešavanja"). Vidljivost je ista kao prije (`canViewBook`): Super admin i kome on to dodijeli, nikom drugom ni naslov.
- **Dugmad i statusi u panelu:** sporedno (outlined) dugme je neutralno (siva linija) u cijeloj temi, narandžasto je samo glavno (puno) dugme; status Aktivan je blaga zelena oznaka, puna boja za pažnju; tabovi bez velikih slova; široka tabela (`ResourceDataGrid`) pokazuje sjenku na rubu na kojem ima još sadržaja. Datumska polja su ugrađena pa prate jezik preglednika (dd.mm.gggg u srpskom).
- **Prazna lista:** dvije vrste (`ResourceDataGrid`): još ništa nije uneseno (naslov, rečenica, glavno dugme za dodavanje; prop `empty`) i filter je previše uzak ("Ništa ne odgovara", Očisti filtere; props `filtered` i `onClearFilters`, a `useListQueryState` daje `hasFilters` i `clearFilters`). Nova lista s dugmetom za dodavanje treba proslijediti `empty`.
- **Kolone:** koje su kolone važne po listi piše u `components/gridColumnDefaults.ts` (jedno mjesto za izmjenu); uz `columnsKey` lista dobija dugme Kolone, ispod 1500 px zadano se vide važne, a izbor osobe se pamti (`grid.columns.<ključ>`). Lista koja nije navedena nema dugme.
- **Raspored:** pet prikaza je jedan red (`ToggleButtonGroup`) s ikonom i kratkom riječi (`planning.tab.*Short`), puni naziv i opis u oblačiću i kao pristupačno ime, a na užem ekranu samo ikone. Filter ko se prikazuje je jedno dugme Filteri (oznaka kad nije "Svi"), legenda je jedan red s "+ još N". Nijedan naslov dugmeta se ne lomi u dva reda.
- **Brojevi uz meni (obavezno pri svakoj izmjeni menija):** broj na ikoni grupe je zbir brojeva njenih stavki (`withGroupTotals`), a stavke koje imaju šta da čeka nabrojane su u `BADGED_PATHS` (`useNavBadgeCounts.ts`). Kad se stavka premjesti, doda ili ukloni, prođite i kroz brojeve: `navBadges.test.ts` pada ako stavka s brojem više nije u meniju ili ako grupa ne daje zbir svojih stavki. Novu stavku sa listom čekanja dopišite u `BADGED_PATHS` i u hook.
- **Zvono (obavještenja):** lista se nastavlja učitavati do posljednjeg obavještenja (15 po stranici, "To su sva obavještenja"); obavještenje bez stranice na koju vodi otvara se u cijelosti u prozoru.
- **Računar, i dalje bez izričitog odobrenja:** zaglavlje (uključujući liniju ispod naziva firme) i naslovna.

## 7. Šta još nije urađeno (odluke)

- Zaglavlja stranica: izvoz i istorija su sada iza jednog dugmeta (`MoreActionsMenu`) u Evidenciji i Rasporedu; lista zaposlenih zadržava uvoz uz glavno dugme.
- "Ti" tekstovi koji ostaju namjerno: obavještenja koja stižu radniku (`notificationText.*`) i pomoćnik; tekstovi panela su na "vi" (provjerava `formalVoice.test.ts`).
- Datumska polja koriste format preglednika; na računaru s srpskim jezikom prikazuju 16.10.2026.
- Brend (logo i naziv firme) u zaglavlju panela mijenja položaj prema širini ekrana i prema tome da li je meni prikvačen.
