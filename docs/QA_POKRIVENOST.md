# Tabela pokrivenosti testovima — STAS Organizer

Datum: 2026-11-12. Namjena: interni radni dokument i osnova za izvještaj za Darija.

## Kako je rađeno i koliko se tabeli vjeruje

Automatski popis iz koda, bez pokretanja testova. Pokrivenost znači samo da **neki test dodiruje taj dio**, ne da je dobro ispitan.

- **Backend:** kontroler je pokriven ako neki integracijski test poziva njegovu rutu ili ima test s njegovim imenom. Testovi koji koriste pomoćne klijente mogu biti propušteni, pa su praznine označene "provjeriti".
- **Admin ekrani:** Vitest pokrivenost = neki test spominje komponentu ekrana. E2E = Playwright specifikacija otvara putanju. E2E pogodak za česte putanje (npr. `/employees`) može biti slučajan. Ekrani s `/:id` (detalji, uređivanje) nisu posebno brojani.
- **Mobilno:** broj testova u mapi funkcije (`test/`). Nijedan test ne radi na uređaju ili emulatoru (nema `integration_test`).

Oznake: ✅ ima, ◐ djelimično, ✗ nema.

## Sažetak

| Sloj | Postojeće | Praznine |
|---|---|---|
| Backend (57 kontrolera, 162 fajla s testovima) | većina modula ima testove | Bulletin, NotificationGroups, PublicHolidays, ScheduledReports, VehicleTolls, Postings, Invitations, Presence (provjeriti) |
| Admin Vitest (88 fajlova) | forme zaposlenika, dashboard, dokumenti, bilješke | većina stranica troškova, projekata, vozila, alata, materijala, kupaca |
| Admin E2E (13 specifikacija) | prijava, Knjiga troškova, dokumenti, zaposlenici, telefon, sesija | rad. vrijeme, odsustva, smještaj, projekti, alati, materijali, obavijesti |
| Mobilno (64 testa) | prijava, lokacija, outbox, vrijeme, work items | 9 funkcija bez ijednog testa; nema testa na emulatoru |
| Autorizacija po ulogama | `ApiAuthorizationTests`, skripte `scripts/qa/qa_rbac.py` | nema jedne tabele rute × uloga |

## 1. Backend po modulima

| Modul | Akcija | Integracijski testovi | Ocjena |
|---|---|---|---|
| Auth, Users, Setup | 14, 8, 1 | AuthenticationTests, LoginHardening, MfaFlow, RefreshTokenCookie, UserManagement | ✅ |
| Employees | 13 | EmployeeTests, EmployeeBranch, EmployeeProjectCompany + još 9 | ✅ |
| Projects | 13 | ProjectTests, ProjectMoney + 4 | ✅ |
| Vehicles, VehicleFines | 12, 4 | VehicleTests, VehicleFineTests, VehicleDateReminder | ✅ |
| VehicleFuel, FuelTransactions | 7, 18 | DriverFuelTests, FuelCardTests, DkvImport, DkvHttp | ✅ |
| VehicleTolls | 5 | — | ✗ provjeriti |
| Tools | 19 | ToolAssignment, ToolKit, SiteScope | ✅ |
| Materials | 7 | MaterialStockTests | ◐ |
| Customers, CustomerPortal | 8, 2 | CustomerTaxDetails, CustomerCountry, CustomerPortalTests | ✅ |
| WorkItems | 6 | WorkItemTests | ✅ |
| TimeEntries, SignedTimesheets | 12, 2 | TimeEntryTests, SignedTimesheetTests, CompanyTimeTests | ✅ |
| Absences, Accommodations | 16, 14 | AbsenceReviewAccess, LeaveRules, LeaveEndpoints, AccommodationBulkStay, HousingDoubleEntry | ✅ |
| Book, BookImports, RecurringExpenses | 17, 4, 6 | BookTests, BookAccess, BookScreens, BookImport, RecurringExpense | ✅ |
| Costs, Payouts, Refunds | 148, 5, 3 | CostTests, CompanyCosts, PayoutTests, RefundTests | ◐ (Costs ima 148 akcija) |
| Finance, Ledgers, Invoices | 12, 35, 7 | FinanceAlerts/Breakdown/Grant/Series, Ledger*, InvoiceTests | ✅ |
| Receivables, Payables, Postings | 1, 1, 2 | BookReceivablesTests (djelimično) | ◐ provjeriti |
| Branches | 20 | BranchTests, BranchTree, BranchLogo, BranchBasis + još | ✅ |
| Planning, Assignments | 12, 4 | PlanningTests, ScheduleTests, ForemanPlanning, AssignmentBody | ✅ |
| Attachments, DocumentRequests, Certificates | 12, 9, 6 | Attachment*, DocumentRequest*, CertificateTests | ✅ |
| Notifications, Broadcasts | 15 | NotificationTests, BroadcastTests, NotificationReconciliation | ✅ |
| **NotificationGroups** | 5 | — | ✗ provjeriti |
| **Bulletin** | 10 | — | ✗ provjeriti |
| **PublicHolidays** | 10 | — | ✗ (čeka Darija) |
| **ScheduledReports** | 3 | — | ✗ provjeriti |
| **Invitations** | 3 | OnboardingTests (možda) | ◐ provjeriti |
| **Presence** | 4 | samo unit (`PresenceTrackerTests`) | ◐ |
| WeeklySiteReports | 13 | WeeklyReportReview/LedgerEntry/Reminder | ✅ |
| Dashboard, Overview, Attention | 8, 4, 2 | DashboardLayout/Snapshot, OverviewTests, AttentionTests | ✅ |
| Privacy, Audit, DataQuality, ClientErrors | 1, 1, 2, 1 | ErasureTests, AccountDeletionRequest, AuditTrail, DataQuality, ClientError* | ✅ |
| Exports | 24 | ExportTests, ScheduleExportTests | ◐ |
| Assistant | 2 | samo unit (`Assistant/`) | ◐ |
| Zdravlje i konfiguracija | — | HealthCheck, Cors, ApiVersioning, CorrelationId, Idempotency, Outbox | ✅ |

## 2. Admin ekrani (83 putanje)

Format: Vitest / E2E. Prikazani su samo grupisani ekrani. 83 putanje ukupno, od toga 3 su preusmjerenja.

| Grupa | Ekrani | Vitest | E2E | Ocjena |
|---|---|---|---|---|
| Prijava | Login, ForgotPassword, ResetPassword, AcceptInvitation, ChangePassword, TwoStep | samo Login | samo Login (13 spec.) | ◐ (Forgot/Reset/Invite/MFA ✗) |
| Početna, mapa | Home, LiveMap | Home | — | ◐ (karta ✗) |
| Zaposlenici | lista, forma, hijerarhija, brzi unos, cijene | ✅ | ✅ | ✅ (Hierarchy ✗) |
| Projekti | lista, forma | ✗ | lista (3 spec.) | ◐ |
| Vozila | lista, forma, brzi unos | brzi unos | lista | ◐ |
| Alati, Materijali, Kupci, Radne stavke | liste i forme | ✗ | ✗ | ✗ |
| Radno vrijeme | lista, forma, sažetak | ✗ | lista (1) | ✗/◐ |
| Odsustva | lista | ✗ | telefon (1) | ◐ |
| Smještaj | lista, forma, uvoz | ✗ | ✗ | ✗ |
| Raspored | `/schedule` | ✗ | telefon (1) | ◐ |
| Narudžbe artikala, povrati, računi, tjedni izvještaji | po jedna stranica | ✅ (po 1) | povrati, računi | ◐ |
| Knjiga troškova | 9 ekrana (pregled, unosi, kategorije, potraživanja, isplate, ponavljajući, uvoz, obračun, vodič) | ✗ | ✅ (1-6 spec. po ekranu) | ◐ (nema Vitest, E2E dobar) |
| Troškovi | 12 ekrana (zalihe, vozila, alati, opšti, smještaj, gorivo, DKV, cijene, praznici, finans. unosi, Costs, prihodi) | ✗ | zalihe | ✗ |
| Knjigovodstvo | Ledgers | ✗ | ✅ (2) | ◐ |
| Dokumenti | istek, zahtjevi | ✅ | ✅ | ✅ |
| Obavijesti | lista, nova, poslane, grupe, grupa-forma | nova, poslane | ✗ | ◐ |
| Korisnici | lista, forma | ✗ | lista (2) | ◐ |
| Administracija | audit, kvalitet podataka, prisustvo, organizacija, zakazani izvještaji, postavke naplate | samo kvalitet podataka | ✗ | ✗ |
| Pomoć | HelpPage | ✗ | ✅ (help-search) | ◐ |
| Kupčev portal | `/customer` | ✗ | ✗ | ✗ (čeka v2) |

## 3. Mobilna aplikacija (Flutter)

| Funkcija | Datoteka | Ekrana | Testova | Ocjena |
|---|---|---|---|---|
| auth | 15 | 3 | 4 | ✅ |
| location | 5 | 0 | 4 | ✅ |
| time_entries | 10 | 2 | 3 | ✅ |
| work_items | 7 | 1 | 3 | ✅ |
| outbox | 2 | 0 | 2 | ✅ |
| notifications | 21 | 2 | 2 | ◐ |
| costs | 14 | 2 | 2 | ◐ |
| reminders, shell | 6, 5 | 1, 3 | 2, 2 | ◐ |
| absences, article_orders, attachments, document_requests, housing, materials, my_documents, refunds, weekly_reports | — | — | po 1 | ◐ |
| vehicles | 23 | 2 | 1 | ◐ (veliko, malo testova) |
| **bulletin, company_settings, customers, employees, ledgers, presence, projects, scan, tools** | — | — | **0** | **✗** |

Na uređaju: nema `integration_test`, nema testa emulatora i nijedan tok od prijave do snimanja smjene nije testiran na stvarnoj aplikaciji.

## 4. Najveće praznine po riziku

1. **Autorizacija između poslovnih jedinica i uloga** — nema jedne tabele rute × uloga. Najveći rizik curenja podataka.
2. **Mobilni tok na emulatoru** — ništa nije testirano na uređaju (prijava, smjena, lokacija, offline outbox, skeniranje).
3. **Radno vrijeme, odsustva, smještaj** — središnji tokovi, a nemaju E2E.
4. **Troškovi (12 ekrana)** — nikakav ekranski test.
5. **Zaboravljena lozinka, poziv, MFA ekran** — prijavni tokovi izvan Logina.
6. **Backend: Bulletin, NotificationGroups, ScheduledReports, VehicleTolls** — provjeriti postoje li testovi pod drugim imenom, inače dodati.
7. **CI crven od 13.10.** — regresija koju niko ne vidi ne vrijedi.

## 5. Redoslijed (mobilno na emulatoru za sada)

1. Popraviti crveni CI i potvrditi da se Playwright E2E pokreće (u `ci.yml` nisam vidio poseban E2E korak).
2. Autorizaciona matrica (generisana, pada kad ruta nema odluku).
3. E2E: radno vrijeme, odsustva, smještaj, projekti, alati, materijali, obavijesti.
4. Flutter `integration_test` na emulatoru: prijava → smjena → lokacija → offline → sinkronizacija; zatim skeniranje i dokumenti.
5. Ekranski testovi za troškove i administraciju.
6. Prije LIVE-a: smoke na test serveru, restore iz backupa, opterećenje, ručni prolaz po ulogama na telefonu.
