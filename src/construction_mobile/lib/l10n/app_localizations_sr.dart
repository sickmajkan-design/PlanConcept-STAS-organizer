// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Serbian (`sr`).
class AppLocalizationsSr extends AppLocalizations {
  AppLocalizationsSr([String locale = 'sr']) : super(locale);

  @override
  String get appName => 'Construction Organizer';

  @override
  String get commonCancel => 'Otkaži';

  @override
  String get commonDelete => 'Obriši';

  @override
  String get commonEdit => 'Izmeni';

  @override
  String get commonAdd => 'Dodaj';

  @override
  String get commonStatus => 'Status';

  @override
  String get vehicleFormAddTitle => 'Dodaj vozilo';

  @override
  String get vehicleFormEditTitle => 'Izmeni vozilo';

  @override
  String get vehicleFormBrand => 'Marka';

  @override
  String get vehicleFormModel => 'Model';

  @override
  String get vehicleFormAdded => 'Vozilo dodato.';

  @override
  String get vehicleFormSaved => 'Vozilo izmenjeno.';

  @override
  String get vehicleDeleteTitle => 'Obrisati ovo vozilo?';

  @override
  String vehicleDeleteBody(String name) {
    return '$name će biti uklonjeno. Ovo se ne može poništiti.';
  }

  @override
  String get commonRetry => 'Pokušaj ponovo';

  @override
  String get commonLoadMore => 'Učitaj još';

  @override
  String commonTotalCount(int count) {
    return 'Ukupno $count';
  }

  @override
  String commonCountOfTotal(int shown, int total) {
    return '$shown od $total';
  }

  @override
  String get commonSignIn => 'Prijavi se';

  @override
  String get commonSignOut => 'Odjavi se';

  @override
  String get commonSignOutQuestion => 'Odjaviti se?';

  @override
  String get commonSignOutBody =>
      'Za korišćenje aplikacije moraćete ponovo da se prijavite.';

  @override
  String get commonNotSet => '—';

  @override
  String get commonDetails => 'Detalji';

  @override
  String get commonAssignment => 'Zaduženje';

  @override
  String get commonContact => 'Kontakt';

  @override
  String get commonEmployment => 'Zaposlenje';

  @override
  String get commonAccount => 'Nalog';

  @override
  String get commonResources => 'Resursi';

  @override
  String get commonCompany => 'Firma';

  @override
  String get commonEmployee => 'Zaposleni';

  @override
  String get commonMaterial => 'Materijal';

  @override
  String get commonTool => 'Alat';

  @override
  String get commonVehicle => 'Vozilo';

  @override
  String get commonProject => 'Projekat';

  @override
  String get commonAlerts => 'Obaveštenja';

  @override
  String get authSignInSubtitle => 'Prijavite se na svoj radni nalog';

  @override
  String get authEmail => 'E-mail';

  @override
  String get authPassword => 'Lozinka';

  @override
  String get authCurrentPassword => 'Trenutna lozinka';

  @override
  String get authNewPassword => 'Nova lozinka';

  @override
  String get authConfirmPassword => 'Potvrdi novu lozinku';

  @override
  String get authShowPassword => 'Prikaži lozinku';

  @override
  String get authHidePassword => 'Sakrij lozinku';

  @override
  String get authForgotPassword => 'Zaboravljena lozinka?';

  @override
  String get authResetPassword => 'Resetovanje lozinke';

  @override
  String get authResetIntro =>
      'Unesite e-mail adresu svog radnog naloga. Ako nalog postoji, poslaćemo link za izbor nove lozinke.';

  @override
  String get authResetSent =>
      'Ako ta adresa pripada nalogu, link za resetovanje je na putu.';

  @override
  String get authSendResetLink => 'Pošalji link';

  @override
  String get authSendAgain => 'Pošalji ponovo';

  @override
  String get authChangePassword => 'Promena lozinke';

  @override
  String get authPasswordChanged => 'Lozinka je promenjena';

  @override
  String get authPasswordChangedBody =>
      'Vaša lozinka je izmenjena. Radi bezbednosti, svi vaši prijavljeni uređaji su odjavljeni.';

  @override
  String get authSignInAgain => 'Prijavi se ponovo';

  @override
  String get authSessionExpired => 'Sesija je istekla. Prijavite se ponovo.';

  @override
  String get validationEmailRequired => 'E-mail je obavezan.';

  @override
  String get validationEmailInvalid => 'Unesite ispravnu e-mail adresu.';

  @override
  String get validationPasswordRequired => 'Lozinka je obavezna.';

  @override
  String get validationPasswordMinLength =>
      'Lozinka mora imati najmanje 8 karaktera.';

  @override
  String validationFieldRequired(String field) {
    return '$field je obavezno polje.';
  }

  @override
  String get validationPasswordUpper => 'Lozinka mora sadržati veliko slovo.';

  @override
  String get validationPasswordLower => 'Lozinka mora sadržati malo slovo.';

  @override
  String get validationPasswordDigit => 'Lozinka mora sadržati cifru.';

  @override
  String get validationPasswordsDiffer => 'Lozinke se ne poklapaju.';

  @override
  String get validationConfirmPassword => 'Potvrdite novu lozinku.';

  @override
  String get errorNoConnection =>
      'Nema veze sa serverom. Proverite internet i pokušajte ponovo.';

  @override
  String get errorTimeout => 'Server predugo ne odgovara. Pokušajte ponovo.';

  @override
  String get errorCancelled => 'Zahtev je otkazan.';

  @override
  String get errorCertificate => 'Sertifikat servera nije mogao biti provaren.';

  @override
  String get errorServer => 'Došlo je do greške na serveru. Pokušajte kasnije.';

  @override
  String get errorNotFound => 'Traženi zapis nije pronađen.';

  @override
  String get errorForbidden => 'Nemate dozvolu za ovu radnju.';

  @override
  String get errorBadRequest => 'Zahtev je odbijen. Proverite unete podatke.';

  @override
  String get errorConflict => 'Radnja je u sukobu sa trenutnim podacima.';

  @override
  String get errorUnknown => 'Došlo je do greške. Pokušajte ponovo.';

  @override
  String get navHome => 'Početna';

  @override
  String get navEmployees => 'Zaposleni';

  @override
  String get navProjects => 'Projekti';

  @override
  String get navVehicles => 'Vozila';

  @override
  String get navTools => 'Alat';

  @override
  String get navMaterials => 'Materijal';

  @override
  String get navNotifications => 'Obaveštenja';

  @override
  String get employeesSearchHint => 'Ime, broj, radno mesto…';

  @override
  String get employeesEmpty => 'Nema zaposlenih za vašu pretragu.';

  @override
  String get employeeNumber => 'Matični broj';

  @override
  String get employeePosition => 'Radno mesto';

  @override
  String get employeePhone => 'Telefon';

  @override
  String get employeeEmail => 'E-mail';

  @override
  String get employeeAddress => 'Adresa';

  @override
  String get employeeDateOfBirth => 'Datum rođenja';

  @override
  String get employeeEmployedSince => 'Zaposlen od';

  @override
  String get employeeAppAccount => 'Nalog u aplikaciji';

  @override
  String get employeeNoProjects => 'Nije dodeljen ni na jedan projekat';

  @override
  String get projectsSearchHint => 'Naziv, klijent, adresa…';

  @override
  String get projectsEmpty => 'Nema projekata za vašu pretragu.';

  @override
  String get projectClient => 'Klijent';

  @override
  String projectSubOf(String name) {
    return 'Deo projekta $name';
  }

  @override
  String get projectAddress => 'Adresa';

  @override
  String get projectStartDate => 'Datum početka';

  @override
  String get projectEndDate => 'Datum završetka';

  @override
  String get projectCoordinates => 'Koordinate';

  @override
  String get projectCrewEmpty => 'Još niko nije dodeljen';

  @override
  String projectAssignedCount(int count) {
    return 'Dodeljeno: $count';
  }

  @override
  String get projectCrewTitle => 'Posada';

  @override
  String projectCrewCount(int count) {
    return 'Posada ($count)';
  }

  @override
  String employeeProjectsCount(int count) {
    return 'Projekti ($count)';
  }

  @override
  String projectAssignedOn(String date) {
    return 'Dodeljen $date';
  }

  @override
  String projectMemberSubtitle(String position, String number) {
    return '$position · $number';
  }

  @override
  String get vehiclesSearchHint => 'Marka, model, registracija…';

  @override
  String get vehiclesEmpty => 'Nema vozila za vašu pretragu.';

  @override
  String get vehicleRegistration => 'Registarski broj';

  @override
  String get vehicleOwnershipType => 'Vlasništvo';

  @override
  String get vehicleFuelType => 'Vrsta goriva';

  @override
  String get vehicleUnassigned => 'Nije zaduženo ni na koga';

  @override
  String get toolsSearchHint => 'Naziv, kategorija, serijski broj…';

  @override
  String get toolsEmpty => 'Nema alata za vašu pretragu.';

  @override
  String get toolSerialNumber => 'Serijski broj';

  @override
  String get toolFormAddTitle => 'Dodaj alat';

  @override
  String get toolFormEditTitle => 'Izmeni alat';

  @override
  String get toolFormName => 'Naziv';

  @override
  String get toolFormCategory => 'Kategorija';

  @override
  String get toolFormAdded => 'Alat dodat.';

  @override
  String get toolFormSaved => 'Alat izmenjen.';

  @override
  String get toolDeleteTitle => 'Obrisati ovaj alat?';

  @override
  String toolDeleteBody(String name) {
    return '$name će biti uklonjen. Ovo se ne može poništiti.';
  }

  @override
  String get toolQrCode => 'QR kod';

  @override
  String get toolUncategorised => 'Bez kategorije';

  @override
  String get toolNotHeld => 'Nije ni na koga zaduženo';

  @override
  String get toolNotOnProject => 'Nije ni na jednom projektu';

  @override
  String get toolLookUp => 'Pronađi alat';

  @override
  String get toolLookUpAction => 'Pronađi';

  @override
  String get toolLookUpByQr => 'Pronađi po QR kodu';

  @override
  String get toolByQrCode => 'Po QR kodu';

  @override
  String get vehicleQrCode => 'QR kod';

  @override
  String get vehicleOwnershipTypeOwned => 'U vlasništvu';

  @override
  String get vehicleOwnershipTypeRented => 'Iznajmljeno';

  @override
  String get vehicleRentalProvider => 'Iznajmljeno od';

  @override
  String get vehicleRentalMonthlyAmount => 'Mesečna rata';

  @override
  String vehicleLoanedOutTo(String name) {
    return 'Izdato na korišćenje: $name';
  }

  @override
  String get rentalRatesTitle => 'Renta / lizing';

  @override
  String get rentalRatesAdd => 'Dodaj cenu';

  @override
  String get rentalRatesEditTitle => 'Izmeni cenu';

  @override
  String get rentalRatesEmpty => 'Još nema unesene rente ili lizinga.';

  @override
  String get rentalRatesProvider => 'Davalac (opciono)';

  @override
  String get rentalRatesNote => 'Napomena (opciono)';

  @override
  String get rentalRatesMonthlyAmount => 'Mesečni iznos';

  @override
  String get rentalRatesStartDate => 'Od';

  @override
  String get rentalRatesEndDate => 'Do (opciono, otvoreno ako je prazno)';

  @override
  String get rentalRatesOpenEnded => 'Otvoreno';

  @override
  String get rentalRatesDeleteTitle => 'Obrisati ovu cenu?';

  @override
  String get rentalRatesDeleteBody => 'Ovo se ne može poništiti.';

  @override
  String get rentalRatesSaved => 'Cena rente sačuvana.';

  @override
  String get rentalOutTitle => 'Izdavanje u zakup';

  @override
  String get rentalOutAdd => 'Izdaj u zakup';

  @override
  String get rentalOutEditTitle => 'Izmeni izdavanje';

  @override
  String get rentalOutEmpty => 'Trenutno nije izdato nikome.';

  @override
  String get rentalOutRenterName => 'Ime/naziv zakupca';

  @override
  String get rentalOutCustomer => 'Povezan klijent (opciono)';

  @override
  String get rentalOutNoCustomer => 'Bez povezanog klijenta';

  @override
  String get rentalOutDailyRate => 'Dnevna cena';

  @override
  String get rentalOutStartDate => 'Od';

  @override
  String get rentalOutStillOut => 'Još nije vraćeno';

  @override
  String get rentalOutReturn => 'Označi kao vraćeno';

  @override
  String get rentalOutReturned => 'Označeno kao vraćeno.';

  @override
  String get rentalOutDeleteTitle => 'Obrisati ovo izdavanje?';

  @override
  String get rentalOutDeleteBody => 'Ovo se ne može poništiti.';

  @override
  String get rentalOutSaved => 'Izdavanje sačuvano.';

  @override
  String get scanTitle => 'Skeniraj ili pronađi';

  @override
  String get scanHint =>
      'Skenirajte QR nalepnicu na alatu ili vozilu, ili unesite kod ispod.';

  @override
  String get scanAction => 'Skeniraj QR kod';

  @override
  String get scanToggleFlash => 'Uključi/isključi lampu';

  @override
  String get scanCodeLabel => 'QR kod';

  @override
  String get scanToolFound => 'Alat';

  @override
  String get scanVehicleFound => 'Vozilo';

  @override
  String get scanCheckOutToMe => 'Zaduži na mene';

  @override
  String get scanReturn => 'Razduži';

  @override
  String get scanCheckedOutToYou => 'Zaduženo na vas';

  @override
  String scanCheckedOutToOther(String name) {
    return 'Zaduženo na $name';
  }

  @override
  String get scanNotCheckedOut => 'Trenutno nije zaduženo';

  @override
  String get scanCheckOutSuccess => 'Zaduženo na vas.';

  @override
  String get scanReturnSuccess => 'Razduženo.';

  @override
  String get scanCameraPermissionDenied =>
      'Potrebna je dozvola za kameru radi skeniranja QR koda.';

  @override
  String get scanTransferToEmployee => 'Prebaci na radnika';

  @override
  String get scanTransferToProject => 'Prebaci na gradilište';

  @override
  String get scanPickEmployee => 'Radnik';

  @override
  String get scanPickProject => 'Gradilište';

  @override
  String get scanTransferConfirm => 'Prebaci';

  @override
  String scanTransferSuccess(String name) {
    return 'Prebačeno na $name.';
  }

  @override
  String scanTransferProjectSuccess(String name) {
    return 'Postavljeno na $name.';
  }

  @override
  String toolCategoryLine(String category) {
    return 'Kategorija: $category';
  }

  @override
  String toolSerialLine(String serial) {
    return 'Serijski broj: $serial';
  }

  @override
  String get materialsSearchHint => 'Naziv, magacin…';

  @override
  String get materialsEmpty => 'Nema materijala za vašu pretragu.';

  @override
  String get materialStock => 'Stanje';

  @override
  String get materialWarehouse => 'Magacin';

  @override
  String get materialWarehouseStock => 'Zalihe u magacinu';

  @override
  String get materialWarehouseOnly => 'Samo zalihe u magacinu';

  @override
  String get materialFormAddTitle => 'Dodaj materijal';

  @override
  String get materialFormEditTitle => 'Izmeni materijal';

  @override
  String get materialFormName => 'Naziv';

  @override
  String get materialFormUnit => 'Jedinica mere';

  @override
  String get materialFormUnitPrice => 'Cena po jedinici (opciono)';

  @override
  String get materialFormAdded => 'Materijal dodat.';

  @override
  String get materialFormSaved => 'Materijal izmenjen.';

  @override
  String get materialDeleteTitle => 'Obrisati ovaj materijal?';

  @override
  String materialDeleteBody(String name) {
    return '$name će biti uklonjen. Ovo se ne može poništiti.';
  }

  @override
  String get materialWarehouseNote =>
      'Zalihe u magacinu, nisu vezane za projekat';

  @override
  String get materialLastUpdated => 'Poslednja izmena';

  @override
  String get materialNoAssignment => 'Nije zaduženo';

  @override
  String get employeeFormAddTitle => 'Dodaj zaposlenog';

  @override
  String get employeeFormEditTitle => 'Izmeni zaposlenog';

  @override
  String get employeeFormNumber => 'Broj zaposlenog';

  @override
  String get employeeFormFirstName => 'Ime';

  @override
  String get employeeFormLastName => 'Prezime';

  @override
  String get employeeFormEmploymentDate => 'Datum zaposlenja';

  @override
  String get employeeFormAdded => 'Zaposleni dodat.';

  @override
  String get employeeFormSaved => 'Zaposleni izmenjen.';

  @override
  String get employeeDeleteTitle => 'Obrisati ovog zaposlenog?';

  @override
  String employeeDeleteBody(String name) {
    return '$name će biti uklonjen. Ovo se ne može poništiti.';
  }

  @override
  String get employeeType => 'Tip';

  @override
  String get employeeTypeEmployee => 'Zaposleni';

  @override
  String get employeeTypeSubcontractor => 'Kooperant';

  @override
  String get projectFormAddTitle => 'Dodaj projekat';

  @override
  String get projectFormEditTitle => 'Izmeni projekat';

  @override
  String get projectFormName => 'Naziv projekta';

  @override
  String get projectFormDescription => 'Opis (opciono)';

  @override
  String get projectFormParentProject => 'Nadređeni projekat (opciono)';

  @override
  String get projectFormNoParent => 'Bez nadređenog — glavni projekat';

  @override
  String get projectFormNoCustomer => 'Bez klijenta';

  @override
  String get projectFormCountryCode => 'Šifra države (npr. BA)';

  @override
  String get projectFormShiftStartTime => 'Početak smene (opciono)';

  @override
  String get projectFormContractValue => 'Vrednost ugovora (opciono)';

  @override
  String get projectFormLatitude => 'Geografska širina';

  @override
  String get projectFormLongitude => 'Geografska dužina';

  @override
  String get projectFormAdded => 'Projekat dodat.';

  @override
  String get projectFormSaved => 'Projekat izmenjen.';

  @override
  String get projectDeleteTitle => 'Obrisati ovaj projekat?';

  @override
  String projectDeleteBody(String name) {
    return '$name će biti uklonjen. Ovo se ne može poništiti.';
  }

  @override
  String get notificationsEmpty => 'Još nema obaveštenja.';

  @override
  String get notificationsUnreadEmpty => 'Nema nepročitanih.';

  @override
  String get notificationsUnread => 'Nepročitano';

  @override
  String notificationsUnreadCount(int count) {
    return 'Nepročitano ($count)';
  }

  @override
  String get notificationsMarkAllRead => 'Označi sve pročitanim';

  @override
  String notificationsAcknowledgedOn(String date) {
    return 'Potvrđeno $date';
  }

  @override
  String get notificationsOpenRelated => 'Otvori';

  @override
  String get notificationProjectAssignedTitle =>
      'Dodijeljeni ste na novo gradilište';

  @override
  String notificationProjectAssignedBody(String projectName) {
    return 'Dodijeljeni ste na gradilište \"$projectName\".';
  }

  @override
  String notificationProjectAssignedBodyWithAddress(
    String projectName,
    String address,
  ) {
    return 'Dodijeljeni ste na gradilište \"$projectName\", na adresi $address.';
  }

  @override
  String notificationProjectAssignedBodyFull(
    String projectName,
    String address,
    String shiftStartTime,
  ) {
    return 'Dodijeljeni ste na gradilište \"$projectName\", na adresi $address. Smjena počinje u $shiftStartTime.';
  }

  @override
  String get notificationEmployeeAssignedTitle =>
      'Radnik dodijeljen na vaše gradilište';

  @override
  String notificationEmployeeAssignedBody(
    String employeeName,
    String projectName,
  ) {
    return '$employeeName je dodijeljen(a) na gradilište \"$projectName\".';
  }

  @override
  String get notificationVehicleAssignedTitle => 'Vozilo dodijeljeno';

  @override
  String notificationVehicleAssignedBody(
    String brand,
    String model,
    String registration,
  ) {
    return 'Vozilo $brand $model ($registration) vam je dodijeljeno.';
  }

  @override
  String get notificationToolAssignedTitle => 'Alat dodijeljen';

  @override
  String notificationToolAssignedBody(String toolName) {
    return 'Alat \"$toolName\" vam je dodijeljen.';
  }

  @override
  String get notificationDocumentExpiringTitle => 'Dokument uskoro ističe';

  @override
  String get notificationDocumentExpiredTitle => 'Dokument je istekao';

  @override
  String notificationDocumentExpiringBody(String fileName, String expiresAt) {
    return '$fileName ($expiresAt)';
  }

  @override
  String notificationDocumentExpiringBodyWithOwner(
    String fileName,
    String ownerName,
    String expiresAt,
  ) {
    return '$fileName — $ownerName ($expiresAt)';
  }

  @override
  String get notificationTaskAssignedTitle => 'Zadatak vam je dodijeljen';

  @override
  String get notificationDefectAssignedTitle => 'Kvar vam je dodijeljen';

  @override
  String notificationWorkItemAssignedBodyWithDueDate(
    String title,
    String dueDate,
  ) {
    return '$title — rok $dueDate';
  }

  @override
  String get notificationWorkItemOverdueTitle => 'Kasni';

  @override
  String get notificationWorkItemDueSoonTitle => 'Uskoro dospijeva';

  @override
  String notificationWorkItemDueBody(String title, String dueDate) {
    return '$title ($dueDate)';
  }

  @override
  String get notificationShiftAutoClosedTitle => 'Smjena automatski zatvorena';

  @override
  String notificationShiftAutoClosedBody(String shiftDate) {
    return 'Niste odjavili smjenu $shiftDate, pa je automatski zatvorena i čeka pregled.';
  }

  @override
  String get notificationAbsenceEditProposedTitle =>
      'Predložena izmjena odobrenog odsustva';

  @override
  String notificationAbsenceEditProposedBody(String startDate, String endDate) {
    return '$startDate–$endDate — potvrdite ili odbijte.';
  }

  @override
  String get notificationAbsenceEditConfirmedTitle =>
      'Izmjena odsustva potvrđena';

  @override
  String notificationAbsenceEditConfirmedBody(
    String startDate,
    String endDate,
  ) {
    return '$startDate–$endDate';
  }

  @override
  String get notificationAbsenceEditDeclinedTitle =>
      'Izmjena odsustva odbijena';

  @override
  String get notificationAbsenceEditDeclinedBody =>
      'Druga strana je odbila predloženu izmjenu.';

  @override
  String get notificationWeeklyReportDueTitle =>
      'Sedmični izvještaj još nije predat';

  @override
  String notificationWeeklyReportDueBody(
    String projectName,
    String isoWeek,
    String isoYear,
  ) {
    return '$projectName — KW$isoWeek/$isoYear';
  }

  @override
  String get notificationEmployeeClockedInTitle => 'Prijava na posao';

  @override
  String notificationEmployeeClockedInBody(
    String employeeName,
    String projectName,
  ) {
    return '$employeeName se prijavio(la) na gradilištu $projectName.';
  }

  @override
  String get notificationEmployeeClockedOutTitle => 'Odjava s posla';

  @override
  String notificationEmployeeClockedOutBody(
    String employeeName,
    String projectName,
    String hours,
    String minutes,
  ) {
    return '$employeeName se odjavio(la) sa gradilišta $projectName nakon ${hours}h ${minutes}min.';
  }

  @override
  String get notificationUnassignedClockInTitle =>
      'Prijava na nedodijeljeno gradilište';

  @override
  String notificationUnassignedClockInBody(
    String employeeName,
    String projectName,
  ) {
    return '$employeeName se prijavio(la) na gradilištu $projectName, iako trenutno nije tamo raspoređen(a).';
  }

  @override
  String get notificationDefectReportedTitle => 'Prijavljen novi kvar';

  @override
  String notificationDefectReportedBody(String reporterName, String title) {
    return '$reporterName je prijavio(la): $title';
  }

  @override
  String get notificationAbsenceRequestedTitle => 'Zahtjev za odsustvo';

  @override
  String notificationAbsenceRequestedBody(
    String employeeName,
    String startDate,
    String endDate,
  ) {
    return '$employeeName je zatražio(la) odsustvo, od $startDate do $endDate.';
  }

  @override
  String get notificationDocumentRetentionEndedTitle =>
      'Period čuvanja dokumenta je istekao';

  @override
  String notificationDocumentRetentionEndedBody(
    String fileName,
    String retainUntil,
  ) {
    return '$fileName više ne mora biti sačuvan (rok čuvanja je bio do $retainUntil). Obrišite ga sami ako više nije potreban.';
  }

  @override
  String notificationDocumentRetentionEndedBodyWithOwner(
    String fileName,
    String ownerName,
    String retainUntil,
  ) {
    return '$fileName — $ownerName više ne mora biti sačuvan (rok čuvanja je bio do $retainUntil). Obrišite ga sami ako više nije potreban.';
  }

  @override
  String get notificationTypeDirectMessage => 'Direktna poruka';

  @override
  String get notificationTypeDocumentExpiring => 'Dokument ističe';

  @override
  String get notificationTypeTaskAssigned => 'Zadatak dodijeljen';

  @override
  String get notificationTypeDefectAssigned => 'Kvar dodijeljen';

  @override
  String get notificationTypeWorkItemDue => 'Rok za posao';

  @override
  String get notificationTypeShiftAutoClosed => 'Smjena automatski zatvorena';

  @override
  String get notificationTypeBulletinPosted => 'Objava na oglasnoj tabli';

  @override
  String get notificationTypeAbsenceEditProposed => 'Izmjena odsustva';

  @override
  String get notificationTypeWeeklyReportDue => 'Sedmični izvještaj';

  @override
  String get notificationTypeEmployeeClockedIn => 'Prijava na posao';

  @override
  String get notificationTypeEmployeeClockedOut => 'Odjava s posla';

  @override
  String get notificationTypeUnassignedProjectClockIn =>
      'Prijava bez rasporeda';

  @override
  String get notificationTypeDefectReported => 'Prijavljen kvar';

  @override
  String get notificationTypeAbsenceRequested => 'Zahtjev za odsustvo';

  @override
  String get notificationTypeDocumentRetentionEnded =>
      'Isteklo čuvanje dokumenta';

  @override
  String get announceTitle => 'Pošalji obaveštenje';

  @override
  String get announceSubject => 'Naslov';

  @override
  String get announceMessage => 'Poruka';

  @override
  String get announceAudienceRole => 'Uloga';

  @override
  String get announceEveryRole => 'Svaka uloga';

  @override
  String get announceAudienceProject => 'Projekat';

  @override
  String get announceEveryProject => 'Svaki projekat';

  @override
  String get announceAudienceGroup => 'Grupa';

  @override
  String get announceEveryGroup => 'Svaka grupa';

  @override
  String get announceRequiresAcknowledgment =>
      'Zahtevaj potvrdu pre nego što primaoci mogu da rade bilo šta drugo';

  @override
  String get announceHint =>
      'Obaveštenje na telefonu se ne može povući — publika je jedina stvar koju vredi proveriti dvaput.';

  @override
  String get announceSend => 'Pošalji';

  @override
  String announceSent(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: 'Poslato $count osoba.',
      few: 'Poslato $count osobama.',
      one: 'Poslato jednoj osobi.',
      zero: 'Poslato nikome — niko nije odgovarao uslovima.',
    );
    return '$_temp0';
  }

  @override
  String get notificationsDisabled =>
      'Obaveštenja su isključena za ovu aplikaciju.';

  @override
  String get notificationsNotConfigured =>
      'Push obaveštenja nisu podešena u ovoj verziji.';

  @override
  String get notificationsNotConfiguredBody =>
      'Slanje push obaveštenja nije podešeno u ovoj verziji. Obaveštenja se i dalje prikazuju ovde.';

  @override
  String get notificationsBlockedBody =>
      'Push obaveštenja su isključena za ovu aplikaciju. I dalje ih možete čitati ovde.';

  @override
  String get notificationsOpenSettings => 'Otvori podešavanja aplikacije';

  @override
  String get notificationsTokenFailed => 'Nije moguće dobiti token uređaja.';

  @override
  String get notificationsFirebaseFailed => 'Firebase poruke nisu uspele.';

  @override
  String get locationSharingOn => 'Deljenje lokacije je uključeno';

  @override
  String get locationSharingOnBody =>
      'Vaša pozicija se šalje kancelariji svakog minuta dok ste prijavljeni.';

  @override
  String get locationStarting => 'Pokretanje deljenja lokacije…';

  @override
  String get locationProblem => 'Problem sa deljenjem lokacije';

  @override
  String get locationNotShared => 'Vaša pozicija se ne deli sa kancelarijom.';

  @override
  String get locationServicesOff => 'Usluge lokacije su isključene';

  @override
  String get locationPermissionDenied => 'Dozvola za lokaciju nije data';

  @override
  String get locationPermissionBlocked => 'Dozvola za lokaciju je blokirana';

  @override
  String get locationAllow => 'Dozvoli lokaciju';

  @override
  String get locationOpenSettings => 'Otvori podešavanja lokacije';

  @override
  String get locationNoFix => 'Još nema GPS signala.';

  @override
  String get locationReadFailed => 'Nije moguće očitati lokaciju uređaja.';

  @override
  String locationQueued(String reason) {
    return 'Na čekanju — $reason';
  }

  @override
  String get locationServiceNotificationTitle => 'Deljenje lokacije je u toku';

  @override
  String get locationServiceNotificationBody =>
      'Kancelarija vidi na kom ste gradilištu. Odjavite se da prekinete.';

  @override
  String get locationServiceChannelName => 'Deljenje lokacije';

  @override
  String locationPending(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count očitavanja čeka slanje',
      few: '$count očitavanja čekaju slanje',
      one: '$count očitavanje čeka slanje',
      zero: 'Sve je poslato',
    );
    return '$_temp0';
  }

  @override
  String locationLastSent(String when) {
    return 'Poslednji put poslato $when.';
  }

  @override
  String get locationOpenAppSettings => 'Otvori podešavanja aplikacije';

  @override
  String get roleSuperAdmin => 'Super administrator';

  @override
  String get roleAdmin => 'Administrator';

  @override
  String get roleProjectManager => 'Rukovodilac projekta';

  @override
  String get roleForeman => 'Poslovođa';

  @override
  String get roleWorker => 'Radnik';

  @override
  String get employeeStatusActive => 'Aktivan';

  @override
  String get employeeStatusOnLeave => 'Na odsustvu';

  @override
  String get employeeStatusSuspended => 'Suspendovan';

  @override
  String get employeeStatusTerminated => 'Raskinut ugovor';

  @override
  String get projectStatusPlanned => 'Planiran';

  @override
  String get projectStatusActive => 'Aktivan';

  @override
  String get projectStatusOnHold => 'Pauziran';

  @override
  String get projectStatusCompleted => 'Završen';

  @override
  String get projectStatusCancelled => 'Otkazan';

  @override
  String get vehicleStatusAvailable => 'Slobodno';

  @override
  String get vehicleStatusAssigned => 'Zaduženo';

  @override
  String get vehicleStatusInService => 'Na servisu';

  @override
  String get vehicleStatusOutOfService => 'Van upotrebe';

  @override
  String get vehicleStatusRentedOut => 'Izdato u zakup';

  @override
  String get toolStatusAvailable => 'Slobodan';

  @override
  String get toolStatusAssigned => 'Zadužen';

  @override
  String get toolStatusUnderRepair => 'Na popravci';

  @override
  String get toolStatusLost => 'Izgubljen';

  @override
  String get toolStatusRetired => 'Rashodovan';

  @override
  String get toolStatusRentedOut => 'Izdato u zakup';

  @override
  String get fuelPetrol => 'Benzin';

  @override
  String get fuelDiesel => 'Dizel';

  @override
  String get fuelElectric => 'Električno';

  @override
  String get fuelHybrid => 'Hibrid';

  @override
  String get fuelLpg => 'LPG';

  @override
  String get notificationTypeEmployeeAssigned => 'Zaposleni dodeljen';

  @override
  String get notificationTypeProjectAssigned => 'Projekat dodeljen';

  @override
  String get notificationTypeToolAssigned => 'Alat zadužen';

  @override
  String get notificationTypeVehicleAssigned => 'Vozilo zaduženo';

  @override
  String get notificationTypeAnnouncement => 'Obaveštenje';

  @override
  String get settingsLanguage => 'Jezik';

  @override
  String get settingsLanguageSerbian => 'Srpski';

  @override
  String get settingsLanguageEnglish => 'English';

  @override
  String get navTimeEntries => 'Radno vreme';

  @override
  String get shiftTitle => 'Moje radno vreme';

  @override
  String get shiftRunning => 'Prijavljeni ste na smenu';

  @override
  String get shiftOff => 'Niste prijavljeni na smenu';

  @override
  String shiftSince(String time) {
    return 'Od $time';
  }

  @override
  String shiftElapsed(int hours, int minutes) {
    return '$hours h $minutes min';
  }

  @override
  String get shiftClockIn => 'Prijavi se na smenu';

  @override
  String get shiftClockOut => 'Odjavi se sa smene';

  @override
  String get shiftClockOutTitle => 'Završetak smene';

  @override
  String get shiftBreakLabel => 'Neplaćena pauza (minuta)';

  @override
  String get shiftBreakHint => 'Ostavite 0 ako je niste koristili.';

  @override
  String get shiftProject => 'Gradilište';

  @override
  String get shiftNoProject => 'Bez gradilišta';

  @override
  String get shiftWorkType => 'Vrsta rada';

  @override
  String get shiftConfirm => 'Potvrdi';

  @override
  String get shiftHistory => 'Poslednji unosi';

  @override
  String get shiftHistoryEmpty => 'Još nema evidentiranih sati.';

  @override
  String get shiftWorked => 'Odrađeno';

  @override
  String get shiftBreak => 'Pauza';

  @override
  String shiftBreakMinutes(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count minuta',
      few: '$count minuta',
      one: '$count minut',
      zero: 'Bez pauze',
    );
    return '$_temp0';
  }

  @override
  String shiftSentBack(String reason) {
    return 'Vraćeno: $reason';
  }

  @override
  String get shiftNotAnEmployee =>
      'Ovaj nalog nije povezan sa zaposlenim, pa ne može da evidentira radno vreme.';

  @override
  String get shiftAutoClosed => 'Automatski zatvorena';

  @override
  String get shiftLocationMismatch => 'Prijava van lokacije gradilišta';

  @override
  String get shiftTimeMismatch => 'Prijava van očekivanog vremena smene';

  @override
  String get timeEntryStatusInProgress => 'U toku';

  @override
  String get timeEntryStatusSubmitted => 'Čeka pregled';

  @override
  String get timeEntryStatusApproved => 'Odobreno';

  @override
  String get timeEntryStatusRejected => 'Vraćeno na doradu';

  @override
  String get workTypeRegular => 'Redovan rad';

  @override
  String get workTypeOvertime => 'Prekovremeni';

  @override
  String get workTypeWeekend => 'Vikend';

  @override
  String get workTypePublicHoliday => 'Praznik';

  @override
  String get workTypeTravel => 'Putovanje';

  @override
  String get attachmentsTitle => 'Dokumenti';

  @override
  String get attachmentsEmpty => 'Na ovom zapisu nema dokumenata.';

  @override
  String get attachmentsExpired => 'Isteklo';

  @override
  String attachmentsExpiresOn(String date) {
    return 'Važi do $date';
  }

  @override
  String get attachmentsOpenFailed => 'Fajl nije moguće otvoriti.';

  @override
  String get attachmentsAddPhoto => 'Dodaj fotografiju';

  @override
  String get attachmentsTakePhoto => 'Slikaj';

  @override
  String get attachmentsFromGallery => 'Izaberi iz galerije';

  @override
  String get attachmentsPhotoNote => 'Napomena (opciono)';

  @override
  String get attachmentsUploading => 'Otpremanje…';

  @override
  String get attachmentsUploaded => 'Fotografija je dodata.';

  @override
  String attachmentsTooLarge(int limit) {
    return 'Fotografija je veća od ograničenja od $limit MB.';
  }

  @override
  String get attachmentsNotAnImage =>
      'Moguće je priložiti samo dokument ili sliku.';

  @override
  String get attachmentsAddDocument => 'Dodaj dokument';

  @override
  String get attachmentsPickFile => 'Izaberi fajl';

  @override
  String get attachmentsChangeFile => 'Izaberi drugi fajl';

  @override
  String get attachmentsCategory => 'Kategorija';

  @override
  String get attachmentsDescription => 'Opis (opciono)';

  @override
  String get attachmentsExpiryDate => 'Datum isteka (opciono)';

  @override
  String get attachmentsRetainUntil => 'Čuvati do (opciono)';

  @override
  String get attachmentsSaved => 'Dokument dodat.';

  @override
  String get attachmentsDeleteTitle => 'Obrisati ovaj dokument?';

  @override
  String attachmentsDeleteBody(String name) {
    return '$name će biti uklonjen. Ovo se ne može poništiti.';
  }

  @override
  String attachmentsRetainedCannotDelete(String date) {
    return 'Čuva se do $date — još se ne može obrisati.';
  }

  @override
  String get attachmentsOpen => 'Otvori';

  @override
  String get attachmentsOpeningExternally => 'Otvaranje u drugoj aplikaciji…';

  @override
  String get attachmentsOpenExternalFailed =>
      'Nijedna aplikacija na telefonu ne može otvoriti ovaj tip fajla.';

  @override
  String get attachmentCategoryContract => 'Ugovor';

  @override
  String get attachmentCategoryCertificate => 'Sertifikat';

  @override
  String get attachmentCategoryMedicalCheck => 'Lekarski pregled';

  @override
  String get attachmentCategoryLicence => 'Licenca';

  @override
  String get attachmentCategoryInsurance => 'Osiguranje';

  @override
  String get attachmentCategorySiteDocument => 'Gradilišna dokumentacija';

  @override
  String get attachmentCategoryPhoto => 'Fotografija';

  @override
  String get attachmentCategoryOther => 'Ostalo';

  @override
  String get navWorkItems => 'Moji zadaci';

  @override
  String get workItemsEmpty => 'Nemate ništa na spisku.';

  @override
  String get workItemsIncludeFinished => 'Prikaži i završeno';

  @override
  String workItemsDue(String date) {
    return 'Rok $date';
  }

  @override
  String get workItemsOverdue => 'Kasni';

  @override
  String get workItemsNoDueDate => 'Bez roka';

  @override
  String get workItemsNoProject => 'Bez gradilišta';

  @override
  String get workItemsReportDefect => 'Prijavi nedostatak';

  @override
  String get workItemsDefectTitle => 'Šta nije u redu';

  @override
  String get workItemsDefectDescription => 'Detalji (opciono)';

  @override
  String get workItemsDefectSend => 'Prijavi';

  @override
  String get workItemsDefectSent => 'Nedostatak je prijavljen.';

  @override
  String get workItemsDefectNeedsTitle => 'Opišite problem u par reči.';

  @override
  String workItemsPhotoCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count fotografija',
      few: '$count fotografije',
      one: '$count fotografija',
      zero: 'Bez fotografija',
    );
    return '$_temp0';
  }

  @override
  String get workItemKindTask => 'Zadatak';

  @override
  String get workItemKindDefect => 'Nedostatak';

  @override
  String get workItemStatusOpen => 'Otvoreno';

  @override
  String get workItemStatusInProgress => 'U toku';

  @override
  String get workItemStatusResolved => 'Urađeno, za proveru';

  @override
  String get workItemStatusClosed => 'Zatvoreno';

  @override
  String get workItemStatusCancelled => 'Otkazano';

  @override
  String get workItemPriorityLow => 'Nizak';

  @override
  String get workItemPriorityNormal => 'Normalan';

  @override
  String get workItemPriorityHigh => 'Visok';

  @override
  String get workItemPriorityUrgent => 'Hitno';

  @override
  String get navSchedule => 'Moj raspored';

  @override
  String get navAbsences => 'Odsustva';

  @override
  String get navWeeklyReports => 'Sedmični izveštaji';

  @override
  String get navBulletin => 'Oglasna ploča';

  @override
  String get bulletinTitle => 'Oglasna ploča';

  @override
  String get bulletinEmpty => 'Trenutno nema oglasa.';

  @override
  String bulletinPostedBy(String name) {
    return 'Objavio/la $name';
  }

  @override
  String get scheduleTitle => 'Moj raspored';

  @override
  String get scheduleEmpty => 'Nema rasporeda za naredne dve nedelje.';

  @override
  String get scheduleToday => 'Danas';

  @override
  String get scheduleTomorrow => 'Sutra';

  @override
  String get scheduleContinues => 'Traje dalje';

  @override
  String get scheduleUpcoming => 'Naredne dve nedelje';

  @override
  String scheduleDateRange(String from, String to) {
    return '$from – $to';
  }

  @override
  String get scheduleAway => 'Odsutan';

  @override
  String get scheduleOnSite => 'Na gradilištu';

  @override
  String get absencesTitle => 'Odsustva';

  @override
  String get absencesEmpty => 'Nisi tražio nijedno odsustvo.';

  @override
  String get absencesPendingOnly => 'Čeka odgovor';

  @override
  String get absencesRequest => 'Zatraži odsustvo';

  @override
  String get absencesType => 'Vrsta';

  @override
  String get absencesStartDate => 'Od';

  @override
  String get absencesEndDate => 'Do';

  @override
  String get absencesReason => 'Razlog (opciono)';

  @override
  String get absencesSend => 'Pošalji zahtev';

  @override
  String get absencesSent => 'Zahtev je poslat.';

  @override
  String get absencesPickDates => 'Izaberi prvi i poslednji dan odsustva.';

  @override
  String get absencesEndsBeforeStart => 'Poslednji dan ne može biti pre prvog.';

  @override
  String absencesDayCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count dana',
      few: '$count dana',
      one: '$count dan',
    );
    return '$_temp0';
  }

  @override
  String get absencesWithdraw => 'Povuci';

  @override
  String get absencesWithdrawTitle => 'Povući ovaj zahtev?';

  @override
  String get absencesWithdrawBody =>
      'Nadređeni više neće videti da tražiš te dane.';

  @override
  String get absencesWithdrawn => 'Zahtev je povučen.';

  @override
  String get absencesGrantedLocked =>
      'Odobreno odsustvo može da otkaže samo nadređeni.';

  @override
  String absencesAnsweredBy(String name) {
    return 'Odgovorio $name';
  }

  @override
  String get absenceTypeAnnualLeave => 'Godišnji odmor';

  @override
  String get absenceTypeSickLeave => 'Bolovanje';

  @override
  String get absenceTypeUnpaidLeave => 'Neplaćeno odsustvo';

  @override
  String get absenceTypePaidSpecialLeave => 'Plaćeno odsustvo';

  @override
  String get absenceTypeTraining => 'Obuka';

  @override
  String get absenceTypeOther => 'Ostalo';

  @override
  String get absenceStatusRequested => 'Na čekanju';

  @override
  String get absenceStatusApproved => 'Odobreno';

  @override
  String get absenceStatusRejected => 'Odbijeno';

  @override
  String get absenceStatusCancelled => 'Povučeno';

  @override
  String get weeklyReportsTitle => 'Sedmični izveštaji sa gradilišta';

  @override
  String get weeklyReportsDescription =>
      'Pošaljite potpisane sate, Aufmaß ili drugi dokaz o radu za gradilište na koje ste raspoređeni.';

  @override
  String get weeklyReportsSubmit => 'Pošalji izveštaj';

  @override
  String get weeklyReportsProject => 'Gradilište';

  @override
  String get weeklyReportsProjectsFailed =>
      'Nije moguće učitati vaša gradilišta.';

  @override
  String get weeklyReportsIsoYear => 'Godina';

  @override
  String get weeklyReportsIsoWeek => 'Sedmica';

  @override
  String get weeklyReportsType => 'Vrsta';

  @override
  String get weeklyReportsHours => 'Sati';

  @override
  String get weeklyReportsNote => 'Napomena (opciono)';

  @override
  String get weeklyReportsAttachFile => 'Dodaj fajl';

  @override
  String get weeklyReportsFileTooLarge => 'Fajl je veći od dozvoljenih 20 MB.';

  @override
  String get weeklyReportsSend => 'Pošalji';

  @override
  String get notifyEmployeeAction => 'Obavesti';

  @override
  String get notifyEmployeeTitle => 'Pošalji poruku';

  @override
  String get notifyEmployeeSubject => 'Naslov';

  @override
  String get notifyEmployeeMessage => 'Poruka';

  @override
  String get notifyEmployeeRequireAck => 'Zahtevaj potvrdu';

  @override
  String get notifyEmployeeRequireAckHint =>
      'Neće moći ništa drugo da radi u aplikaciji dok ne potvrdi da je video/videla ovo.';

  @override
  String get notifyEmployeeSend => 'Pošalji';

  @override
  String get notifyEmployeeSent => 'Poruka poslata.';

  @override
  String get teamTodayAction => 'Ekipa danas';

  @override
  String get teamTodayTitle => 'Ekipa danas';

  @override
  String get teamTodayEmpty => 'Još niko nije prijavio dolazak danas.';

  @override
  String teamTodayStillWorking(String time) {
    return 'Od $time — još uvek radi';
  }

  @override
  String get weeklyReportsSent => 'Izveštaj poslat.';

  @override
  String get weeklyReportTypeSignedHours => 'Potpisani sati';

  @override
  String get weeklyReportTypeAufmass => 'Aufmaß';

  @override
  String get weeklyReportTypeOther => 'Ostalo';

  @override
  String get weeklyReportStatusSubmitted => 'Čeka pregled';

  @override
  String get weeklyReportStatusProcessed => 'Obrađeno';

  @override
  String get weeklyReportsHistoryTitle => 'Do sada poslato';

  @override
  String get weeklyReportsHistoryEmpty => 'Još ništa nije poslato.';

  @override
  String weeklyReportsIsoWeekLabel(int isoWeek, int isoYear) {
    return 'KW$isoWeek/$isoYear';
  }

  @override
  String weeklyReportsProcessedOn(String date) {
    return 'Obrađeno $date';
  }

  @override
  String get navVehicleExpenses => 'Troškovi vozila';

  @override
  String get navToolExpenses => 'Troškovi alata';

  @override
  String get vehicleExpensesTitle => 'Troškovi vozila';

  @override
  String get vehicleExpensesEmpty => 'Još nema evidentiranih troškova.';

  @override
  String get vehicleExpensesFuelOnly => 'Samo točenja';

  @override
  String get vehicleExpensesRecord => 'Evidentiraj trošak';

  @override
  String get vehicleExpensesVehicle => 'Vozilo';

  @override
  String get vehicleExpensesKind => 'Vrsta';

  @override
  String get vehicleExpensesAmount => 'Iznos';

  @override
  String get vehicleExpensesLitres => 'Litara';

  @override
  String get vehicleExpensesOdometer => 'Kilometraža';

  @override
  String get vehicleExpensesSupplier => 'Gde';

  @override
  String get vehicleExpensesNote => 'Napomena';

  @override
  String get vehicleExpensesSend => 'Evidentiraj';

  @override
  String get vehicleExpensesSent => 'Trošak je evidentiran.';

  @override
  String get vehicleExpensesNeedsVehicle => 'Izaberi vozilo.';

  @override
  String get vehicleExpensesNeedsAmount => 'Upiši koliko je koštalo.';

  @override
  String get vehicleExpensesFuelNeedsLitres => 'Upiši koliko je litara sipano.';

  @override
  String get vehicleExpensesOdometerHint =>
      'Nije obavezno, ali dva stanja daju potrošnju.';

  @override
  String vehicleExpensesPerLitre(String price) {
    return '$price po litru';
  }

  @override
  String get vehicleExpenseKindFuel => 'Gorivo';

  @override
  String get vehicleExpenseKindService => 'Servis';

  @override
  String get vehicleExpenseKindRepair => 'Popravka';

  @override
  String get vehicleExpenseKindInsurance => 'Osiguranje';

  @override
  String get vehicleExpenseKindRegistration => 'Registracija';

  @override
  String get vehicleExpenseKindOther => 'Ostalo';

  @override
  String get toolExpensesTitle => 'Troškovi alata';

  @override
  String get toolExpensesEmpty => 'Još nema evidentiranih troškova.';

  @override
  String get toolExpensesRecord => 'Evidentiraj trošak';

  @override
  String get toolExpensesTool => 'Alat';

  @override
  String get toolExpensesKind => 'Vrsta';

  @override
  String get toolExpensesAmount => 'Iznos';

  @override
  String get toolExpensesSupplier => 'Gde';

  @override
  String get toolExpensesNote => 'Napomena';

  @override
  String get toolExpensesSend => 'Evidentiraj';

  @override
  String get toolExpensesSent => 'Trošak evidentiran.';

  @override
  String get toolExpensesNeedsTool => 'Izaberite alat.';

  @override
  String get toolExpensesNeedsAmount => 'Unesite koliko je koštalo.';

  @override
  String get toolExpenseKindRepair => 'Popravka';

  @override
  String get toolExpenseKindMaintenance => 'Održavanje';

  @override
  String get toolExpenseKindCalibration => 'Kalibracija';

  @override
  String get toolExpenseKindOther => 'Ostalo';

  @override
  String get companySettingsTitle => 'Profil firme';

  @override
  String get companySettingsLogo => 'Logo';

  @override
  String get companySettingsUploadLogo => 'Otpremi logo';

  @override
  String get companySettingsRemoveLogo => 'Ukloni logo';

  @override
  String companySettingsLogoTooLarge(int limit) {
    return 'Logo je veći od $limit MB ograničenja.';
  }

  @override
  String get companySettingsName => 'Naziv firme';

  @override
  String get companySettingsAddress => 'Adresa';

  @override
  String get companySettingsTaxId => 'PIB';

  @override
  String get companySettingsRegistrationNumber => 'Matični broj';

  @override
  String get companySettingsVatNumber => 'PDV broj';

  @override
  String get companySettingsPhone => 'Telefon';

  @override
  String get companySettingsEmail => 'Email';

  @override
  String get companySettingsWeeklyReportsForwardEmail =>
      'Prosleđivanje nedeljnih izveštaja na';

  @override
  String get companySettingsWeeklyReportsForwardEmailHint =>
      'Opciono — kopija svakog podnetog nedeljnog izveštaja se šalje na ovaj email.';

  @override
  String get companySettingsSaved => 'Profil firme sačuvan.';

  @override
  String get ledgersTitle => 'Evidencija';

  @override
  String get ledgersEmpty => 'Još nema evidencija.';

  @override
  String get ledgersSearchHint => 'Naziv…';

  @override
  String get ledgersSectionsTitle => 'Sekcije';

  @override
  String get ledgersSectionEmpty => 'Nema redova u ovoj sekciji.';

  @override
  String ledgersRowCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count redova',
      one: '1 red',
    );
    return '$_temp0';
  }

  @override
  String get ledgersNetTotal => 'Neto ukupno';

  @override
  String get workItemsDefectPhoto => 'Fotografija';

  @override
  String get workItemsDefectPhotoAdded => 'Fotografija priložena';

  @override
  String get workItemsDefectPhotoHint => 'Slika je obično ceo izveštaj.';

  @override
  String get workItemsDefectPhotoFailed =>
      'Nedostatak je prijavljen, ali fotografija nije priložena.';

  @override
  String get workItemsAddPhoto => 'Dodaj fotografiju';

  @override
  String get failureOffline =>
      'Nema veze sa serverom. Proverite mrežu i pokušajte ponovo.';

  @override
  String get failureTimeout => 'Server predugo ne odgovara. Pokušajte ponovo.';

  @override
  String get failureCancelled => 'Zahtev je otkazan.';

  @override
  String get failureCertificate =>
      'Sertifikat servera nije mogao da se proveri.';

  @override
  String get failureBadRequest => 'Zahtev je odbijen. Proverite unete podatke.';

  @override
  String get failureUnauthorized => 'Sesija je istekla. Prijavite se ponovo.';

  @override
  String get failureForbidden => 'Nemate dozvolu za ovu radnju.';

  @override
  String get failureNotFound => 'Traženi podatak nije pronađen.';

  @override
  String get failureConflict => 'Radnja je u sukobu sa trenutnim podacima.';

  @override
  String get failureServer => 'Greška na serveru. Pokušajte kasnije.';

  @override
  String get failureUnknown => 'Došlo je do greške. Pokušajte ponovo.';

  @override
  String get crashTitle => 'Ovaj ekran ne može da se prikaže';

  @override
  String get crashBody =>
      'Nešto na ovom ekranu je otkazalo pri iscrtavanju. Vratite se nazad i pokušajte ponovo.';

  @override
  String offlineDataNoticeTime(String time) {
    return 'Nema veze — prikazani su podaci sačuvani u $time.';
  }

  @override
  String offlineDataNoticeDate(String date) {
    return 'Nema veze — prikazani su podaci sačuvani $date.';
  }

  @override
  String get offlineDataRetry => 'Pokušaj ponovo';

  @override
  String get toolLookUpHint => 'Unesite QR kod odštampan na pločici alata.';

  @override
  String get serverAddressTitle => 'Adresa servera';

  @override
  String get serverAddressHint =>
      'Adresa servera vaše firme. Pitajte onoga ko ga je postavio; na istom Wi-Fi-ju kao server obično izgleda kao http://192.168.1.20:5000.';

  @override
  String get serverAddressLabel => 'Adresa';

  @override
  String get serverAddressInvalid =>
      'Unesite punu adresu, koja počinje sa http:// ili https://';

  @override
  String get commonSave => 'Sačuvaj';

  @override
  String get shiftWaitingToSend =>
      'Zabeleženo na ovom telefonu. Biće poslato kad bude signala.';

  @override
  String get ackBannerHeading => 'Potrebna potvrda';

  @override
  String get ackConfirmButton => 'Vidio sam';

  @override
  String get ackGatedMessage => 'Prvo potvrdite obavještenje koje čeka';
}
