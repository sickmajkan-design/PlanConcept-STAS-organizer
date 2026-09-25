# QA skripte

Skripte kojima je 2026-09-25 urađeno potpuno testiranje API-ja (vidi `docs/QA_IZVJESTAJ_2026-09-25.md`).
Pokreću se protiv **lokalne** instance sa **svježom bazom**, nikad protiv servera koji klijent gleda: prave korisnike, projekte, troškove i brišu ih.

```bash
# 1. API u Development okruženju, npr.
DOTNET_ROLL_FORWARD=Major ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://0.0.0.0:5000 ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=construction_qa;Username=postgres;Password=postgres' Database__ApplyMigrationsOnStartup=true Seed__SuperAdmin__Email=qa.super@construction.local Seed__SuperAdmin__Password='QaPassword123!' JwtSettings__SecretKey='qa-signing-key-at-least-32-characters-long' dotnet run --project src/Construction.API --no-launch-profile

# 2. Fixture: klijenti, gradilišta, zaposleni i korisnici svih uloga (upisuje qa_state.json)
python scripts/qa/qa_setup.py

# 3. Provjere
python scripts/qa/qa_rbac.py                       # svaki GET endpoint × 7 uloga: 5xx i neočekivan pristup
python scripts/qa/flows_time_absence_auth.py       # smjene, odsustva, prijava/odjava/obnova tokena
python scripts/qa/flows_finance_portal_locations.py # finansijska prava, izolacija klijenata, lokacije
python scripts/qa/qa_fuzz.py                       # loš i napadački ulaz na sve POST/PUT
```

`QA_BASE` mijenja adresu (zadano `http://localhost:5000`). Tokeni traju kratko, pa svaka skripta prijavljuje uloge iznova.
Skripte se ne vrte u CI-ju: pišu podatke i traže bazu koju se može baciti.

UI: `src/construction_admin/qa/ui_crawl.mjs` prolazi kroz svaku rutu panela na širini računara i telefona
(`node qa/ui_crawl.mjs <e-pošta> <lozinka>`, panel na `localhost:5173` uperen u ovaj API).
