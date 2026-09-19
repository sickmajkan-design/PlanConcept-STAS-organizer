/**
 * Serbian (Latin, ekavian) translations of the English messages the .NET API
 * returns (ForbiddenAccess/Conflict/NotFound exceptions and FluentValidation).
 *
 * translateServerMessage() returns null for unknown text so the caller can
 * fall back to the original. Dynamic parts (names, dates, numbers) are passed
 * through unchanged.
 */

type Pair = readonly [en: string, sr: string]

// ---------------------------------------------------------------------------
// Exact messages, grouped by feature area
// ---------------------------------------------------------------------------

/** Auth, accounts, passwords, roles. */
const AUTH: Pair[] = [
  ['User is not authenticated.', 'Korisnik nije prijavljen.'],
  ['No signed-in user.', 'Nijedan korisnik nije prijavljen.'],
  ['No signed-in user to record the review against.', 'Nema prijavljenog korisnika kojem bi se pregled pripisao.'],
  ['This account has been deactivated.', 'Ovaj nalog je deaktiviran.'],
  ['Current password is incorrect.', 'Trenutna lozinka nije tačna.'],
  ['Current password is required.', 'Trenutna lozinka je obavezna.'],
  ['New password must be different from the current password.', 'Nova lozinka mora biti različita od trenutne.'],
  ['Password is required.', 'Lozinka je obavezna.'],
  ['Password must be at least 8 characters long.', 'Lozinka mora imati najmanje 8 znakova.'],
  ['Password must not exceed 128 characters.', 'Lozinka ne smije imati više od 128 znakova.'],
  ['Password must contain at least one upper-case letter.', 'Lozinka mora sadržati najmanje jedno veliko slovo.'],
  ['Password must contain at least one lower-case letter.', 'Lozinka mora sadržati najmanje jedno malo slovo.'],
  ['Password must contain at least one digit.', 'Lozinka mora sadržati najmanje jednu cifru.'],
  ['Use the change-password endpoint to set your own password.', 'Za promjenu vlastite lozinke koristite opciju promjene lozinke.'],
  ['Refresh token is required.', 'Token za osvježavanje je obavezan.'],
  ['Reset token is required.', 'Token za resetovanje je obavezan.'],
  ['Email is required.', 'E-pošta je obavezna.'],
  ['Email is not a valid email address.', 'E-pošta nije ispravna adresa.'],
  ['Not a valid email address.', 'Adresa e-pošte nije ispravna.'],
  ['You cannot change your own role.', 'Ne možete promijeniti vlastitu ulogu.'],
  ['You cannot deactivate your own account.', 'Ne možete deaktivirati vlastiti nalog.'],
  ['This is the only active Super Admin. Promote another account first.', 'Ovo je jedini aktivni Super Admin. Prvo unaprijedite drugi nalog.'],
  ['Role is not a known role.', 'Uloga nije poznata.'],
  ['Role is not a valid value.', 'Uloga nije ispravna vrijednost.'],
  ['A customer account may not be linked to an employee.', 'Nalog kupca ne može biti povezan sa zaposlenim.'],
  ['A customer account must be linked to a customer.', 'Nalog kupca mora biti povezan sa kupcem.'],
  ['Only a customer account may be linked to a customer.', 'Sa kupcem se može povezati samo nalog kupca.'],
  ['This account is not linked to a customer.', 'Ovaj nalog nije povezan sa kupcem.'],
  ['Language must be \'sr\' or \'en\'.', 'Jezik mora biti "sr" ili "en".'],
  ['Locale must be \'sr\' or \'en\'.', 'Jezik mora biti "sr" ili "en".'],
  ['Device token is required.', 'Token uređaja je obavezan.'],
  ['Platform is required and must be a valid value.', 'Platforma je obavezna i mora imati ispravnu vrijednost.'],
]

/** Company profile and settings. */
const COMPANY: Pair[] = [
  ['Only a SuperAdmin may change the company logo.', 'Samo Super Admin može promijeniti logotip firme.'],
  ['Only a SuperAdmin may edit the company profile.', 'Samo Super Admin može uređivati profil firme.'],
  ['Only a SuperAdmin may remove the company logo.', 'Samo Super Admin može ukloniti logotip firme.'],
  ['The logo must be an image (jpg, png, webp or heic).', 'Logotip mora biti slika (jpg, png, webp ili heic).'],
  ['Company name is required.', 'Naziv firme je obavezan.'],
  ['A country is required.', 'Država je obavezna.'],
  ['Use the two-letter country code (ISO 3166-1 alpha-2).', 'Koristite dvoslovnu oznaku države (ISO 3166-1 alpha-2).'],
  ['An address is required.', 'Adresa je obavezna.'],
  ['The tracking link must be a full web address (starting with http:// or https://).', 'Link za praćenje mora biti puna web adresa (počinje sa http:// ili https://).'],
  ['Audit retention must be a positive period, or unset to keep everything.', 'Period čuvanja revizije mora biti pozitivan ili ostaviti prazno da se sve čuva.'],
  ['Idempotency retention must be a positive period.', 'Period čuvanja idempotentnosti mora biti pozitivan.'],
  ['Location retention must be a positive period, or unset to keep everything.', 'Period čuvanja lokacija mora biti pozitivan ili ostaviti prazno da se sve čuva.'],
  ['Time-entry coordinate retention must be a positive period, or unset to keep them.', 'Period čuvanja koordinata radnih sati mora biti pozitivan ili ostaviti prazno da se čuvaju.'],
  ['The outbox retention period cannot be negative.', 'Period čuvanja izlaznog reda ne može biti negativan.'],
  ['The refresh-token grace period cannot be negative.', 'Period tolerancije tokena za osvježavanje ne može biti negativan.'],
  ['The reset-token grace period cannot be negative.', 'Period tolerancije tokena za resetovanje ne može biti negativan.'],
  ['The claim lease must be long enough for a send to finish.', 'Rezervacija mora trajati dovoljno dugo da se slanje završi.'],
  ['The assistant is not configured on this installation.', 'Asistent nije podešen na ovoj instalaciji.'],
  ['Unknown widget type.', 'Nepoznat tip widgeta.'],
]

/** Generic validation, paging, dates. */
const GENERIC: Pair[] = [
  ['A name is required.', 'Naziv je obavezan.'],
  ['A title is required.', 'Naslov je obavezan.'],
  ['Title is required.', 'Naslov je obavezan.'],
  ['Body is required.', 'Sadržaj je obavezan.'],
  ['Message is required.', 'Poruka je obavezna.'],
  ['A date is required.', 'Datum je obavezan.'],
  ['A start date is required.', 'Datum početka je obavezan.'],
  ['An end date is required.', 'Datum završetka je obavezan.'],
  ['Start time is required.', 'Vrijeme početka je obavezno.'],
  ['Timestamp is required.', 'Vremenska oznaka je obavezna.'],
  ['Timestamp must not be in the future.', 'Vremenska oznaka ne smije biti u budućnosti.'],
  ['End date must not be before the start date.', 'Datum završetka ne smije biti prije datuma početka.'],
  ['The end of the period must not be before its start.', 'Kraj perioda ne smije biti prije njegovog početka.'],
  ['The end of the range must be after its start.', 'Kraj raspona mora biti poslije njegovog početka.'],
  ['The end of the range must not be before its start.', 'Kraj raspona ne smije biti prije njegovog početka.'],
  ['The end of the window must not be before its start.', 'Kraj intervala ne smije biti prije njegovog početka.'],
  ["'From' must not be after 'To'.", 'Datum "Od" ne smije biti poslije datuma "Do".'],
  ["'EntityName' is required when filtering by 'EntityId'.", 'Naziv entiteta je obavezan kada se filtrira po identifikatoru entiteta.'],
  ['Page number must be at least 1.', 'Broj stranice mora biti najmanje 1.'],
  ['MaxAgeMinutes must be at least 1.', 'Maksimalna starost u minutima mora biti najmanje 1.'],
  ['MaxQuantity must not be negative.', 'Maksimalna količina ne smije biti negativna.'],
  ['Pick a year closer to now.', 'Izaberite godinu bližu današnjoj.'],
  ['The day of the month must be between 1 and 28.', 'Dan u mjesecu mora biti između 1 i 28.'],
  ['The reminder window must be at least 1 day.', 'Period podsjetnika mora biti najmanje 1 dan.'],
  ['The reminder window must be at most 365 days.', 'Period podsjetnika smije biti najviše 365 dana.'],
  ['Latitude must be between -90 and 90.', 'Geografska širina mora biti između -90 i 90.'],
  ['Longitude must be between -180 and 180.', 'Geografska dužina mora biti između -180 i 180.'],
  ['Latitude and longitude must be provided together.', 'Geografska širina i dužina moraju biti navedene zajedno.'],
  ['Latitude and longitude must be supplied together.', 'Geografska širina i dužina moraju biti navedene zajedno.'],
  ['Accuracy must not be negative.', 'Tačnost ne smije biti negativna.'],
  ['At least one ping is required.', 'Potrebna je najmanje jedna lokacija.'],
  ['Give a reason somebody reading the trail later could act on.', 'Navedite razlog koji je razumljiv nekome ko kasnije čita evidenciju.'],
  ['A reason is required; it is recorded in the audit trail.', 'Razlog je obavezan; upisuje se u evidenciju izmjena.'],
  ['Unknown assignment target.', 'Nepoznat cilj dodjele.'],
]

/** Employees, customers, projects, sites. */
const PEOPLE_PROJECTS: Pair[] = [
  ['First name is required.', 'Ime je obavezno.'],
  ['Last name is required.', 'Prezime je obavezno.'],
  ['Employee number is required.', 'Broj zaposlenog je obavezan.'],
  ['Employee is required.', 'Zaposleni je obavezan.'],
  ['Position is required.', 'Radno mjesto je obavezno.'],
  ['Employment date is required.', 'Datum zaposlenja je obavezan.'],
  ['Date of birth must be before the employment date.', 'Datum rođenja mora biti prije datuma zaposlenja.'],
  ['Date of birth must be in the past.', 'Datum rođenja mora biti u prošlosti.'],
  ['Status is not a valid employee status.', 'Status nije ispravan status zaposlenog.'],
  ['Type is not a valid employee type.', 'Tip nije ispravan tip zaposlenog.'],
  ['This employee has no app account to notify.', 'Ovaj zaposleni nema nalog u aplikaciji kojem se može poslati obavještenje.'],
  ['You may only notify someone currently posted to one of your own sites.', 'Obavještenje možete poslati samo osobi koja je trenutno raspoređena na neko vaše gradilište.'],
  ['Customer name is required.', 'Naziv kupca je obavezan.'],
  ['This customer still has projects. Reassign or remove them first.', 'Ovaj kupac još ima projekte. Prvo ih premjestite ili uklonite.'],
  ['Project name is required.', 'Naziv projekta je obavezan.'],
  ['Contract value cannot be negative.', 'Vrijednost ugovora ne može biti negativna.'],
  ['Status is not a valid project status.', 'Status nije ispravan status projekta.'],
  ['A project cannot be its own parent.', 'Projekat ne može biti sam sebi nadređen.'],
  ["A sub-project's parent must itself be a Main project.", 'Nadređeni projekat podprojekta mora biti glavni projekat.'],
  ['This project has its own sub-projects and cannot become a sub-project itself. Reparent or remove them first.', 'Ovaj projekat ima vlastite podprojekte i ne može sam postati podprojekat. Prvo ih premjestite ili uklonite.'],
  ['This project still has sub-projects. Reparent or remove them first.', 'Ovaj projekat još ima podprojekte. Prvo ih premjestite ili uklonite.'],
  ['You may not assign work to someone.', 'Ne možete nikome dodijeliti posao.'],
]

/** Time entries and shifts. */
const TIME_ENTRIES: Pair[] = [
  ['You are already clocked in.', 'Već ste prijavljeni na posao.'],
  ['You are not clocked in.', 'Niste prijavljeni na posao.'],
  ['Only accounts linked to an employee can record work time.', 'Radno vrijeme mogu evidentirati samo nalozi povezani sa zaposlenim.'],
  ['A shift cannot end in the future.', 'Smjena ne može završiti u budućnosti.'],
  ['A shift cannot start in the future.', 'Smjena ne može početi u budućnosti.'],
  ['The shift must end after it starts.', 'Smjena mora završiti poslije početka.'],
  ['Break cannot be longer than a shift.', 'Pauza ne može biti duža od smjene.'],
  ['Break must not be negative.', 'Pauza ne smije biti negativna.'],
  ['The break is as long as the shift, which would leave no time worked.', 'Pauza traje koliko i smjena, pa ne bi ostalo odrađenog vremena.'],
  ['This overlaps a shift the employee already has recorded.', 'Ovo se preklapa sa smjenom koju zaposleni već ima evidentiranu.'],
  ['This entry is already approved.', 'Ovaj unos je već odobren.'],
  ['This entry is approved. Reject it first to make changes.', 'Ovaj unos je odobren. Prvo ga odbijte da biste unijeli izmjene.'],
  ['This entry was changed by someone else just now. Reload it and try again.', 'Ovaj unos je upravo promijenio neko drugi. Učitajte ga ponovo i pokušajte opet.'],
  ['This shift is still running and cannot be reviewed yet.', 'Ova smjena još traje i ne može se pregledati.'],
  ['This shift cannot end before it started. A supervisor has to record the correct end time.', 'Ova smjena ne može završiti prije početka. Nadzornik mora unijeti ispravno vrijeme završetka.'],
  ['You cannot review your own hours.', 'Ne možete pregledati vlastite radne sate.'],
  ['A reason is required when sending an entry back.', 'Razlog je obavezan pri vraćanju unosa.'],
  ['Only accounts linked to an employee can report locations.', 'Lokacije mogu prijavljivati samo nalozi povezani sa zaposlenim.'],
  ['No employee is assigned to this site — pick who to attribute the report to.', 'Nijedan zaposleni nije raspoređen na ovo gradilište. Izaberite kome se izvještaj pripisuje.'],
  ['Hours cannot be negative.', 'Radni sati ne mogu biti negativni.'],
  ['Hours must be greater than zero.', 'Radni sati moraju biti veći od nule.'],
  ['You may not export timesheets.', 'Ne možete izvoziti evidenciju radnih sati.'],
]

/** Absences and leave. */
const ABSENCES: Pair[] = [
  ['The absence cannot end before it starts.', 'Odsustvo ne može završiti prije početka.'],
  ['This employee already has approved time off over those dates.', 'Ovaj zaposleni već ima odobreno odsustvo za te datume.'],
  ['That start date is further ahead than leave can be booked.', 'Taj datum početka je predaleko unaprijed da bi se odsustvo moglo zakazati.'],
  ['A reason is required when refusing leave.', 'Razlog je obavezan pri odbijanju odsustva.'],
  ['This has already been answered. Ask a supervisor to change it.', 'Na ovo je već odgovoreno. Zamolite nadzornika da ga promijeni.'],
  ['This request was withdrawn.', 'Ovaj zahtjev je povučen.'],
  ['This request was changed by someone else just now. Reload it and try again.', 'Ovaj zahtjev je upravo promijenio neko drugi. Učitajte ga ponovo i pokušajte opet.'],
  ['A change is already waiting on confirmation for this absence.', 'Za ovo odsustvo već čeka potvrdu jedna izmjena.'],
  ['There is no pending change on this absence.', 'Za ovo odsustvo nema izmjene na čekanju.'],
  ['Only an already-approved absence can have a change proposed.', 'Izmjena se može predložiti samo za već odobreno odsustvo.'],
  ['Only annual leave supports this two-sided change flow.', 'Ovaj dvostrani postupak izmjene podržava samo godišnji odmor.'],
  ['You may not propose a change to this absence.', 'Ne možete predložiti izmjenu ovog odsustva.'],
  ['You cannot grant your own leave.', 'Ne možete odobriti vlastito odsustvo.'],
  ['You may not grant leave.', 'Ne možete odobravati odsustvo.'],
  ['You may not grant or refuse leave.', 'Ne možete odobravati ni odbijati odsustvo.'],
  ['You may only book your own leave.', 'Možete zakazati samo vlastito odsustvo.'],
  ['This account is not linked to an employee, so it can only book leave for someone else.', 'Ovaj nalog nije povezan sa zaposlenim, pa može zakazivati odsustvo samo za druge.'],
  ['You may not remove this absence.', 'Ne možete ukloniti ovo odsustvo.'],
  ['Choose at least one holiday to import.', 'Izaberite najmanje jedan praznik za uvoz.'],
  ['That date is already on the calendar for that country.', 'Taj datum je već u kalendaru za tu državu.'],
  ['You may not manage the holiday calendar.', 'Ne možete upravljati kalendarom praznika.'],
  ['You may not see the holiday calendar.', 'Ne možete vidjeti kalendar praznika.'],
]

/** Costs, pay, rates, revenue (approval workflow and permissions). */
const COSTS: Pair[] = [
  ['A cost cannot be incurred in the future.', 'Trošak ne može nastati u budućnosti.'],
  ['Pay cannot be recorded for the future.', 'Plata se ne može evidentirati unaprijed.'],
  ['Revenue cannot be recorded for the future.', 'Prihod se ne može evidentirati unaprijed.'],
  ['An amount cannot be negative.', 'Iznos ne može biti negativan.'],
  ['A payment of nothing is not a payment.', 'Uplata od nule nije uplata.'],
  ['A day out has to cost something.', 'Radni dan mora imati neku cijenu.'],
  ['A month has to cost something.', 'Mjesec mora imati neku cijenu.'],
  ['An hour has to cost something.', 'Radni sat mora imati neku cijenu.'],
  ['A daily rate is required.', 'Dnevna cijena je obavezna.'],
  ['An hourly rate is required.', 'Cijena po satu je obavezna.'],
  ['Price must not be negative.', 'Cijena ne smije biti negativna.'],
  ['That amount looks like a typo rather than a daily rate.', 'Taj iznos liči na grešku u kucanju, a ne na dnevnu cijenu.'],
  ['That amount looks like a typo rather than a monthly rate.', 'Taj iznos liči na grešku u kucanju, a ne na mjesečnu cijenu.'],
  ["That rate looks like a typo rather than a day's pay.", 'Ta cijena liči na grešku u kucanju, a ne na dnevnicu.'],
  ['That rate looks like a typo rather than a wage.', 'Ta cijena liči na grešku u kucanju, a ne na platu.'],
  ['Another rate already covers those dates.', 'Druga cijena već pokriva te datume.'],
  ['The rate cannot end before it starts.', 'Cijena ne može isteći prije početka važenja.'],
  ['Only hourly pay carries hours.', 'Radne sate imaju samo plate po satu.'],
  ['Say how many hours were paid for.', 'Navedite koliko je sati plaćeno.'],
  ['This cost is already approved.', 'Ovaj trošak je već odobren.'],
  ['This cost is already waiting for review.', 'Ovaj trošak već čeka pregled.'],
  ['This cost was already sent back. Rejecting it again with a new reason overrides the earlier one — confirm to proceed.', 'Ovaj trošak je već vraćen. Ponovnim odbijanjem s novim razlogom poništava se raniji razlog. Potvrdite za nastavak.'],
  ['This cost was changed by someone else just now. Reload it and try again.', 'Ovaj trošak je upravo promijenio neko drugi. Učitajte ga ponovo i pokušajte opet.'],
  ['You cannot review a cost you recorded yourself.', 'Ne možete pregledati trošak koji ste sami evidentirali.'],
  ['A reason is required when sending a cost back.', 'Razlog je obavezan pri vraćanju troška.'],
  ['You may not review vehicle costs.', 'Ne možete pregledati troškove vozila.'],
  ['You may not see cost reports.', 'Ne možete vidjeti izvještaje o troškovima.'],
  ['You may not see costs.', 'Ne možete vidjeti troškove.'],
  ['You may not record costs.', 'Ne možete evidentirati troškove.'],
  ['You may not correct costs.', 'Ne možete ispravljati troškove.'],
  ['You may not remove recorded costs.', 'Ne možete uklanjati evidentirane troškove.'],
  ['You may not record vehicle costs.', 'Ne možete evidentirati troškove vozila.'],
  ['You may not correct vehicle costs.', 'Ne možete ispravljati troškove vozila.'],
  ['You may not see vehicle costs.', 'Ne možete vidjeti troškove vozila.'],
  ['You may not record tool costs.', 'Ne možete evidentirati troškove alata.'],
  ['You may not correct tool costs.', 'Ne možete ispravljati troškove alata.'],
  ['You may not see tool costs.', 'Ne možete vidjeti troškove alata.'],
  ['You may not record pay.', 'Ne možete evidentirati plate.'],
  ['You may not correct pay entries.', 'Ne možete ispravljati unose plata.'],
  ['You may not remove pay entries.', 'Ne možete uklanjati unose plata.'],
  ['You may not see pay entries.', 'Ne možete vidjeti unose plata.'],
  ['You may not export pay entries.', 'Ne možete izvoziti unose plata.'],
  ['You may not correct pay rates.', 'Ne možete ispravljati cijene plata.'],
  ['You may not remove pay rates.', 'Ne možete uklanjati cijene plata.'],
  ['You may not see pay rates.', 'Ne možete vidjeti cijene plata.'],
  ['You may not set pay rates.', 'Ne možete postavljati cijene plata.'],
  ['You may not correct accommodation rates.', 'Ne možete ispravljati cijene smještaja.'],
  ['You may not remove accommodation rates.', 'Ne možete uklanjati cijene smještaja.'],
  ['You may not see accommodation rates.', 'Ne možete vidjeti cijene smještaja.'],
  ['You may not set accommodation rates.', 'Ne možete postavljati cijene smještaja.'],
  ['You may not correct rental rates.', 'Ne možete ispravljati cijene najma.'],
  ['You may not remove rental rates.', 'Ne možete uklanjati cijene najma.'],
  ['You may not see rental rates.', 'Ne možete vidjeti cijene najma.'],
  ['You may not set rental rates.', 'Ne možete postavljati cijene najma.'],
]

/** Vehicles, tools, rentals, loans, fuel. */
const ASSETS: Pair[] = [
  ['Brand is required.', 'Marka je obavezna.'],
  ['Model is required.', 'Model je obavezan.'],
  ['Registration number is required.', 'Registarski broj je obavezan.'],
  ['Tool name is required.', 'Naziv alata je obavezan.'],
  ['QR code is required.', 'QR kod je obavezan.'],
  ['Status is not a valid tool status.', 'Status nije ispravan status alata.'],
  ['Status is not a valid vehicle status.', 'Status nije ispravan status vozila.'],
  ['Ownership type is not a valid value.', 'Tip vlasništva nije ispravna vrijednost.'],
  ['An owner record is required.', 'Zapis o vlasniku je obavezan.'],
  ['A vehicle that is in service or out of service cannot be assigned.', 'Vozilo koje je u servisu ili van upotrebe ne može se dodijeliti.'],
  ['The tool is already assigned to this employee.', 'Alat je već dodijeljen ovom zaposlenom.'],
  ['The tool is already assigned to this project.', 'Alat je već dodijeljen ovom projektu.'],
  ['The tool is already checked out to someone else.', 'Alat je već zadužen kod nekog drugog.'],
  ['The tool is already checked out to you.', 'Alat je već zadužen kod vas.'],
  ['The tool is currently assigned; unassign it before changing its status.', 'Alat je trenutno dodijeljen. Uklonite dodjelu prije promjene statusa.'],
  ['The tool is not assigned to any employee.', 'Alat nije dodijeljen nijednom zaposlenom.'],
  ['The tool is not assigned to any project.', 'Alat nije dodijeljen nijednom projektu.'],
  ['The tool is not available to rent out.', 'Alat nije dostupan za iznajmljivanje.'],
  ['The vehicle is already assigned to this employee.', 'Vozilo je već dodijeljeno ovom zaposlenom.'],
  ['The vehicle is already assigned to this project.', 'Vozilo je već dodijeljeno ovom projektu.'],
  ['The vehicle is already checked out to someone else.', 'Vozilo je već zaduženo kod nekog drugog.'],
  ['The vehicle is already checked out to you.', 'Vozilo je već zaduženo kod vas.'],
  ['The vehicle is assigned to an employee; unassign it before changing its status.', 'Vozilo je dodijeljeno zaposlenom. Uklonite dodjelu prije promjene statusa.'],
  ['The vehicle is not assigned to any employee.', 'Vozilo nije dodijeljeno nijednom zaposlenom.'],
  ['The vehicle is not assigned to any project.', 'Vozilo nije dodijeljeno nijednom projektu.'],
  ['The vehicle is not available to rent out.', 'Vozilo nije dostupno za iznajmljivanje.'],
  ['This tool is not checked out to you.', 'Ovaj alat nije zadužen kod vas.'],
  ['This vehicle is not checked out to you.', 'Ovo vozilo nije zaduženo kod vas.'],
  ['Only employees can check out tools.', 'Alate mogu zaduživati samo zaposleni.'],
  ['Only employees can check out vehicles.', 'Vozila mogu zaduživati samo zaposleni.'],
  ['Only employees can return tools.', 'Alate mogu razdužiti samo zaposleni.'],
  ['Only employees can return vehicles.', 'Vozila mogu razdužiti samo zaposleni.'],
  ['Say who has the tool.', 'Navedite ko ima alat.'],
  ['Say who has the vehicle.', 'Navedite ko ima vozilo.'],
  ['This loan has already been returned.', 'Ovaj najam je već vraćen.'],
  ['The loan cannot end before it started.', 'Najam ne može završiti prije početka.'],
  ['The loan cannot start after it ended.', 'Najam ne može početi poslije završetka.'],
  ['The posting cannot end before it starts.', 'Raspored ne može završiti prije početka.'],
  ['You may not record tool rentals.', 'Ne možete evidentirati najam alata.'],
  ['You may not correct tool rentals.', 'Ne možete ispravljati najam alata.'],
  ['You may not remove tool rentals.', 'Ne možete uklanjati najam alata.'],
  ['You may not return tool rentals.', 'Ne možete vraćati najam alata.'],
  ['You may not see tool rentals.', 'Ne možete vidjeti najam alata.'],
  ['You may not record vehicle rentals.', 'Ne možete evidentirati najam vozila.'],
  ['You may not correct vehicle rentals.', 'Ne možete ispravljati najam vozila.'],
  ['You may not remove vehicle rentals.', 'Ne možete uklanjati najam vozila.'],
  ['You may not return vehicle rentals.', 'Ne možete vraćati najam vozila.'],
  ['You may not see vehicle rentals.', 'Ne možete vidjeti najam vozila.'],
  ['Fuel type is required and must be a valid value.', 'Vrsta goriva je obavezna i mora imati ispravnu vrijednost.'],
  ['A fill-up of nothing is not a fill-up.', 'Točenje nula litara nije točenje.'],
  ['Only a fill-up has litres.', 'Litre ima samo točenje goriva.'],
  ['Say how many litres went in.', 'Navedite koliko je litara natočeno.'],
  ['Say which provider issued the card.', 'Navedite koji je izdavalac izdao karticu.'],
  ['The card number is required.', 'Broj kartice je obavezan.'],
  ['You may not manage fuel cards.', 'Ne možete upravljati gorivnim karticama.'],
  ['You may not remove fuel cards.', 'Ne možete uklanjati gorivne kartice.'],
  ['You may not see fuel cards.', 'Ne možete vidjeti gorivne kartice.'],
  ['You may not import fuel statements.', 'Ne možete uvoziti izvode goriva.'],
  ['The statement must be an .xlsx or .csv file.', 'Izvod mora biti .xlsx ili .csv datoteka.'],
]

/** Materials and stock. */
const MATERIALS: Pair[] = [
  ['Material name is required.', 'Naziv materijala je obavezan.'],
  ['Unit of measure is required.', 'Jedinica mjere je obavezna.'],
  ['Quantity must not be negative.', 'Količina ne smije biti negativna.'],
  ['Change must not be zero.', 'Promjena ne smije biti nula.'],
  ['Sign must be 1 or -1.', 'Predznak mora biti 1 ili -1.'],
  ['A delivery needs an invoice or receipt number.', 'Isporuka mora imati broj fakture ili računa.'],
  ['A delivery or an issue is a quantity, not a change; use a correction to take stock down.', 'Isporuka ili izdavanje je količina, a ne promjena. Za smanjenje zalihe koristite ispravku.'],
  ['A movement cannot be moved to a different material; delete it and record a new one instead.', 'Kretanje se ne može prebaciti na drugi materijal. Obrišite ga i evidentirajte novo.'],
  ['A movement of nothing is not a movement.', 'Kretanje od nule nije kretanje.'],
  ['Say which site the material went to.', 'Navedite na koje je gradilište materijal otišao.'],
  ['Stock cannot move in the future.', 'Zaliha se ne može mijenjati unaprijed.'],
  ['That correction would put the stock below zero.', 'Ta ispravka bi zalihu spustila ispod nule.'],
  ['That movement would put the stock below zero.', 'To kretanje bi zalihu spustilo ispod nule.'],
  ['The adjustment would make the stock quantity negative.', 'Ta prilagodba bi količinu na zalihi učinila negativnom.'],
  ['Undoing that movement would put the stock below zero.', 'Poništavanjem tog kretanja zaliha bi pala ispod nule.'],
  ['You may not export stock movements.', 'Ne možete izvoziti kretanja zaliha.'],
  ['You may not correct recorded movements.', 'Ne možete ispravljati evidentirana kretanja.'],
  ['You may not remove recorded movements.', 'Ne možete uklanjati evidentirana kretanja.'],
  ['You may not record stock movements.', 'Ne možete evidentirati kretanja zaliha.'],
  ['You may not see stock movements.', 'Ne možete vidjeti kretanja zaliha.'],
]

/** Work items, defects, reports, ledger, scheduled reports. */
const WORK: Pair[] = [
  ['A defect has to be raised against a site.', 'Kvar mora biti prijavljen na neko gradilište.'],
  ['Only a supervisor can sign work off as closed.', 'Samo nadzornik može potpisati rad kao završen.'],
  ['Only an administrator can delete work. Cancel it instead.', 'Rad može obrisati samo administrator. Umjesto toga ga otkažite.'],
  ['This item is finished. Reopen it before making changes.', 'Ova stavka je završena. Ponovo je otvorite prije izmjena.'],
  ['This item was changed by someone else just now. Reload it and try again.', 'Ovu stavku je upravo promijenio neko drugi. Učitajte je ponovo i pokušajte opet.'],
  ['You may not change this item.', 'Ne možete mijenjati ovu stavku.'],
  ['You may not raise work of this kind.', 'Ne možete otvarati rad ove vrste.'],
  ['You may only add photographs to your own work.', 'Fotografije možete dodavati samo uz vlastiti rad.'],
  ['A photograph does not expire.', 'Fotografija ne ističe.'],
  ['A weekday is required for a weekly report.', 'Za sedmični izvještaj je obavezan dan u sedmici.'],
  ['You may only submit a report for a site you are assigned to.', 'Izvještaj možete predati samo za gradilište na koje ste raspoređeni.'],
  ['The report is recorded but its file is missing from storage.', 'Izvještaj je evidentiran, ali njegova datoteka nedostaje u pohrani.'],
  ['This row has already been promoted to a real record.', 'Ovaj red je već pretvoren u pravi zapis.'],
  ['A row may link at most one of employee, vehicle, tool or material.', 'Red može biti povezan najviše sa jednim od: zaposleni, vozilo, alat ili materijal.'],
  ['Sourced columns are computed automatically and cannot be edited.', 'Izvedene kolone se računaju automatski i ne mogu se uređivati.'],
  ["You may not delete another user's scheduled report.", 'Ne možete obrisati tuđi zakazani izvještaj.'],
  ['An earlier message cannot be empty.', 'Ranija poruka ne može biti prazna.'],
]

/** Attachments and files. */
const FILES: Pair[] = [
  ['A file is required — the office cannot bill without proof.', 'Datoteka je obavezna. Kancelarija ne može fakturisati bez dokaza.'],
  ['A file name is required.', 'Naziv datoteke je obavezan.'],
  ['That file type is not accepted.', 'Ova vrsta datoteke nije dozvoljena.'],
  ['The file is empty.', 'Datoteka je prazna.'],
  ['The file is larger than the 10 MB limit.', 'Datoteka je veća od ograničenja od 10 MB.'],
  ['The file is recorded but its contents are missing from storage.', 'Datoteka je evidentirana, ali njen sadržaj nedostaje u pohrani.'],
  ['You may not attach files of this kind to this record.', 'Ne možete priložiti datoteke ove vrste uz ovaj zapis.'],
  ['You may not delete attachments.', 'Ne možete brisati priloge.'],
  ['You may not view the files on this record.', 'Ne možete pregledati datoteke uz ovaj zapis.'],
]

const EXACT_PAIRS: Pair[] = [
  ...AUTH,
  ...COMPANY,
  ...GENERIC,
  ...PEOPLE_PROJECTS,
  ...TIME_ENTRIES,
  ...ABSENCES,
  ...COSTS,
  ...ASSETS,
  ...MATERIALS,
  ...WORK,
  ...FILES,
]

/** English message to Serbian text, for static messages. */
export const SERVER_MESSAGES_EXACT: ReadonlyMap<string, string> = new Map(EXACT_PAIRS)

// ---------------------------------------------------------------------------
// Interpolated messages
// ---------------------------------------------------------------------------

interface Template {
  re: RegExp
  sr: (m: RegExpMatchArray) => string
}

/**
 * Builds a template from an English pattern. `{}` captures any text and
 * `{#}` captures digits. Everything else is matched literally.
 */
function tpl(pattern: string, sr: (...g: string[]) => string): Template {
  const body = pattern
    .split(/(\{#\}|\{\})/)
    .map((part) => {
      if (part === '{}') return '(.+?)'
      if (part === '{#}') return '(\\d+)'
      return part.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
    })
    .join('')
  return { re: new RegExp(`^${body}$`), sr: (m) => sr(...m.slice(1)) }
}

/** Entity names used by "X with id 'Y' was not found." */
const ENTITY_SR: Record<string, string> = {
  Employee: 'Zaposleni',
  Project: 'Projekat',
  Customer: 'Kupac',
  Vehicle: 'Vozilo',
  Tool: 'Alat',
  Material: 'Materijal',
  Absence: 'Odsustvo',
  User: 'Korisnik',
  Attachment: 'Prilog',
  WorkItem: 'Radna stavka',
}

const TEMPLATES: Template[] = [
  // Not found
  tpl("{} with id '{}' was not found.", (e, id) => `Nije pronađeno: ${ENTITY_SR[e] ?? e} sa oznakom '${id}'.`),
  tpl("No tool found for QR code '{}'.", (q) => `Nije pronađen alat za QR kod '${q}'.`),
  tpl("No vehicle found for QR code '{}'.", (q) => `Nije pronađeno vozilo za QR kod '${q}'.`),
  tpl("No location has been reported yet for employee '{}'.", (id) => `Za zaposlenog '${id}' još nije prijavljena nijedna lokacija.`),
  tpl("Employee '{}' is not posted to project '{}'.", (e, p) => `Zaposleni '${e}' nije raspoređen na projekat '${p}'.`),

  // Uniqueness conflicts
  tpl("A group named '{}' already exists.", (n) => `Grupa pod nazivom '${n}' već postoji.`),
  tpl("An account for '{}' already exists.", (e) => `Nalog za '${e}' već postoji.`),
  tpl('{} already has an account.', (n) => `${n} već ima nalog.`),
  tpl("Card number '{}' is already in use.", (v) => `Broj kartice '${v}' je već u upotrebi.`),
  tpl("Employee number '{}' is already in use.", (v) => `Broj zaposlenog '${v}' je već u upotrebi.`),
  tpl("QR code '{}' is already in use.", (v) => `QR kod '${v}' je već u upotrebi.`),
  tpl("Registration number '{}' is already in use.", (v) => `Registarski broj '${v}' je već u upotrebi.`),
  tpl("Serial number '{}' is already in use.", (v) => `Serijski broj '${v}' je već u upotrebi.`),
  tpl("VIN '{}' is already in use.", (v) => `VIN '${v}' je već u upotrebi.`),

  // Roles and permissions
  tpl('A {} may not administer a {} account.', (c, t) => `Uloga ${c} ne može upravljati nalogom uloge ${t}.`),
  tpl('A {} may not grant the {} role.', (c, r) => `Uloga ${c} ne može dodijeliti ulogu ${r}.`),
  tpl('A {} item cannot be moved to {}.', (f, t) => `Stavka sa statusom ${f} ne može se prebaciti u status ${t}.`),

  // Assets
  tpl("The tool cannot be assigned while its status is '{}'.", (s) => `Alat se ne može dodijeliti dok je njegov status '${s}'.`),
  tpl("The vehicle cannot be assigned while its status is '{}'.", (s) => `Vozilo se ne može dodijeliti dok je njegov status '${s}'.`),
  tpl("The vehicle cannot be checked out while its status is '{}'.", (s) => `Vozilo se ne može zadužiti dok je njegov status '${s}'.`),

  // Costs and approval
  tpl('This cost was sent back: "{}". Approving it now overrides that decision — confirm to proceed.', (n) => `Ovaj trošak je vraćen: "${n}". Odobravanjem se poništava ta odluka. Potvrdite za nastavak.`),
  tpl('This entry was sent back: "{}". Approving it now overrides that decision — confirm to proceed.', (n) => `Ovaj unos je vraćen: "${n}". Odobravanjem se poništava ta odluka. Potvrdite za nastavak.`),
  tpl('A cost cannot be recorded more than {#} days back.', (n) => `Trošak se ne može evidentirati više od ${n} dana unazad.`),
  tpl('A loan cannot be recorded more than {#} days back.', (n) => `Najam se ne može evidentirati više od ${n} dana unazad.`),
  tpl('A movement cannot be recorded more than {#} days back.', (n) => `Kretanje se ne može evidentirati više od ${n} dana unazad.`),
  tpl('Pay cannot be recorded more than {#} days back.', (n) => `Plata se ne može evidentirati više od ${n} dana unazad.`),
  tpl('A loan cannot start more than {#} days from now.', (n) => `Najam ne može početi više od ${n} dana unaprijed.`),

  // Time entries and absences
  tpl('A shift cannot be longer than {#} hours.', (n) => `Smjena ne može trajati duže od ${n} sati.`),
  tpl('A shift cannot be recorded more than {#} days back.', (n) => `Smjena se ne može evidentirati više od ${n} dana unazad.`),
  tpl('The time this shift ended is either in the future or more than {#} hours ago. A supervisor has to record it.', (n) => `Vrijeme završetka ove smjene je u budućnosti ili prije više od ${n} sati. Mora ga evidentirati nadzornik.`),
  tpl('The time this shift started is either in the future or more than {#} hours ago. A supervisor has to record it.', (n) => `Vrijeme početka ove smjene je u budućnosti ili prije više od ${n} sati. Mora ga evidentirati nadzornik.`),
  tpl('An absence cannot be recorded more than {#} days back.', (n) => `Odsustvo se ne može evidentirati više od ${n} dana unazad.`),
  tpl('An absence longer than {#} days is a change of employment, not leave.', (n) => `Odsustvo duže od ${n} dana je promjena zaposlenja, a ne odsustvo.`),

  // Documents and files
  tpl('This document must be kept until {} and cannot be deleted before then.', (d) => `Ovaj dokument se mora čuvati do ${d} i ne može se obrisati prije toga.`),
  tpl('The file is larger than the {#} MB limit.', (n) => `Datoteka je veća od ograničenja od ${n} MB.`),
  tpl('That file type is not accepted. Allowed: {}', (a) => `Ova vrsta datoteke nije dozvoljena. Dozvoljeno: ${a}`),
  tpl('A deadline more than {#} years out is probably a typo.', (n) => `Rok udaljen više od ${n} godina vjerovatno je greška u kucanju.`),

  // Paging, ranges, limits
  tpl('Page size must be between 1 and {#}.', (n) => `Veličina stranice mora biti između 1 i ${n}.`),
  tpl('SortBy must be one of: {}', (l) => `Sortiranje mora biti po jednom od: ${l}`),
  tpl('Language must be one of: {}', (l) => `Jezik mora biti jedan od: ${l}`),
  tpl('The period must not exceed {#} days.', (n) => `Period ne smije biti duži od ${n} dana.`),
  tpl('The range must not exceed {#} days.', (n) => `Raspon ne smije biti duži od ${n} dana.`),
  tpl('The window must be between 0 and {#} days.', (n) => `Interval mora biti između 0 i ${n} dana.`),
  tpl('The window must not exceed {#} days.', (n) => `Interval ne smije biti duži od ${n} dana.`),
  tpl('A batch may contain at most {#} pings.', (n) => `Skup smije sadržati najviše ${n} lokacija.`),
  tpl('A conversation may carry at most {#} earlier messages.', (n) => `Razgovor smije sadržati najviše ${n} ranijih poruka.`),
  tpl('An earlier message cannot exceed {#} characters.', (n) => `Ranija poruka ne smije imati više od ${n} znakova.`),
]

/**
 * Returns the Serbian text for a known API message, or null if unknown
 * (the caller should then show the original).
 */
export function translateServerMessage(message: string): string | null {
  const text = message.trim()
  if (!text) return null

  const exact = SERVER_MESSAGES_EXACT.get(text)
  if (exact !== undefined) return exact

  for (const { re, sr } of TEMPLATES) {
    const m = text.match(re)
    if (m) return sr(m)
  }
  return null
}
