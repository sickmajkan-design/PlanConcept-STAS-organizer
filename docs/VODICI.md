# Vodiči za korištenje platforme

Sve na srpskom (ijekavica). Isti sadržaj, kraće, nalazi se u aplikaciji: izbornik **Uputstva**, ili **Ctrl+K** pa upišite riječ.

| Vodič | Za koga | O čemu |
|---|---|---|
| [VODIC_DOKUMENTI.md](VODIC_DOKUMENTI.md) | kancelarija | dodavanje dokumenata (i ispuštanje bilo gdje na stranici), pretraga, pregled, rokovi, dijeljenje s radnikom, revizija |
| [VODIC_ZAHTJEVI_ZA_DOKUMENTE.md](VODIC_ZAHTJEVI_ZA_DOKUMENTE.md) | kancelarija i radnici | traženje dokumenta, slanje s telefona, prihvatanje i vraćanje |
| [VODIC_DOKUMENTI_PODSJETNICI.md](VODIC_DOKUMENTI_PODSJETNICI.md) | kancelarija i radnici | brojač dokumenata, podsjetnici po dokumentu, telefon u smještaju, podsjetnik za prijavu |
| [VODIC_KNJIGA_TROSKOVA.md](VODIC_KNJIGA_TROSKOVA.md) | Super Admin (i oni kojima je dodijeljeno) | pregled, zapisi, obračun mjeseca, isplate, ponavljajući troškovi, uvoz iz Excela, izvoz za knjigovođu |
| [VODIC_RASPORED.md](VODIC_RASPORED.md) | voditelji, Admin, Super Admin | raspored i planiranje radne snage |
| [VODIC_POSLOVNE_JEDINICE.md](VODIC_POSLOVNE_JEDINICE.md) | Admin, Super Admin | filijale, predstavništva, zaposleni po jedinicama |
| [VODIC_GORIVO_VOZILA_SMJESTAJ.md](VODIC_GORIVO_VOZILA_SMJESTAJ.md) | kancelarija | gorivo, vozila i smještaj |
| [VODIC_PRETRAGA_I_PRECICE.md](VODIC_PRETRAGA_I_PRECICE.md) | svi | Ctrl+K, radnje, prečice |
| [VODIC_PRIJAVA_I_SIGURNOST.md](VODIC_PRIJAVA_I_SIGURNOST.md) | svi | prijava, lozinka, drugi faktor, trag izmjena |

## Pravilo za razvoj

Svaka nova funkcija dolazi s pet stvari: **bilješka o izdanju** (u aplikaciji), **uputstvo** u `features/help/helpTopics.ts` (srpski i engleski), **vodič** u ovoj mapi (koraci i bar jedan primjer), **unos u pretragu Ctrl+K** (stranica, radnja ili grupa zapisa) i **srpski prevod** poruka servera koje korisnik može vidjeti (`serverMessages.more.sr.ts`).
