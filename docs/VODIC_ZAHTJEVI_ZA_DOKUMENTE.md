# Vodič: zahtjevi za dokumente

Uprava traži od ljudi dokument koji nedostaje. Osoba ga pošalje **sa telefona**, uprava provjeri i **prihvati** ili **vrati s razlogom**. Ništa se ne gubi po porukama i telefonskim pozivima: svaki zahtjev ima status, rok i historiju.

Namijenjeno kancelariji (Admin i Super Admin) i radnicima s aplikacijom. Povezano: [vodič o dokumentima](VODIC_DOKUMENTI.md).

---

## 1. Tok jednog zahtjeva

```
Uprava traži  →  Osoba šalje (aplikacija)  →  Uprava provjerava
                                                 ├─ Prihvati → dokument na kartonu, podijeljen s osobom
                                                 └─ Vrati s razlogom → osoba šalje ponovo
```

Statusi koje vidite u listi:

| Status | Šta znači |
|---|---|
| **Čeka osobu** | zatražen je, osoba još nije poslala |
| **Za provjeru** | osoba je poslala; čeka vas (ovo je na vrhu) |
| **Prihvaćeno** | dokument je na kartonu osobe i podijeljen s njom |
| **Vraćeno** | uprava je vratila s razlogom; osoba treba poslati ponovo |
| **Povučeno** | uprava je odustala |
| **Kasni** | rok je prošao, a dokument nije stigao |

---

## 2. Kako zatražiti dokument

1. Otvorite **Zahtjevi za dokumente** (izbornik Administracija) i kliknite **Zatraži dokument**. (Prečica: **Ctrl+K**, „zatraži“. Na kartonu zaposlenog isto dugme već ima izabranu osobu.)
2. Izaberite **jednu ili više osoba**.
3. Izaberite šta tražite: pojedinačni dokument (vrsta i naslov, npr. *Pasoš*), ili paket **Novi radnik**, koji traži uobičajeni set odjednom.
4. Po želji upišite **napomenu** (npr. „obje strane lične karte“) i **rok**.
5. Pošaljite. Osobe su **odmah obaviještene** u aplikaciji.

Pravila:

- Isti zahtjev za istu osobu se ne pravi dvaput dok prvi čeka; pri slanju piše koliko ih je preskočeno.
- Jedna osoba može imati ograničen broj otvorenih zahtjeva istovremeno.
- Fotografija se ne traži kao dokument (traži se konkretna vrsta: pasoš, viza, ljekarski pregled…).

### Primjer: tri nova radnika

Počinje rad u ponedjeljak, a Dario, Mira i Esad nemaju ništa na kartonu.

1. Zatraži dokument > izaberite sva tri imena.
2. Paket **Novi radnik**, rok **petak**.
3. Pošaljite. Sva trojica dobijaju obavijest u aplikaciji sa spiskom šta se traži.
4. U petak otvorite listu: filter **Samo kojima je rok istekao** pokazuje ko kasni, a **Za provjeru** ono što je stiglo.

---

## 3. Šta radi osoba (aplikacija)

1. Dobije obavijest, otvori **Više > Moji dokumenti** (ili obavijest) i vidi šta se traži, uz rok i napomenu.
2. Izabere **Slikaj** ili **Izaberi** i pošalje fotografije ili PDF. Može više datoteka za jedan dokument (npr. obje strane).
3. Zahtjev prelazi u **Za provjeru**; osoba vidi da je poslala.
4. Ako je uprava vrati, vidi **razlog** i šalje ponovo. Svaki pokušaj se broji.

Ima ograničen broj pokušaja po zahtjevu; kad se potroše, aplikacija traži da se osoba javi kancelariji.

---

## 4. Provjera i odluka

1. Otvorite zahtjev (**Provjeri**). Vidite poslane datoteke, može se otvoriti pregled.
2. **Prihvati.**
   - Ako dokument ističe (viza, ljekarski pregled), upišite **Vrijedi do**: od tog datuma kreću [podsjetnici](VODIC_DOKUMENTI_PODSJETNICI.md).
   - Dokument se **stavlja na karton osobe i dijeli s njom**.
3. Ili **Vrati…** i upišite **razlog**: najmanje pet znakova, **ništa o zdravlju osobe**. Osoba ga čita u aplikaciji. Ponuđeni su gotovi razlozi:
   - *Slika je mutna ili se ne može pročitati. Slikajte ponovo na dobrom svjetlu.*
   - *Nedostaju stranice. Pošaljite cijeli dokument.*
   - *Ovo nije dokument koji je traženo.*
   - *Dokument je istekao. Pošaljite važeći.*
4. Zahtjev koji više ne treba: **Povuci zahtjev**. Osoba je obaviještena.
5. Povučen zahtjev može se **trajno obrisati** (**Obriši zahtjev**, uz potvrdu): nestaje iz liste i s telefona osobe, a datoteke koje je poslala ostaju na njenom kartonu, nepodijeljene. Brisanje se upisuje u reviziju.

Svaki zahtjev pamti **historiju** (ko je šta uradio i kada) i **broj pokušaja**.

### Primjer: mutna fotografija

Mira je poslala pasoš, ali je slika mutna.

1. Otvorite zahtjev > **Vrati…**.
2. Izaberite razlog *Slika je mutna ili se ne može pročitati…* i **Vrati osobi**.
3. Mira odmah vidi razlog u aplikaciji i šalje ponovo (**2. pokušaj**).
4. Kad stigne čitka slika: **Prihvati**. Pasoš je na njenom kartonu i vidljiv joj je u aplikaciji.

---

## 5. Obavijesti

- Osoba: odmah kad se zatraži, vrati ili povuče zahtjev.
- Uprava: kad osoba pošalje, odgovor je u listi bez osvježavanja stranice.
- E-mail obavijesti o zahtjevima nisu uključene (planirano kao opcija).

---

## 6. Česta pitanja

**Mogu li zatražiti isti dokument od cijele ekipe?** Da: izaberite više osoba; svako dobija svoj zahtjev.

**Osoba kaže da nema šta da pošalje.** Povucite zahtjev; osoba je obaviještena, a historija ostaje.

**Gdje završava dokument kad prihvatim?** Na kartonu osobe, u kartici Dokumenti, s datumom isteka i podsjetnicima koje ste postavili. Vidljiv je osobi u aplikaciji.

**Ko vidi zahtjeve?** Samo uprava (Admin i Super Admin). Radnik vidi samo svoje.

**Mogu li zahtjev obrisati odmah?** Prvo ga povucite; obrisati se može samo povučen zahtjev (prihvaćeni se ne briše).
