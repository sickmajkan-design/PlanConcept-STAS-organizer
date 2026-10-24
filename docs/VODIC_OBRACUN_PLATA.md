# Vodič: Obračun plata po radniku za mjesec (Finansije > Evidencija)

> **Gdje je:** izbornik **Finansije > Evidencija**. Svaki mjesec je jedna tabela (list), a svaki radnik je jedan red u tabeli gradilišta na kojem je radio. Plata se **računa sama** iz sati i cijene; vi upisujete samo ono što program ne zna.

Za koga je: Admin i Super Admin s pravom na Finansije (iznosi).

> Ne mješati s **Knjigom troškova** (Evidencija novca, stranica „Obračun mjeseca“). To je pregled po gradilištima i isplate. Ovdje je obračun **po radniku**; iz ove tabele Knjiga uzima zaradu radnika.

---

## Formula koju program koristi

| Kolona | Kako se dobije |
|---|---|
| **Sati** | zbir sedmica (KW) |
| **Zarada radnika** | cijena sata radnika × sati **+** godišnji odmor **+** refundacija **+** razlika/bonus **−** akontacija |
| **Naplata klijentu** | sati × cijena sata za klijenta (ili iznos izdatih faktura, ako je gradilište paušal ili mjerenje) |
| **Marža** | naplata − zarada radnika |
| **Rezultat** | marža − doprinosi − rent a car − gorivo − stanovanje − akontacija − regres |

Kućice koje se računaju same imaju oznaku formule. Ako upišete vrijednost preko njih, to je **ručna izmjena** (vidi se strelica „Vrati na izračunatu vrijednost“).

---

## Korak po korak

### 1. Napravite novi mjesec
1. **Finansije > Evidencija > Novi mjesec.**
2. Izaberite godinu i mjesec.
3. U „Kako želiš početi?“ izaberite **Obračun radnika (preporučeno)**: gotove kolone s ugrađenim računom. (Alternativa: *Kopiraj strukturu iz prošlog mjeseca*; prenose se cijene sata i regres, sve ostalo je prazno.)
4. Uključite **Popuni gradilišta i radnike iz sistema**: program napravi sekciju za svako gradilište i red za svakog radnika koji je tog mjeseca bio raspoređen na njemu.
5. Ako vodite više poslovnih jedinica, izaberite jedinicu čiji je ovo platni spisak.

### 2. Upišite sate iz potpisanih satnica
- Kolone **KW..** su kalendarske sedmice mjeseca. Sate svakog radnika upisujete u sedmicu u kojoj su rađeni.
- Mjerodavne su **potpisane satnice**. Za svaku sedmicu priložite skeniranu potpisanu satnicu (dugme za potpisane satnice na gradilištu). Dok nije priložena, program upozori u listi „Za provjeru“.
- Sati iz aplikacije su samo **kontrola**: ako se razlikuju od upisanih, program to javi kao podsjetnik. Ako ipak želite da se sedmice pune same iz odobrenih unosa, pri pravljenju mjeseca uključite **Popuni sate iz aplikacije**.

### 3. Provjerite cijene
- **Cijena sata radnika** dolazi iz kartona zaposlenog, a **cijena sata za klijenta** iz gradilišta. Ako nedostaje, program upozori („ima sate, ali nema cijenu“).
- Cijenu možete ručno prepisati u tom redu; izmjena se označi.

### 4. Upišite ono što program ne zna
Samo ove kolone se upisuju ručno:
- **Doprinosi**: iz platne liste. Upišite 0 ako ih nema, inače stoji upozorenje.
- **Razlika od prošle plate / bonus**
- **Akontacija (−)**: ono što je radniku već isplaćeno tog mjeseca.
- **Regres**

Automatski se pune: **Gorivo, Rent a car, Stanovanje** (iz njihovih modula), **Godišnji odmor (+)** (iz odobrenih odsustava) i **Refundacija (+)** (iz odobrenih refundacija radnika).

### 5. Pregledajte „Za provjeru“
Lista iznad tabele pokazuje šta ne štima: sati bez cijene, ručno prepisane kućice, sati upisani dva puta (više od onog što mjesec ima), neodobreni sati, nedostajući doprinosi, sedmica bez potpisane satnice. Riješite ili provjerite svaku stavku.

### 6. Pogledajte rezultat
- Na kraju svakog gradilišta je **zbir** i rezultat gradilišta.
- **Sažetak** (kućice) pokazuje ukupnu maržu, troškove i **neto zbir** (zarada firme). Dodati možete kućice za fiksne troškove, alat i uniforme (**Dodaj kućicu**).
- **Izvezi u Excel** daje istu tabelu za knjigovođu.

### 7. Zaključajte mjesec
**Zaključaj mjesec** kad je sve provjereno i poslato knjigovođi. Brojke se čuvaju takve kakve jesu i ne mogu se mijenjati. Ispravka traži **Otključaj** uz razlog, koji se upisuje u evidenciju izmjena.

---

## Primjer

Radnik **Marko**, gradilište **Hala B**, oktobar:
- cijena sata radnika 12 €, sati 160 → 1.920 €
- godišnji odmor +80 €, akontacija −500 €
- **Zarada radnika = 1.920 + 80 − 500 = 1.500 €**
- klijent se naplaćuje 18 €/h → naplata 2.880 €, **marža = 2.880 − 1.500 = 1.380 €**

---

## Isplata plate

Obračunatu zaradu isplaćujete u **Evidencija novca > Isplate radnicima** (po grupama: radnici, kooperanti, kancelarija): akontacije i konačna isplata = zarada − akontacije. Isplata ne mijenja dobit, samo tok novca.

## Česta pitanja

**Radnik se ne pojavljuje u tabeli.** Nije bio raspoređen na gradilištu tog mjeseca. Dodajte red ručno (**Dodaj red**) i povežite ga sa zaposlenim.

**Zarada izlazi 0.** Provjerite da su upisani sati i da postoji cijena sata radnika.

**Kućica je pokvarena nakon ručnog upisa.** Kliknite strelicu uz nju da se vratite na izračunatu vrijednost.

**Mjesec je zaključan, a treba ispravka.** Otključajte uz razlog, ispravite i ponovo zaključajte.
