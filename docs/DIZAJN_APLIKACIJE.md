# Izgled aplikacije: pravila koja vrijede na svakom ekranu

Aplikacija ima jedan izgled. Ovo su pravila koja ga drže istim; dio ih provjerava test
(`test/design_consistency_test.dart`), pa ih novi ekran ne može tiho prekršiti.

## Raspored

- **Razmak od ivice ekrana je 16.** Daje ga lista (padding), ne kartica.
- **Kartica nema vlastitu marginu** (`CardTheme.margin` je nula). Razmak između kartica je 8: u listi sa
  straničenjem daje ga `PagedListView`, u običnoj listi `margin: EdgeInsets.only(bottom: 8)`.
- Unutrašnji razmak kartice je 16.

## Tekst u kartici

- Naslov kartice: `titleMedium`, debljina 600. Sporedni redovi: `bodySmall` u `onSurfaceVariant`.
- Status je uvijek `StatusChip` (jedan oblik, boje po značenju: zeleno odobreno/dostavljeno, žuto čeka/u toku,
  crveno odbijeno). Nikad običan `Chip` s vlastitim bojama. Visok prioritet `secondaryContainer`, hitno `errorContainer`.

## Kontrole

- **Filter** je `FilterChip`, uključen ili isključen (nikad `ChoiceChip`).
- **Glavna radnja na kartici** je `OutlinedButton`; tiha radnja (povuci) je `TextButton` sa ikonom.
- **Nova stavka** je `FloatingActionButton.extended` sa `+`.
- **Prazna lista** je `EmptyView` (ikona i rečenica), nikad samo tekst.
- **Unos** ide u donji list (`showModalBottomSheet`) sa naslovom `titleLarge`; polja bez brojača znakova.

## Tekst

- Rečenice su u obraćanju sa „vi" ("Niste tražili…", "Napišite…"). Dugmad su u imperativu ("Dodaj", "Povuci").
- Ijekavica ("dijeljenje", "korištenje", "morat ćete").
- Datum je `dd.MM.yyyy.`; iznos ima dvije decimale i oznaku valute.
