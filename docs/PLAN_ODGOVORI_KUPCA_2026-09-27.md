# Odgovori kupca 2026-09-27: šta mijenjaju

Ulaz: 14 odgovora Darija Stankovića (El Plan Concept GmbH) od 26.–27.9.2026. Numeracija odgovora je njegova. Uz svaki odgovor je pitanje na koje ga vežem; oznaka (?) znači da vezu nisam mogao sa sigurnošću utvrditi.

## 1. Odgovori i posljedice

| # | Pitanje (moje tumačenje) | Odgovor | Posljedica |
|---|---|---|---|
| 1 | Potpisane satnice (A3) | Čuvati uz klijenta i projekat, po kalendarskim sedmicama | A3 dobija ključ **klijent + projekat + KW**. Isto u FA7 ([PLAN_FAKTURE_PO_FIRMAMA.md](PLAN_FAKTURE_PO_FIRMAMA.md)). |
| 2 | Kooperanti | Piše se samo naziv firme; sate dobija uz sate svojih radnika ili ih kooperanti pošalju mejlom | Red kooperanta = **naziv firme, bez imena osobe**; sati se upisuju ručno. Zatvara pitanje 6 iz liste. |
| 3 | Prag razlike sati prema aplikaciji (A2): 4 sata u mjesecu | Da | **Prag 4 h u mjesecu je potvrđen** (ostaje podesiv). Zatvara pitanje 7. |
| 4 | Paušal koji traje više mjeseci | Prema odrađenom poslu | Raspodjela po mjesecima je **ručna, prema odrađenom**; nema formule. Zatvara pitanje 2. |
| 5 | Aufmaß | Zbir računa | **Dovoljan je zbir računa**, bez vezivanja za aufmaß izvještaj. Zatvara pitanje 3. |
| 6 | DKV: može li poslati izvod sa zaglavljima, i ostaje li mjesečni uvoz | "Da li ti znači PDF šta?" | Protupitanje: znači li nam PDF za uvoz DKV podataka. **Da, ali slabo.** Uvoz iz CSV/Excel je pouzdan (postoji mapiranje kolona). PDF izvod je slika stranice i čitanje tabele iz njega je nepouzdano; vidi §3, tačka 1. |
| 7 | Godišnji: radni ili kalendarski dani, pravo | Radni dani, 20 dana godišnje | Zadano pravo 20 ostaje; dani se broje **radnim danima**. |
| 8 | Godišnji: srazmjerno, prenos | Računa se srazmjerni dio; stari godišnji se koristi **do 1.6. naredne godine** | Prenos ističe **1. juna**. Podsjetnik prije isteka (B11 raste na Should). |
| 9 | Godišnji: iznos za dan | Firma određuje iznos; kod njega **32 € neto po danu** | **Postavka firme "iznos godišnjeg po danu"** (zadano prazno, kupac postavlja 32 €). Zamjenjuje "dani × sati × cijena" iz B10. |
| 10 | "Uprava" | Samo uprava, tj. **Super Admin i Admin** | Uprava = Super Admin + Admin. Voditelj **nije** uprava (odgovor na P-1). |
| 11 | Ko odobrava odsustva; ko šta vidi | Samo Admin i Super Admin. Voditelj vidi projekat na kojem je raspoređen, svoju flotu, raspored. **Niko ne vidi satnice drugih, cijene poslova ni bilo šta u € osim Super Admina i onih kojima on dodijeli; to fiksno ne može ići na radnike** | Vidi §2, promjene 1–3. |
| 12 | Widget aktivni projekti | Aktivni projekti, planirani projekti, radnici na projektu | Widget prikazuje **aktivne, planirane i broj radnika**. Rok i budžet nisu tražili. |
| 13 | Refundacije | Svaki iznos traži odobrenje za isplatu uz platu; obavezan račun ili faktura na uvid | **Prilog je obavezan**; bez priloga se zahtjev ne šalje. Uvijek kroz platu. |
| 14 | (?) Klijent sa više firmi (P-6) | "Ovo nisam najbolje razumio" | Kupac ne odlučuje; **implementaciju određuje proizvod**. Odluka: klijent može imati više firmi, radnik se raspoređuje u jednu firmu po projektu, a fakturisanje ide po firmi (jedna faktura sa podjelom ili po jedna za svaku, oba načina). Klijent sa jednom firmom radi kao dosad. Vidi FA4 i B12–B14. |

## 2. Promjene koje ovo traži u postojećem

1. **Odobravanje odsustava.** Danas to smije **Predradnik i iznad** (`AbsencesController.Review`, politika `ForemanAndAbove`). Kupac traži samo **Admin i Super Admin**. Promjena API-ja, widgeta "Zahtjevi za odsustvo" (K1) i aplikacije; Voditelj i Predradnik mogu vidjeti zahtjeve svog gradilišta, ali ne odlučivati.
2. **Refundacije.** Provjeriti ko odlučuje (mora biti Admin i Super Admin) i da je račun obavezan (kupac: bez računa nema isplate).
3. **Pravo pregleda iznosa.** Uvesti pravo "vidi iznose (€)" koje **dodjeljuje samo Super Admin, po osobi**, a **ne može se dati Radniku**. Obuhvata satnice drugih, cijene poslova, plate, obračun, fakture, troškove. Svaka dodjela ide u audit log. Radnik vidi samo svoje sate.
4. **Voditelj** vidi samo projekat na kojem je raspoređen (raspored, flota, ekipa), bez €. Ovo sužava i D6 iz liste ("Predradnik i ostalo ograničenje pristupa") za Voditelja.
5. **Godišnji u plati (B10)**: iznos = broj dana × iznos po danu iz postavke (32 € neto), ne sati × cijena.
6. **Godišnji pravila (B8)**: radni dani, srazmjerno pravo pri zaposlenju usred godine, prenos do 1.6., pola dana ostaje otvoreno.
7. **Widget "Aktivni projekti" (B2)**: statusi aktivan i planiran, i broj radnika po projektu.

### Stanje promjena (2026-09-27)

| Promjena | Stanje |
|---|---|
| 1. Odobravanje odsustava samo Admin i Super Admin | Urađeno (API, panel, obavještenja, testovi) |
| 2. Refundacije odlučuju samo Admin i Super Admin | Urađeno; obavezan račun uz zahtjev još nije |
| 3. Pravo na iznose: ne može na Radnika ni Kupca | Urađeno (API, forma, testovi) |
| 3a. Plate i satnice (zarada radnika, pregledi radnika i projekta, izvoz plata, praznici) traže pravo na iznose, ne samo ulogu | Urađeno |
| 3b. Ostali iznosi koje vide uloge sa gradilišta (evidencije troškova materijala, vozila, alata, smještaja i njihovi izvozi) | Urađeno: evidentiranje ostaje otvoreno gradilištu, čitanje iznosa traži pravo. Izvještaji troškova su to već tražili. |
| 4. Voditelj samo svoj projekat, flota, raspored | Nije urađeno |
| 5–7 | Nije urađeno |

Napomena: Voditelj i Predradnik bez prava i dalje mogu evidentirati troškove i vide količine, ali ne iznose. Nisu još provjerena sva mjesta gdje se pojavljuje iznos (npr. cijena na kartici materijala, najam vozila i alata, aplikacija na telefonu); to je stavka za prolaz kroz cijelu aplikaciju.

## 3. Šta i dalje treba pitati

1. **DKV izvod:** može li se na DKV portalu izvod preuzeti kao **CSV ili Excel** (obično se može, uz "Export")? Ako je samo PDF, uvoz se radi kao dodatak sa ručnom provjerom. Molba: jedan stvarni izvod, brojeve kartica može prekriti.
2. ~~Klijent sa više firmi~~ — odlučeno (odgovor 14).
3. **Fakture:** izdajete li ih u programu, ili je dovoljan nacrt za knjigovođu (vidi FA u [PLAN_FAKTURE_PO_FIRMAMA.md](PLAN_FAKTURE_PO_FIRMAMA.md))?
4. **Ko dobija pravo na iznose** osim Super Admina (npr. knjigovođa, Admin)? Predlog: nikoga zadano, samo izričito.
5. **Refundacija:** gornja granica i valuta (P-3 još nije odgovoren u dijelu o valuti).

## 4. Predloženi redoslijed poslije odgovora

1. **Prvo prava (promjene 1–4):** mijenjaju ono što je već isporučeno i sigurnosno su najosjetljivije.
2. Sprint obračuna A1, A2, A4, A5, A6 (nepromijenjeno).
3. Godišnji B8–B10 sa novim pravilima, refundacija: obavezan prilog.
4. B12–B13 (firme), zatim fakture FA1–FA6.
