# QPlan: plan brenda

Nacrt, 2026-11-26. Vlasnik brenda: El Plan Concept. Vektorski fajlovi znaka su u `brand/`.

## 1. Sažetak

- **Ime:** QPlan (čita se "kju-plan"). Piše se tačno ovako: jedna riječ, veliko Q i P. Ne "Q-Plan", ne "Qplan", ne "QPLAN" (osim u pečatu i na predmetima gdje je sve velikim slovima).
- **Znak:** zaobljeno slovo Q (prozor Rasporeda) s tri trake kao na traci vremena. Rep slova je kabl.
- **Šta je QPlan:** platforma za raspored ljudi, opreme i gradilišta, evidenciju sati i troškova za građevinske i elektro firme.
- **Obećanje:** "Ko, gdje, s čim i kad: na jednom mjestu."
- **Potpis proizvođača:** "by El Plan Concept", malim slovima ispod znaka, na stranici prijave, u e-mailovima i podnožju.

## 2. Kome govorimo

| Publika | Šta im treba | Kako pričamo |
|---|---|---|
| Vlasnici i direktori firmi | pregled, kontrola troškova, manje telefona | kratko, o rezultatu, bez tehnike |
| Kancelarija i planeri | raspored, oprema, odsustva | konkretno, imenima funkcija iz panela (Raspored, Oprema, Sati) |
| Voditelji gradilišta i radnici | jednostavno, na telefonu | veoma kratke rečenice, krupno, bez stručnih izraza |

Ton: jasan, miran, praktičan. Kratke rečenice. Ijekavica. Bez uzvičnika i bez rečnika "revolucija", "inovativno". Nazive iz aplikacije pišemo kako stoje u njoj (Raspored, Gradilište, Oprema).

## 3. Znak i logotip

- **Znak:** `brand/qplan-mark*.svg`. **Logotip** (znak + riječ): `brand/qplan-logo*.svg`. Riječ "Plan" je nacrtana kao vektor (linija iste debljine, zaobljeni krajevi), pa ne zavisi ni od jednog pisma.
- **Varijante:** u boji (svijetla podloga), za tamnu podlogu, bijela (na narandžastoj ili slici), jednobojna tinta (štampa, pečat, faks). Ikonica aplikacije: `brand/qplan-icon.svg`. Favicon (pojednostavljen, deblje linije): `brand/qplan-favicon.svg`.
- **Zaštitni prostor:** oko logotipa najmanje širina jedne trake znaka (oko 7% visine znaka) pomnoženo sa tri; dovoljno je jedno "Q" visine praznog prostora sa svake strane.
- **Najmanja veličina:** znak 16 px (ekran) ili 6 mm (štampa); logotip 80 px širine ili 25 mm.
- **Ne radimo:** ne rastežemo, ne rotiramo, ne mijenjamo boje van tabele, ne dodajemo sjene i sjaj, ne stavljamo znak na šarenu pozadinu bez bijele ili jednobojne verzije, ne mijenjamo razmak između znaka i riječi.

## 4. Boje

| Naziv | Hex | Upotreba |
|---|---|---|
| Narandžasta | `#E65100` | glavna boja, ikonica, dugmad, prsten znaka |
| Škriljac | `#37474F` | rep i dvije trake znaka, sekundarni tekst |
| Tinta | `#1D1D1F` | riječ "Plan", naslovi |
| Svijetla narandžasta | `#FF8A3D` | narandžasta na tamnoj temi |
| Svijetli škriljac | `#B0BEC5` | škriljac na tamnoj temi |
| Papir | `#F5F5F7` | pozadine ekrana |

Ista paleta je u panelu i aplikaciji već danas, pa prelaz ne mijenja izgled. Narandžasta nikad ne ide na narandžastu: tu koristimo bijelu verziju.

## 5. Pismo

- **Aplikacije i panel:** sistemsko pismo (San Francisco, Segoe UI, Roboto), kako je sada.
- **Logotip:** nacrtano kao vektor, ne kuca se.
- **Štampa i web:** za naslove debeli (700) sans, uz čvrst razmak slova (-0.03em). Ako se bira pismo za cijeli brend: Inter ili Manrope su besplatni i slični zaobljenom znaku.

## 6. Ime, zaštita i rizici (prije većih ulaganja)

Provjereno je samo da li se adrese otvaraju i šta pretraga nalazi, to nije pravna provjera.

- **Žig QPLAN** je registrovan u SAD na Qualcomm (podnesen 1992). Vjerovatno u drugoj klasi proizvoda, ali to treba da potvrdi pravnik.
- **Q-PLAN International** (Grčka) radi poslovno planiranje i komercijalizaciju na EU projektima, pa postoji blizak naziv.
- **Domene:** qplan.com, qplan.de, qplan.eu i qplan.me su zauzeti. Izgleda slobodno: qplan.app, qplan.io, qplan.ba, qplan.rs, qplan.hr (provjeriti kod registratora).
- **Preporuka:** odmah registrovati qplan.app i qplan.ba; za Njemačku i EU ne prijavljivati žig bez savjeta pravnika (EUIPO TMview pregled klasa 9, 35, 42). Za BiH, Srbiju i Hrvatsku rizik je manji.

## 7. Plan uvođenja

**Gotovo u kodu (ovaj paket):**
- naziv QPlan u panelu (kartica, manifest, prijava, meni, javne stranice podrške i privatnosti), u aplikaciji (Android i iOS naziv, početni ekran, prijava, prevodi), u e-mailovima (ime pošiljaoca), u API dokumentaciji i pomoćniku
- novi znak u panelu (bočna traka, gornja traka, prijava) i u aplikaciji, ikonice aplikacije (Android, iOS, web, PWA, favicon)
- vektorski fajlovi u `brand/`

**Sljedećih 30 dana:**
- domene (qplan.app, qplan.ba) i adrese e-pošte (podrska@, info@), stranica podrške i privatnosti na novoj domeni
- prijava na App Store i Google Play pod imenom QPlan: naziv, ikonica 1024 px, opis (tekstovi u `docs/APPSTORE_TEKSTOVI.md`)
- pečat i vizitke s novim znakom (rezervisana mjesta za naziv i sjedište firme)
- potpis "by El Plan Concept" u e-mailovima, zaglavlju prijave i podnožju

**Sljedećih 90 dana:**
- memorandum, koverta, majice i kacige za ekipu (u dokumentu s primjerima: ploče "Majice i kaciga", "Pečat, vizitke, memorandum")
- predstavljanje Darijovim jedinicama: svaka zadržava svoj logo u panelu, a QPlan je kao platforma u istoj traci
- jednostavna web stranica proizvoda (naslov, tri mogućnosti, demo, kontakt)

**Kasnije:**
- zaštita imena (žig u BiH i regionu, pa po potrebi EU)
- vlastito pismo logotipa ako brend ide dalje od regije
- video od 30 sekundi (Raspored u pokretu)

## 8. Šta namjerno nije preimenovano

Interni identifikatori ostaju kakvi su, jer se vide samo programerima, a promjena bi pokvarila instalirane aplikacije i servere:
- nazivi koda i prostora imena (`Construction.*`, `construction_admin`, `construction_mobile`)
- Android paket i iOS bundle id (promjena znači novu aplikaciju u prodavnici)
- nazivi slika na serveru (ghcr), repozitorij, Docker nazivi
- JWT izdavač (`construction-api`): promjena bi odjavila sve korisnike

## 9. Šta ostaje ručno

- Stranica preuzimanja APK na test serveru i naziv fajla (`construction-organizer-<verzija>.apk`): sljedeći APK nazvati `qplan-<verzija>.apk` i promijeniti naslov stranice.
- Naziv i ikonica u konzolama prodavnica (Apple, Google) kad aplikacije budu prijavljene.
- Pečat i vizitke: upisati tačan naziv firme i sjedište (u tekstovima App Store dokumenta stoji "El Plan Concept d.o.o. Derventa").
- Ako se ime nekad promijeni: jedna tačka u panelu (`nav.appName`), jedna u aplikaciji (`appName` u prevodima).
