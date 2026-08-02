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
  String get commonRetry => 'Pokušaj ponovo';

  @override
  String get commonLoadMore => 'Učitaj još';

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
  String get materialWarehouseNote =>
      'Zalihe u magacinu, nisu vezane za projekat';

  @override
  String get materialLastUpdated => 'Poslednja izmena';

  @override
  String get materialNoAssignment => 'Nije zaduženo';

  @override
  String get notificationsEmpty => 'Još nema obaveštenja.';

  @override
  String get notificationsUnreadEmpty => 'Nema nepročitanih.';

  @override
  String get notificationsUnread => 'Nepročitano';

  @override
  String get notificationsMarkAllRead => 'Označi sve pročitanim';

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
}
