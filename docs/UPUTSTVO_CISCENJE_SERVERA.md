# Uputstvo: brisanje dokumenata i čišćenje servera

Za koga: za osobu koja vodi administraciju i za osobu koja održava server. Koraci 1 i 2 se rade u panelu, koraci 3 i 4 na serveru.

## Prije svega: šta se desi kad se dokument obriše

- **Obrisan dokument odmah gubi datoteku na serveru.** To je provjereno 13. 11. 2026. na test serveru: 191 dokument, 199 datoteka, svaka datoteka ima svoj zapis, i nijedna obrisana datoteka nije ostala na disku.
- **Dokument pod rokom čuvanja** (polje Čuvati do, u budućnosti) se ne može obrisati, nikako i ni grupno. Ako ga treba obrisati, prvo se rok čuvanja izmijeni na dokumentu.
- **Kopije za slučaj kvara** (noćni backup) čuvaju obrisani dokument još do **14 dana**. To je namjerno: kopija služi da se vrati stanje poslije greške. Poslije 14 dana nestaje i iz kopija.

## 1. Brisanje dokumenata koji više ne trebaju (Admin i Super Admin)

1. Otvorite **Dokumenti** na lijevoj traci.
2. Izaberite period **Sve** i uključite **Prikaži i dokumente bez roka isteka**, da vidite sve, ne samo ono što uskoro ističe.
3. Suzite listu: pretraga (naslov, ime datoteke, vlasnik, napomena, oznaka), pa filteri **Zapis**, **Vrsta**, **Oznaka**. Sortiranje je klikom na naslov stupca.
4. Označite dokumente kvačicom u prvom stupcu. Kvačica u zaglavlju označi sve prikazano. Ono što se ne može izabrati je sivo: dokument pod rokom čuvanja, i dokumenti knjige troškova i potpisanih satnica (njih brišete na njihovim ekranima).
5. Iznad liste piše **Izabrano: N (veličina)**. Pritisnite **Obriši izabrano zauvijek** i potvrdite.
6. Panel javi koliko je obrisano i koliko je prostora oslobođeno. Ako je neki dokument ostao zbog roka čuvanja, to piše u poruci.

Pazite: briše se samo ono što je **prikazano i označeno**. Dokument koji je filterom sakriven se ne briše, čak i ako je ranije bio označen.

## 2. Čišćenje onoga što je ostalo na serveru (samo Super Admin)

Otvorite **Trag izmjena**, kartica **Prostor na serveru**, dugme **Očisti**. Prozor pokazuje tri vrste ostataka, koliko ih ima i koliko zauzimaju. Svaka se briše posebno, uz potvrdu:

| Vrsta | Šta je | Kad je brisati |
|---|---|---|
| **Datoteke bez zapisa** | Datoteke na disku na koje nijedan zapis ne pokazuje (ostatak neuspjelog brisanja ili vraćanja iz stare kopije). Starije od 24 sata. | Kad god broj nije nula. Bez gubitka: nikome ne trebaju. |
| **Dokumenti obrisanih zapisa** | Dokumenti zaposlenika, projekata, vozila, alata, zadataka i smještaja koji su obrisani, a dokumenti su ostali. | Kad ste sigurni da vam ti dokumenti ne trebaju (npr. bivši radnik, a rok čuvanja je istekao). Dokumenti pod rokom čuvanja se preskaču. |
| **Tragovi obrisanih dokumenata** | Redovi u bazi koji pokazuju da je dokument nekad postojao. Datoteke već nema. | Povremeno, ako ne trebate istoriju brisanja. Zapis ko je šta obrisao ostaje u dnevniku servera. |

Poslije brisanja panel kaže koliko je datoteka i zapisa obrisano i koliko je prostora oslobođeno.

## 3. Čišćenje Docker slika na serveru

Svako izdanje aplikacije na serveru ostavi staru verziju kao sliku bez oznake. Na test serveru ih je 13. 11. bilo 388 i zauzimale su 16,8 GB od ukupno 38 GB. To je ono što najviše puni disk, mnogo više od dokumenata (52 MB).

Panel ovo ne može, jer API namjerno nema pristup Dockeru. Radi se na serveru:

```
ssh -i ~/.ssh/stas_deploy_ed25519 root@2.28.73.236

df -h /                                    # prije: koliko je slobodno
/opt/construction/scripts/prune-docker.sh  # briše samo slike bez oznake i stari build cache
df -h /                                    # poslije
```

Skripta nikad ne dira pokrenute kontejnere, slike s oznakom ni volumene, pa dokumenti, baza i backup ostaju netaknuti. Ako skripte nema na serveru, kopirajte `scripts/prune-docker.sh` u `/opt/construction/scripts/` i dajte joj pravo izvršavanja (`chmod +x`).

**Da se radi samo od sebe, svake noći u 03:30:**

```
printf '30 3 * * * root /opt/construction/scripts/prune-docker.sh >> /var/log/construction-prune.log 2>&1\n' > /etc/cron.d/construction-prune
chmod 644 /etc/cron.d/construction-prune
```

Rezultat svake noći piše u `/var/log/construction-prune.log` (koliko je oslobođeno i koliko je ostalo).

**Izborno, ako je disk i dalje pun:** sistemski dnevnici (`/var/log`) su na test serveru zauzimali 2 GB. Smanjuju se naredbom `journalctl --vacuum-size=300M`.

## 4. Provjera da je sve u redu

1. Na kartici **Prostor na serveru** (Trag izmjena) traka i procenat slobodnog su bolji, a oznaka je **U redu**.
2. Adresa `https://2.28.73.236.nip.io/health/dependencies` pokazuje `"status":"Healthy"` (a ne `Degraded` ili `Unhealthy`).
3. Super Admin na početnoj stranici više ne vidi liniju **Malo prostora na serveru**.
4. Prijava, otvaranje dokumenta i pregled rade kao ranije.

Granice upozorenja: **malo** je ispod 20% ili 8 GB slobodno, **gotovo puno** ispod 10% ili 3 GB. Obavještenje stiže Super Adminu najviše jednom u 20 sati, dok je prostora malo.

## 5. Redovno održavanje

- **Svake sedmice** (1 minut): pogledajte karticu Prostor na serveru. Žuto ili crveno znači da treba djelovati.
- **Poslije svakog izdanja:** pokrenite skriptu za Docker slike (ili ostavite noćnu).
- **Jednom mjesečno:** otvorite Očisti i obrišite datoteke bez zapisa, a po potrebi i dokumente obrisanih zapisa.
- **Jednom godišnje:** prođite Dokumente po godini i obrišite ono što zakon više ne traži da se čuva.

## 6. Ako je disk skoro pun (hitno)

1. Odmah pokrenite skriptu za Docker slike (korak 3). To obično oslobodi najviše.
2. Ako treba još: korak 1 (grupno brisanje velikih, starih dokumenata sortiranih po veličini) i korak 2.
3. Ako ni to nije dovoljno: povećajte disk kod davaoca servera, pa `df -h /` da vidite novu veličinu.
4. Ne brišite ništa ručno u `/var/lib/docker/volumes`: tamo su baza, dokumenti i backup.

## Napomene

- Sve ovo je isprobano na lokalnom okruženju i test serveru. Prije puštanja na pravi (LIVE) server isto treba proći na kopiji.
- Brisanje je trajno. Prije većeg grupnog brisanja uvijek provjerite izabranu listu i veličinu u poruci.
- Pravila i tehnički detalji su u `docs/SECURITY.md`, odjeljak *Cleaning the server*.
