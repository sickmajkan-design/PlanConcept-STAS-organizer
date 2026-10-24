# Vodič: Sedmični izvještaji sa gradilišta (pregled, odobravanje, upis u Evidenciju)

> **Gdje je:** **Planiranje > Rad i zadaci > Sedmični izvještaji**. Pregled i odobravanje: Admin i Super admin. Slanje: poslovođa (i ured, u ime poslovođe).

Poslovođa svake sedmice pošalje izvještaj za gradilište: **slike potpisane satnice** i **radnike s njihovim satima**. Ured provjeri podatke uz sliku, ispravi ako treba i odobri; odobreni sati se sami upišu u mjesečnu tabelu u **Finansije > Evidencija**.

## Rok i opomena

- Izvještaj za prošlu sedmicu se šalje do **ponedjeljka u 12:00**.
- Ako do roka nema izvještaja za gradilište na kojem su bili raspoređeni radnici, poslovođa dobija **opomenu u utorak ujutro**, jednom po gradilištu i sedmici. Ured vidi takva gradilišta pod **Kasni**.

## Slanje (poslovođa, ili ured u njegovo ime)

1. **Novi izvještaj:** izaberite gradilište i sedmicu (KW) i vrstu (Potpisani sati, Aufmaß, Drugo).
2. **Radnici i sati:** dodajte radnika po radnika i upišite sate za cijelu sedmicu. Radnik može biti bilo ko, ne samo raspoređeni na gradilište.
3. **Sedmica koja prelazi u novi mjesec** (npr. 28.9.–4.10.): za svakog radnika upišite sate posebno za oba mjeseca.
4. **Slike:** dodajte sve slike potpisane satnice u isto slanje. Ako je sedmica na više listova, svaki list je jedna slika. Slike možete i prevući u okvir.
5. Dva izvještaja za isto gradilište i sedmicu se **spajaju u jedan pregled** (slike i radnici se dodaju jedni drugima).

> Android aplikacija za sada šalje izvještaj kao ranije (jedna slika i ukupan zbir). Radnike tada dodajete u pregledu.

## Pregled (ured)

Čipovi iznad liste: **Čeka pregled**, **Potrebna korekcija**, **Kasni**, **Odobreno**. Dugmad na redu:

| Dugme | Šta radi |
|---|---|
| **Pregledaj** | otvara izvještaj: slike lijevo (listanje, uvećanje), radnici i sati desno |
| **Preuzmi** | snima sliku na računar |
| **Odobri** | samo za stari izvještaj bez popisa radnika |

U pregledu:
- Svaki red pokazuje šta **trenutno** piše u Evidenciji za tog radnika i sedmicu: *prazno*, *isto* ili *razlikuje se*.
- Sate možete promijeniti, radnika zamijeniti, red dodati ili obrisati. **Zbir radnika** se poredi sa zbirom koji je upisao poslovođa.
- Gdje se razlikuje od onog što već piše, birate **Zamijeni** (zadano) ili **Zadrži staro**.
- **Odobri i upiši u Evidenciju:** sati idu u pravu sedmicu (kolona KW) i red radnika; sekcija gradilišta i red se naprave ako ih nema.
- **Potrebna korekcija:** traži razlog; poslovođa ga vidi u obavijesti i šalje novi izvještaj koji zamjenjuje stari. Ako je izvještaj već bio odobren, upisani sati se **povlače** (ručno upisano ostaje).

Odobravanje neće proći ako mjesec **nema Evidenciju** (napravite je: Finansije > Evidencija > Novi mjesec) ili je **zaključan** (otključajte uz razlog).

## Evidencija poslije odobrenja

- Ćelije upisane iz izvještaja su **zelene** (tooltip: upisano iz odobrenog izvještaja).
- Iznad tabele piše **koliko je sati odobreno iz izvještaja, a koliko upisano ručno**. Zarada radnika, naplata i marža se računaju iz svih sati, kao i prije.
- Sate uvijek možete ručno ispraviti; ćelija tada prestaje biti zelena.

## Obavijesti i widget

- Ured dobija obavijest čim izvještaj stigne; poslovođa dobija obavijest o odobrenju ili vraćanju (s razlogom).
- Na početnoj stranici se može dodati widget **Sedmični izvještaji** (čeka pregled, korekcija, kasni).

## Primjer

Poslovođa Hale B pošalje KW42 s dvije slike i šest radnika. Ured otvara Pregledaj, vidi da Igor ima 42,5 h na slici a poslovođa je upisao 40, ispravi, a kod Adina vidi da u Evidenciji već piše 30 h pa izabere Zadrži staro. Odobri: pet ćelija se upiše, jedna ostaje; iznad tabele piše da je 212,5 h odobreno iz izvještaja.
