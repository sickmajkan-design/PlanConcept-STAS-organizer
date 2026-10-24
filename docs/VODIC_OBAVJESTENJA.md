# Vodič: Obavještenja (slanje, istorija, ko je pročitao)

> **Gdje je:** zvonce u gornjem redu > **Novo obavještenje** i **Poslana obavještenja**. Šalju Admin i Super admin.

## Slanje

1. **Naslov** i **poruka**.
2. **Kome šaljem:** dodajte jedan ili više izvora.

   | Izvor | Koga uzima |
   |---|---|
   | **Radnik** | jednu osobu, po imenu |
   | **Grupa** | članove grupe za obavještenja (Administracija > Grupe) |
   | **Projekat** | ekipu koja je danas raspoređena na gradilištu |
   | **Jedinica** | ljude koje poslovna jedinica trenutno zapošljava, s jedinicama ispod nje |
   | **Uloga** | sve naloge s tom ulogom (npr. Poslovođa) |
   | **Svi** | sve koji rade u firmi (traži potvrdu prije slanja) |

3. **Primalac se broji jednom**, i kad je u više izvora. Desno se uživo vidi lista, preko kojeg izvora je ko stigao, i koliko ih može primiti obavještenje.
4. **Osobe bez korisničkog naloga** su označene i ne mogu primiti obavještenje (nema nikog da ga pročita). Ako nikog nema s nalogom, slanje se odbija.
5. **Traži potvrdu:** primalac ne može ništa drugo u aplikaciji dok ne potvrdi da je pročitao.
6. **Pošalji.** Ide kao push na telefon i u zvonce u aplikaciji i panelu.

Poruku jednoj osobi možete poslati i sa njenog kartona (Zaposleni > osoba > **Pošalji poruku**).

## Poslana obavještenja

Lista pokazuje svako slanje: naslov, kome (izvori), kada, ko je poslao, trakom **pročitalo** i trakom **potvrdilo**. Filteri: *Traže potvrdu*, *Nisu svi pročitali*; pretraga po naslovu ili primaocu.

Klik na red otvara detalj: poruku, brojeve (primalaca, pročitalo, potvrdilo, nije pročitalo) i tabelu po osobi:

- **Preko** kojeg izvora je osoba stigla;
- **Isporuka:** *Push* (osoba ima registrovan telefon) ili *Samo zvonce* (vidi ga tek kad otvori aplikaciju ili panel);
- **Pročitao** (kada) i, ako se traži, **Potvrdio** (kada, ili *čeka se potvrda*).

Filteri tabele: *Nisu pročitali*, *Pročitali, nisu potvrdili*, *Potvrdili*.

### Podsjetnik

**Podsjeti (N)** šalje novo obavještenje s oznakom "Podsjetnik" samo onima koji nisu pročitali (ili, kad se traži potvrda, nisu potvrdili). Ista osoba ne dobija podsjetnik češće od jednom u 24 sata. Podsjetnik se ne računa kao novo slanje: brojevi pročitano i potvrđeno ostaju za prvo.

## Šta treba znati

- Slanje se ne može povući. Za "Svi" program traži potvrdu prije slanja.
- Zapis poslanog (šta je rečeno, kome i ko je pročitao) se čuva **12 mjeseci**, pa se briše. Obavještenja u sandučetu osobe ostaju.
- Obavještenja koja šalje sam sistem (odsustva, dokumenti, izvještaji...) nisu u listi poslanih; tamo su samo obavještenja koja ste vi poslali.
- Obavještenja poslana prije ove izmjene ne pokazuju ko ih je pročitao.
