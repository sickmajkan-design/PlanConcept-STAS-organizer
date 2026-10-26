# Vodič: koferi s alatom, brzi unos i kupac uz projekat

Za kancelariju (Admin i Super Admin). Isti sadržaj, kraće, nalazi se u aplikaciji: **Uputstva**, ili **Ctrl+K** pa „kofer“ ili „brzi unos“.

## 1. Kupac uz ime projekta

Svugdje gdje se pokazuje projekat (raspored, liste projekata, sati, materijal, radni nalozi, narudžbe, alati i vozila, izvještaji, kontrolna tabla) pored imena stoji **kupac** ljubičastom bojom.

- Na uskim mjestima (raspored, telefon) kupac je u drugom redu, ispod imena.
- Projekat bez kupca pokazuje **„Bez kupca“**, da se vidi šta treba popuniti.
- Kupac se uzima s projekta. Da ga promijenite, izmijenite projekat.
- Isti kupac može imati više projekata, pa ime kupca pomaže da se razlikuju projekti sličnog naziva.

## 2. Brzi unos vozila i alata

**Vozila** (ili **Alati**), pa dugme **Brzi unos**. Isto je u Ctrl+K: „brzi unos“.

1. Svaki red je jedno vozilo ili alat, a svako polje koje se može mijenjati je ćelija. Kliknite ćeliju i pišite. **Tab** ide udesno, **Enter** nadolje.
2. Datumi se pišu kao `2027-03-31`. Pogrešan datum se označi crveno.
3. Izmijenjene ćelije imaju plavi rub dok se ne snime.
4. **Isto za više redova:** označite redove, izaberite polje i vrijednost, pa **Primijeni na označene**. Vrijednost se upiše u te redove, a snimate tek na kraju.
5. **Snimi izmjene** šalje samo redove koje ste mijenjali. Ako neki red ne uspije (npr. registracija je već zauzeta), piše koji i zašto, a ostali su snimljeni.

Zaglavlje vozila pokazuje VIN, gorivo, QR, ko ga drži i na kom je projektu, uz datume registracije, tehničkog, osiguranja i servisa. Zaglavlje alata pokazuje serijski broj, QR, ko ga drži i na kom je projektu.

**Primjer.** Pet vozila ima osiguranje do 31. marta. U Brzom unosu vozila označite petoro, izaberite *Osiguranje do*, upišite `2027-03-31`, kliknite *Primijeni na 5* i *Snimi izmjene*.

**Još nije dodato:** lijepljenje redova iz Excela i skeniranje QR koda u tabeli.

## 3. Kofer s alatom

Kofer je alat koji ima popis onoga što je u njemu. Svaka stavka ima **količinu** i **cijenu jednog komada**, pa je vrijednost kofera zbir.

**Pravljenje.** *Alati*, *Dodaj alat*, označite **„Ovo je kofer s više alata“**. To se kasnije ne mijenja. Kofer ima svoj QR kod, dodjeljuje se radniku i projektu kao svaki alat.

**Sastav.** U kartici *Sastav kofera* dodajte stavke. Izmjena: dvostruki klik na naziv ili meni (tri tačke), pa *Izmijeni*. Uklonjena stavka ostaje u istoriji kofera.

**Kad stavka nestane** (meni stavke, *Prijavi da nedostaje*), pa jedno od:

| Izbor | Šta znači |
|---|---|
| Zaduži radnika | Cijena stavke se zadužuje radniku koji drži kofer (ili onome kog izaberete). Ostaje otvorena dok je ne označite **Naplaćeno**. |
| Otpiši | Niko se ne zadužuje. |
| Radnik kupio zamjenu | Radnik je svojim novcem kupio zamjenu, a stavka je opet u koferu. Birate: **Refundirati radniku** (iznos je unaprijed puna cijena, a možete ga promijeniti) ili **Bez refundacije**, ako je štetu napravio radnik. Pri refundaciji birate način: **uz platu** ili **gotovinom na ruke**. Iznos ostaje otvoren na koferu dok ga ne označite **Isplaćeno**. |
| Vrati u kofer | Stavka je nađena ili zamijenjena iz magacina. |

**Kad se stavka pokvari** (*Prijavi kvar*): pokvarena stavka **mora doći do uprave**. Kad stigne, izaberite *Uprava primila pokvarenu stavku*. Tek tada možete otpisati, vratiti u kofer ili označiti zamjenu koju je radnik kupio.

Na vrhu kartice stoje zbirovi: vrijednost kofera, šta nedostaje, kvar, šta se naplaćuje od radnika i šta se refundira radnicima. Ispod su otvoreni iznosi i istorija: ko je šta prijavio i odlučio.

**Primjer.** Kofer električara E-014 ima 12 stavki i vrijedi 186 KM. Marko prijavi da su nestala kliješta (12 KM). Zadužite ga, a kad naplatite, kliknete *Naplaćeno*.

**Gdje završava isplata.** Platforma sama ne dodaje iznos na platu i ne izdaje gotovinu: samo bilježi šta se kome duguje i kako treba da se isplati. Uz platu: kancelarija uključi iznos u platu radnika pa na koferu označi Isplaćeno. Gotovinom: predate novac pa označite Isplaćeno. Cijenu možete promijeniti i pri zaduženju i pri refundaciji. Zaduženje, otpis i refundaciju rade samo Admin i Super Admin.

## 4. Kopiranje koferâ i alata

U koferu ili alatu kliknite **Kopiraj**.

1. Upišite **koliko primjeraka** (do 50).
2. Upišite **naziv** s oznakom `{n}` za broj, npr. `Kofer električara E-{n}`, i **od kog broja** počinje, i koliko cifara ima broj (`3` daje `015`).
3. Izaberite šta se kopira: stavke i količine, cijene, poslovna jedinica.
4. U pregledu vidite nazive koji će nastati. **Napravi N** pravi primjerke.

Svaki primjerak dobija **novi naziv i novi QR kod**. Ne kopiraju se serijski broj (pripada jednom fizičkom komadu), ko ga drži, projekat, istorija i dokumenti. Novi primjerak kreće **slobodan**. Ako promijenite sastav kasnije, mijenja se samo taj kofer, a kopije ostaju kakve jesu.

**Primjer.** Kopiranjem kofera E-014 tri puta, od broja 15, nastaju E-015, E-016 i E-017.

## Za razvoj

- Kofer je `Tool` s `IsKit`. Stavke: `ToolKitItem`, istorija: `ToolKitEvent`. Prelazi stanja su u `ActOnToolKitItemCommand`.
- Kopiranje: `CopyToolCommand` (`POST /tools/{id}/copy`). Testovi: `ToolKitTests`.
- Kupac uz projekat: `ProjectLabel` (jedan hook, `useProjectCustomers`, učitava kupce svih projekata i keširaju se pet minuta).
- Brzi unos: `pages/quickEntry/QuickEntryPage.tsx`, snima postojećim `PUT` poziva, red po red.
