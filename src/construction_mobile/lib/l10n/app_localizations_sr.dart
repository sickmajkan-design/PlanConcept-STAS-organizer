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
  String get navVehicleExpenses => 'Troškovi vozila';

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
}
