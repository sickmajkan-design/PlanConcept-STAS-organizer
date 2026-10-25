# Vodič: pretraga (Ctrl+K) i prečice

Jedno polje za sve: **stranice, radnje, zapisi, dokumenti i uputstva**. Otvara se prečicom **Ctrl+K** (na Macu **Cmd+K**), ili klikom na polje za pretragu u zaglavlju.

---

## 1. Kako se koristi

1. Pritisnite **Ctrl+K** bilo gdje u panelu.
2. Upišite **bar dva slova**.
3. **Strelice gore i dolje** biraju, **Enter** otvara, **Esc** zatvara.

Bez upisa vidite **nedavno otvorene zapise**, radnje i sve stranice koje možete otvoriti.

Rezultati su uvijek samo ono što vaša uloga smije otvoriti: radnik ne dobija finansije, administrator ne dobija Knjigu dok mu nije dodijeljena.

---

## 2. Šta se pretražuje

| Grupa | Šta nalazi | Primjer upisa |
|---|---|---|
| **Stranice** | sve stranice menija, i po srodnim riječima | *uvoz*, *akontacija*, *raspored* |
| **Radnje** | početak nečeg novog | *otpremi*, *zatraži*, *novi radnik* |
| **Uputstva** | koraci, primjeri i savjeti iz stranice Uputstva | *rok*, *podsjetnik*, *zaključaj* |
| **Zaposleni, projekti, kupci, vozila, alati, materijal** | zapisi po nazivu i broju | *Horvat*, *ZG-123*, *hala* |
| **Dokumenti** | naslov, ime datoteke, vlasnik, napomena i **oznake** | *viza*, *ugovor marko* |
| **Zahtjevi za dokumente** | šta je zatraženo ili od koga | *pasoš*, *esad* |
| **Knjiga zapisa** | opis, druga strana, gradilište | *gorivo*, *dkv* |
| **Odsustva, refundacije, narudžbe, fakture, evidencije, oglasna ploča** | po osobi, opisu, broju | *novak*, *F-2026* |
| **Kartice i izvod za gorivo** | broj kartice, red izvoda | *874* |

Kad upišete **više riječi**, svaka mora da se podudara (*viza marko* nalazi samo Markovu vizu).

---

## 3. Radnje

Radnje nude početak nečeg novog bez traženja po meniju. Prikazuju se samo one koje vaša uloga smije.

| Radnja | Gdje vodi |
|---|---|
| **Otpremi dokument** | Dokumenti, s već otvorenim dodavanjem |
| **Zatraži dokument** | Zahtjevi za dokumente, s otvorenim novim zahtjevom |
| **Uvezi mjesec iz Excela** | Knjiga troškova, uvoz |
| **Uvezi smještaj iz Excela** | uvoz smještaja |
| **Uvezi isporuke materijala iz Excela** | uvoz isporuka |
| **Uvezi izvod za gorivo** | uvoz izvoda za gorivo |
| **Provjera DKV izvoda** | usklađivanje izvoda |
| **Novi zaposleni, projekat, kupac, vozilo, alat, materijal** | odgovarajući obrazac |
| **Evidentiraj trošak vozila**, **Prijavi odsustvo**, **Novi unos radnog vremena**, **Nova radna stavka** | odgovarajući obrazac |
| **Pošalji obavještenje**, **Novi korisnik** | administracija |

---

## 4. Primjeri

- **„viza“** pokazuje dokumente s tom riječi u naslovu ili oznaci, uputstvo *Rokovi važenja i podsjetnici* i radnju *Otpremi dokument*.
- **„hala“** pokazuje projekat *Hala B* i zapise u Knjizi koji ga spominju.
- **„uvoz“** pokazuje sva četiri uvoza iz Excela.
- **„novak“** pokazuje zaposlenu Anu Novak, njena odsustva i dokumente na njenom kartonu.

---

## 5. Favoriti i nedavno

- **Zvjezdica** uz stranicu je dodaje u favorite.
- Zapisi koje ste nedavno otvorili stoje na vrhu kad ništa ne upišete.

---

## 6. Ostale prečice

Tipka **?** (kad ne pišete u polje) otvara popis svih prečica.

| Prečica | Šta radi |
|---|---|
| **Ctrl+K** | pretraga stranica, zapisa, dokumenata i uputstava, ili početak nečeg novog |
| **?** | prikaži popis prečica |
| **Esc** | zatvori prozor ili panel |
| **‹ ›** na zapisu | prethodni/sljedeći zapis iz liste iz koje je otvoren |

Traka s lijeve strane: zadržite miš preko ikonice grupe da vidite njene stranice, klik odlazi na glavnu.

---

## 7. Za razvoj

Ko dodaje novu stranicu ili funkciju mora je učiniti pretraživom:

- stranica u `layout/navConfig.tsx` je u pretrazi sama; za srodne riječi dodati redak u `layout/pageKeywords.ts`;
- nova radnja ide u `layout/paletteActions.tsx` (uz `keywords`);
- novi zapisi se dodaju kao grupa u `features/globalSearch/useGlobalSearch.tsx`, s provjerom prava (`SearchScope`);
- uputstvo u `features/help/helpTopics.ts` (srpski i engleski; test provjerava oba jezika).
