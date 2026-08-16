// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appName => 'Construction Organizer';

  @override
  String get commonCancel => 'Cancel';

  @override
  String get commonRetry => 'Try again';

  @override
  String get commonLoadMore => 'Load more';

  @override
  String get commonSignIn => 'Sign in';

  @override
  String get commonSignOut => 'Sign out';

  @override
  String get commonSignOutQuestion => 'Sign out?';

  @override
  String get commonSignOutBody =>
      'You will need to sign in again to use the app.';

  @override
  String get commonNotSet => '—';

  @override
  String get commonDetails => 'Details';

  @override
  String get commonAssignment => 'Assignment';

  @override
  String get commonContact => 'Contact';

  @override
  String get commonEmployment => 'Employment';

  @override
  String get commonAccount => 'Account';

  @override
  String get commonResources => 'Resources';

  @override
  String get commonAlerts => 'Alerts';

  @override
  String get authSignInSubtitle => 'Sign in to your work account';

  @override
  String get authEmail => 'Email';

  @override
  String get authPassword => 'Password';

  @override
  String get authCurrentPassword => 'Current password';

  @override
  String get authNewPassword => 'New password';

  @override
  String get authConfirmPassword => 'Confirm new password';

  @override
  String get authShowPassword => 'Show password';

  @override
  String get authHidePassword => 'Hide password';

  @override
  String get authForgotPassword => 'Forgot password?';

  @override
  String get authResetPassword => 'Reset password';

  @override
  String get authResetIntro =>
      'Enter the email address of your work account. If an account exists, we will send a link to choose a new password.';

  @override
  String get authResetSent =>
      'If that address belongs to an account, a reset link is on its way.';

  @override
  String get authSendResetLink => 'Send reset link';

  @override
  String get authSendAgain => 'Send again';

  @override
  String get authChangePassword => 'Change password';

  @override
  String get authPasswordChanged => 'Password changed';

  @override
  String get authPasswordChangedBody =>
      'Your password has been updated. For security, all your signed-in devices have been signed out.';

  @override
  String get authSignInAgain => 'Sign in again';

  @override
  String get authSessionExpired =>
      'Your session has expired. Please sign in again.';

  @override
  String get validationEmailRequired => 'Email is required.';

  @override
  String get validationEmailInvalid => 'Enter a valid email address.';

  @override
  String get validationPasswordRequired => 'Password is required.';

  @override
  String get validationPasswordUpper =>
      'Password must contain an upper-case letter.';

  @override
  String get validationPasswordLower =>
      'Password must contain a lower-case letter.';

  @override
  String get validationPasswordDigit => 'Password must contain a digit.';

  @override
  String get validationPasswordsDiffer => 'The passwords do not match.';

  @override
  String get validationConfirmPassword => 'Confirm the new password.';

  @override
  String get errorNoConnection =>
      'No connection to the server. Check your network and try again.';

  @override
  String get errorTimeout =>
      'The server took too long to respond. Please try again.';

  @override
  String get errorCancelled => 'The request was cancelled.';

  @override
  String get errorCertificate =>
      'The server certificate could not be verified.';

  @override
  String get errorServer =>
      'The server encountered an error. Please try again later.';

  @override
  String get errorNotFound => 'The requested item could not be found.';

  @override
  String get errorForbidden =>
      'You do not have permission to perform this action.';

  @override
  String get errorBadRequest =>
      'The request was rejected. Please check the entered data.';

  @override
  String get errorConflict => 'The action conflicts with the current data.';

  @override
  String get errorUnknown => 'Something went wrong. Please try again.';

  @override
  String get navEmployees => 'Employees';

  @override
  String get navProjects => 'Projects';

  @override
  String get navVehicles => 'Vehicles';

  @override
  String get navTools => 'Tools';

  @override
  String get navMaterials => 'Materials';

  @override
  String get navNotifications => 'Notifications';

  @override
  String get employeesSearchHint => 'Name, number, position…';

  @override
  String get employeesEmpty => 'No employees match your search.';

  @override
  String get employeeNumber => 'Employee number';

  @override
  String get employeePosition => 'Position';

  @override
  String get employeePhone => 'Phone';

  @override
  String get employeeEmail => 'Email';

  @override
  String get employeeAddress => 'Address';

  @override
  String get employeeDateOfBirth => 'Date of birth';

  @override
  String get employeeEmployedSince => 'Employed since';

  @override
  String get employeeAppAccount => 'App account';

  @override
  String get employeeNoProjects => 'Not assigned to any project';

  @override
  String get projectsSearchHint => 'Name, client, address…';

  @override
  String get projectsEmpty => 'No projects match your search.';

  @override
  String get projectClient => 'Client';

  @override
  String get projectAddress => 'Address';

  @override
  String get projectStartDate => 'Start date';

  @override
  String get projectEndDate => 'End date';

  @override
  String get projectCoordinates => 'Coordinates';

  @override
  String get projectCrewEmpty => 'Nobody assigned yet';

  @override
  String projectAssignedOn(String date) {
    return 'Assigned $date';
  }

  @override
  String projectMemberSubtitle(String position, String number) {
    return '$position · $number';
  }

  @override
  String get vehiclesSearchHint => 'Brand, model, registration…';

  @override
  String get vehiclesEmpty => 'No vehicles match your search.';

  @override
  String get vehicleRegistration => 'Registration number';

  @override
  String get vehicleFuelType => 'Fuel type';

  @override
  String get vehicleUnassigned => 'Not assigned to any employee';

  @override
  String get toolsSearchHint => 'Name, category, serial number…';

  @override
  String get toolsEmpty => 'No tools match your search.';

  @override
  String get toolSerialNumber => 'Serial number';

  @override
  String get toolQrCode => 'QR code';

  @override
  String get toolUncategorised => 'Uncategorised';

  @override
  String get toolNotHeld => 'Not held by an employee';

  @override
  String get toolNotOnProject => 'Not assigned to any project';

  @override
  String get toolLookUp => 'Look up a tool';

  @override
  String get toolLookUpAction => 'Look up';

  @override
  String get toolLookUpByQr => 'Look up by QR code';

  @override
  String get toolByQrCode => 'By QR code';

  @override
  String toolCategoryLine(String category) {
    return 'Category: $category';
  }

  @override
  String toolSerialLine(String serial) {
    return 'Serial number: $serial';
  }

  @override
  String get materialsSearchHint => 'Name, warehouse…';

  @override
  String get materialsEmpty => 'No materials match your search.';

  @override
  String get materialStock => 'Stock';

  @override
  String get materialWarehouse => 'Warehouse';

  @override
  String get materialWarehouseStock => 'Warehouse stock';

  @override
  String get materialWarehouseOnly => 'Warehouse stock only';

  @override
  String get materialWarehouseNote => 'Warehouse stock, not tied to a project';

  @override
  String get materialLastUpdated => 'Last updated';

  @override
  String get materialNoAssignment => 'No assignment';

  @override
  String get notificationsEmpty => 'No notifications yet.';

  @override
  String get notificationsUnreadEmpty => 'Nothing unread.';

  @override
  String get notificationsUnread => 'Unread';

  @override
  String get notificationsMarkAllRead => 'Mark all read';

  @override
  String get notificationsDisabled =>
      'Notifications are turned off for this app.';

  @override
  String get notificationsNotConfigured =>
      'Push notifications are not configured in this build.';

  @override
  String get notificationsNotConfiguredBody =>
      'Push delivery is not configured in this build. Notifications are still listed here.';

  @override
  String get notificationsBlockedBody =>
      'Push notifications are turned off for this app. You can still read them here.';

  @override
  String get notificationsOpenSettings => 'Open app settings';

  @override
  String get notificationsTokenFailed => 'Could not obtain a device token.';

  @override
  String get notificationsFirebaseFailed => 'Firebase messaging failed.';

  @override
  String get locationSharingOn => 'Location sharing is on';

  @override
  String get locationSharingOnBody =>
      'Your position is sent to the office every minute while you are signed in.';

  @override
  String get locationStarting => 'Starting location sharing…';

  @override
  String get locationProblem => 'Location sharing has a problem';

  @override
  String get locationNotShared =>
      'Your position is not being shared with the office.';

  @override
  String get locationServicesOff => 'Location services are switched off';

  @override
  String get locationPermissionDenied => 'Location permission not granted';

  @override
  String get locationPermissionBlocked => 'Location permission is blocked';

  @override
  String get locationAllow => 'Allow location';

  @override
  String get locationOpenSettings => 'Open location settings';

  @override
  String get locationNoFix => 'No GPS fix yet.';

  @override
  String get locationReadFailed => 'Could not read the device location.';

  @override
  String locationQueued(String reason) {
    return 'Queued — $reason';
  }

  @override
  String get locationServiceNotificationTitle => 'Sharing your location';

  @override
  String get locationServiceNotificationBody =>
      'The office can see which site you are on. Sign out to stop.';

  @override
  String get locationServiceChannelName => 'Location sharing';

  @override
  String locationPending(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count readings waiting to be sent',
      one: '$count reading waiting to be sent',
      zero: 'Everything sent',
    );
    return '$_temp0';
  }

  @override
  String locationLastSent(String when) {
    return 'Last sent $when.';
  }

  @override
  String get locationOpenAppSettings => 'Open app settings';

  @override
  String get roleSuperAdmin => 'Super Admin';

  @override
  String get roleAdmin => 'Admin';

  @override
  String get roleProjectManager => 'Project Manager';

  @override
  String get roleForeman => 'Foreman';

  @override
  String get roleWorker => 'Worker';

  @override
  String get employeeStatusActive => 'Active';

  @override
  String get employeeStatusOnLeave => 'On leave';

  @override
  String get employeeStatusSuspended => 'Suspended';

  @override
  String get employeeStatusTerminated => 'Terminated';

  @override
  String get projectStatusPlanned => 'Planned';

  @override
  String get projectStatusActive => 'Active';

  @override
  String get projectStatusOnHold => 'On hold';

  @override
  String get projectStatusCompleted => 'Completed';

  @override
  String get projectStatusCancelled => 'Cancelled';

  @override
  String get vehicleStatusAvailable => 'Available';

  @override
  String get vehicleStatusAssigned => 'Assigned';

  @override
  String get vehicleStatusInService => 'In service';

  @override
  String get vehicleStatusOutOfService => 'Out of service';

  @override
  String get toolStatusAvailable => 'Available';

  @override
  String get toolStatusAssigned => 'Assigned';

  @override
  String get toolStatusUnderRepair => 'Under repair';

  @override
  String get toolStatusLost => 'Lost';

  @override
  String get toolStatusRetired => 'Retired';

  @override
  String get fuelPetrol => 'Petrol';

  @override
  String get fuelDiesel => 'Diesel';

  @override
  String get fuelElectric => 'Electric';

  @override
  String get fuelHybrid => 'Hybrid';

  @override
  String get fuelLpg => 'LPG';

  @override
  String get notificationTypeEmployeeAssigned => 'Employee assigned';

  @override
  String get notificationTypeProjectAssigned => 'Project assigned';

  @override
  String get notificationTypeToolAssigned => 'Tool assigned';

  @override
  String get notificationTypeVehicleAssigned => 'Vehicle assigned';

  @override
  String get notificationTypeAnnouncement => 'Announcement';

  @override
  String get settingsLanguage => 'Language';

  @override
  String get settingsLanguageSerbian => 'Srpski';

  @override
  String get settingsLanguageEnglish => 'English';

  @override
  String get navTimeEntries => 'Work time';

  @override
  String get shiftTitle => 'My work time';

  @override
  String get shiftRunning => 'You are clocked in';

  @override
  String get shiftOff => 'You are not clocked in';

  @override
  String shiftSince(String time) {
    return 'Since $time';
  }

  @override
  String shiftElapsed(int hours, int minutes) {
    return '$hours h $minutes min';
  }

  @override
  String get shiftClockIn => 'Clock in';

  @override
  String get shiftClockOut => 'Clock out';

  @override
  String get shiftClockOutTitle => 'End the shift';

  @override
  String get shiftBreakLabel => 'Unpaid break (minutes)';

  @override
  String get shiftBreakHint => 'Leave at 0 if you did not take one.';

  @override
  String get shiftProject => 'Site';

  @override
  String get shiftNoProject => 'No site';

  @override
  String get shiftWorkType => 'Type of work';

  @override
  String get shiftConfirm => 'Confirm';

  @override
  String get shiftHistory => 'Recent entries';

  @override
  String get shiftHistoryEmpty => 'No hours recorded yet.';

  @override
  String get shiftWorked => 'Worked';

  @override
  String get shiftBreak => 'Break';

  @override
  String shiftBreakMinutes(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count minutes',
      few: '$count minutes',
      one: '$count minute',
      zero: 'No break',
    );
    return '$_temp0';
  }

  @override
  String shiftSentBack(String reason) {
    return 'Sent back: $reason';
  }

  @override
  String get shiftNotAnEmployee =>
      'This account is not linked to an employee, so it cannot record work time.';

  @override
  String get timeEntryStatusInProgress => 'Running';

  @override
  String get timeEntryStatusSubmitted => 'Awaiting review';

  @override
  String get timeEntryStatusApproved => 'Approved';

  @override
  String get timeEntryStatusRejected => 'Sent back';

  @override
  String get workTypeRegular => 'Regular';

  @override
  String get workTypeOvertime => 'Overtime';

  @override
  String get workTypeWeekend => 'Weekend';

  @override
  String get workTypePublicHoliday => 'Public holiday';

  @override
  String get workTypeTravel => 'Travel';

  @override
  String get attachmentsTitle => 'Documents';

  @override
  String get attachmentsEmpty => 'No documents on this record.';

  @override
  String get attachmentsExpired => 'Expired';

  @override
  String attachmentsExpiresOn(String date) {
    return 'Valid until $date';
  }

  @override
  String get attachmentsOpenFailed => 'The file could not be opened.';

  @override
  String get attachmentsAddPhoto => 'Add a photo';

  @override
  String get attachmentsTakePhoto => 'Take a photo';

  @override
  String get attachmentsFromGallery => 'Choose from gallery';

  @override
  String get attachmentsPhotoNote => 'Note (optional)';

  @override
  String get attachmentsUploading => 'Uploading…';

  @override
  String get attachmentsUploaded => 'Photo added.';

  @override
  String attachmentsTooLarge(int limit) {
    return 'The photo is larger than the $limit MB limit.';
  }

  @override
  String get attachmentsNotAnImage =>
      'Only a document or image can be attached.';

  @override
  String get attachmentCategoryContract => 'Contract';

  @override
  String get attachmentCategoryCertificate => 'Certificate';

  @override
  String get attachmentCategoryMedicalCheck => 'Medical check';

  @override
  String get attachmentCategoryLicence => 'Licence';

  @override
  String get attachmentCategoryInsurance => 'Insurance';

  @override
  String get attachmentCategorySiteDocument => 'Site document';

  @override
  String get attachmentCategoryPhoto => 'Photo';

  @override
  String get attachmentCategoryOther => 'Other';

  @override
  String get navWorkItems => 'My work';

  @override
  String get workItemsEmpty => 'Nothing on your list.';

  @override
  String get workItemsIncludeFinished => 'Include finished';

  @override
  String workItemsDue(String date) {
    return 'Due $date';
  }

  @override
  String get workItemsOverdue => 'Overdue';

  @override
  String get workItemsNoDueDate => 'No deadline';

  @override
  String get workItemsNoProject => 'No site';

  @override
  String get workItemsReportDefect => 'Report a defect';

  @override
  String get workItemsDefectTitle => 'What is wrong';

  @override
  String get workItemsDefectDescription => 'Details (optional)';

  @override
  String get workItemsDefectSend => 'Report';

  @override
  String get workItemsDefectSent => 'Defect reported.';

  @override
  String get workItemsDefectNeedsTitle =>
      'Describe the problem in a few words.';

  @override
  String workItemsPhotoCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count photos',
      one: '$count photo',
      zero: 'No photos',
    );
    return '$_temp0';
  }

  @override
  String get workItemKindTask => 'Task';

  @override
  String get workItemKindDefect => 'Defect';

  @override
  String get workItemStatusOpen => 'Open';

  @override
  String get workItemStatusInProgress => 'In progress';

  @override
  String get workItemStatusResolved => 'Done, to check';

  @override
  String get workItemStatusClosed => 'Closed';

  @override
  String get workItemStatusCancelled => 'Cancelled';

  @override
  String get workItemPriorityLow => 'Low';

  @override
  String get workItemPriorityNormal => 'Normal';

  @override
  String get workItemPriorityHigh => 'High';

  @override
  String get workItemPriorityUrgent => 'Urgent';

  @override
  String get navSchedule => 'My schedule';

  @override
  String get navAbsences => 'Time off';

  @override
  String get scheduleTitle => 'My schedule';

  @override
  String get scheduleEmpty => 'Nothing scheduled for the next two weeks.';

  @override
  String get scheduleToday => 'Today';

  @override
  String get scheduleTomorrow => 'Tomorrow';

  @override
  String get scheduleContinues => 'Runs on';

  @override
  String get scheduleUpcoming => 'Next two weeks';

  @override
  String scheduleDateRange(String from, String to) {
    return '$from – $to';
  }

  @override
  String get scheduleAway => 'Away';

  @override
  String get scheduleOnSite => 'On site';

  @override
  String get absencesTitle => 'Time off';

  @override
  String get absencesEmpty => 'You have not asked for any time off.';

  @override
  String get absencesPendingOnly => 'Waiting for an answer';

  @override
  String get absencesRequest => 'Ask for time off';

  @override
  String get absencesType => 'Kind';

  @override
  String get absencesStartDate => 'From';

  @override
  String get absencesEndDate => 'To';

  @override
  String get absencesReason => 'Reason (optional)';

  @override
  String get absencesSend => 'Send request';

  @override
  String get absencesSent => 'Request sent.';

  @override
  String get absencesPickDates =>
      'Pick the first and last day you will be away.';

  @override
  String get absencesEndsBeforeStart =>
      'The last day cannot be before the first.';

  @override
  String absencesDayCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count days',
      few: '$count days',
      one: '$count day',
    );
    return '$_temp0';
  }

  @override
  String get absencesWithdraw => 'Withdraw';

  @override
  String get absencesWithdrawTitle => 'Withdraw this request?';

  @override
  String get absencesWithdrawBody =>
      'Your supervisor will no longer see you asking for these days.';

  @override
  String get absencesWithdrawn => 'Request withdrawn.';

  @override
  String get absencesGrantedLocked =>
      'Granted time off has to be cancelled by your supervisor.';

  @override
  String absencesAnsweredBy(String name) {
    return 'Answered by $name';
  }

  @override
  String get absenceTypeAnnualLeave => 'Annual leave';

  @override
  String get absenceTypeSickLeave => 'Sick leave';

  @override
  String get absenceTypeUnpaidLeave => 'Unpaid leave';

  @override
  String get absenceTypePaidSpecialLeave => 'Paid special leave';

  @override
  String get absenceTypeTraining => 'Training';

  @override
  String get absenceTypeOther => 'Other';

  @override
  String get absenceStatusRequested => 'Waiting';

  @override
  String get absenceStatusApproved => 'Granted';

  @override
  String get absenceStatusRejected => 'Refused';

  @override
  String get absenceStatusCancelled => 'Withdrawn';

  @override
  String get navVehicleExpenses => 'Vehicle costs';

  @override
  String get vehicleExpensesTitle => 'Vehicle costs';

  @override
  String get vehicleExpensesEmpty => 'No costs recorded yet.';

  @override
  String get vehicleExpensesFuelOnly => 'Fill-ups only';

  @override
  String get vehicleExpensesRecord => 'Record a cost';

  @override
  String get vehicleExpensesVehicle => 'Vehicle';

  @override
  String get vehicleExpensesKind => 'Kind';

  @override
  String get vehicleExpensesAmount => 'Amount';

  @override
  String get vehicleExpensesLitres => 'Litres';

  @override
  String get vehicleExpensesOdometer => 'Odometer (km)';

  @override
  String get vehicleExpensesSupplier => 'Where';

  @override
  String get vehicleExpensesNote => 'Note';

  @override
  String get vehicleExpensesSend => 'Record';

  @override
  String get vehicleExpensesSent => 'Cost recorded.';

  @override
  String get vehicleExpensesNeedsVehicle => 'Pick the vehicle.';

  @override
  String get vehicleExpensesNeedsAmount => 'Enter what it cost.';

  @override
  String get vehicleExpensesFuelNeedsLitres => 'Say how many litres went in.';

  @override
  String get vehicleExpensesOdometerHint =>
      'Optional, but two readings are what give you consumption.';

  @override
  String vehicleExpensesPerLitre(String price) {
    return '$price per litre';
  }

  @override
  String get vehicleExpenseKindFuel => 'Fuel';

  @override
  String get vehicleExpenseKindService => 'Service';

  @override
  String get vehicleExpenseKindRepair => 'Repair';

  @override
  String get vehicleExpenseKindInsurance => 'Insurance';

  @override
  String get vehicleExpenseKindRegistration => 'Registration';

  @override
  String get vehicleExpenseKindOther => 'Other';

  @override
  String get workItemsDefectPhoto => 'Photo';

  @override
  String get workItemsDefectPhotoAdded => 'Photo attached';

  @override
  String get workItemsDefectPhotoHint =>
      'The picture is usually the whole report.';

  @override
  String get workItemsDefectPhotoFailed =>
      'The defect was reported, but the photo could not be attached.';

  @override
  String get workItemsAddPhoto => 'Add a photo';

  @override
  String get failureOffline =>
      'No connection to the server. Check your network and try again.';

  @override
  String get failureTimeout =>
      'The server took too long to respond. Please try again.';

  @override
  String get failureCancelled => 'The request was cancelled.';

  @override
  String get failureCertificate =>
      'The server certificate could not be verified.';

  @override
  String get failureBadRequest =>
      'The request was rejected. Please check the entered data.';

  @override
  String get failureUnauthorized =>
      'Your session has expired. Please sign in again.';

  @override
  String get failureForbidden =>
      'You do not have permission to perform this action.';

  @override
  String get failureNotFound => 'The requested item could not be found.';

  @override
  String get failureConflict => 'The action conflicts with the current data.';

  @override
  String get failureServer =>
      'The server encountered an error. Please try again later.';

  @override
  String get failureUnknown => 'Something went wrong. Please try again.';

  @override
  String get crashTitle => 'This screen could not be displayed';

  @override
  String get crashBody =>
      'Something on this screen failed while it was being drawn. Go back and try again.';

  @override
  String offlineDataNoticeTime(String time) {
    return 'No connection — showing data saved at $time.';
  }

  @override
  String offlineDataNoticeDate(String date) {
    return 'No connection — showing data saved on $date.';
  }

  @override
  String get offlineDataRetry => 'Try again';

  @override
  String get toolLookUpHint => 'Enter the QR code printed on the tool\'s tag.';

  @override
  String get serverAddressTitle => 'Server address';

  @override
  String get serverAddressHint =>
      'The address of your organisation\'s server. Ask whoever set it up; on the same Wi-Fi as the server it usually looks like http://192.168.1.20:5000.';

  @override
  String get serverAddressLabel => 'Address';

  @override
  String get serverAddressInvalid =>
      'Enter a full address, starting with http:// or https://';

  @override
  String get commonSave => 'Save';

  @override
  String get shiftWaitingToSend =>
      'Recorded on this phone. It will be sent when there is signal.';
}
