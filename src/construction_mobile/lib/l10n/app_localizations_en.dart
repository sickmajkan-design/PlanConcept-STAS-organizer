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
  String get commonDelete => 'Delete';

  @override
  String get commonEdit => 'Edit';

  @override
  String get commonAdd => 'Add';

  @override
  String get commonStatus => 'Status';

  @override
  String get vehicleFormAddTitle => 'Add a vehicle';

  @override
  String get vehicleFormEditTitle => 'Edit vehicle';

  @override
  String get vehicleFormBrand => 'Brand';

  @override
  String get vehicleFormModel => 'Model';

  @override
  String get vehicleFormAdded => 'Vehicle added.';

  @override
  String get vehicleFormSaved => 'Vehicle updated.';

  @override
  String get vehicleDeleteTitle => 'Delete this vehicle?';

  @override
  String vehicleDeleteBody(String name) {
    return '$name will be removed. This cannot be undone.';
  }

  @override
  String get commonRetry => 'Try again';

  @override
  String get commonLoadMore => 'Load more';

  @override
  String commonTotalCount(int count) {
    return '$count total';
  }

  @override
  String commonCountOfTotal(int shown, int total) {
    return '$shown of $total';
  }

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
  String get commonCompany => 'Company';

  @override
  String get commonEmployee => 'Employee';

  @override
  String get commonMaterial => 'Material';

  @override
  String get commonTool => 'Tool';

  @override
  String get commonVehicle => 'Vehicle';

  @override
  String get commonProject => 'Project';

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
  String get validationPasswordMinLength =>
      'Password must be at least 8 characters long.';

  @override
  String validationFieldRequired(String field) {
    return '$field is required.';
  }

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
  String get navHome => 'Home';

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
  String projectSubOf(String name) {
    return 'Part of $name';
  }

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
  String projectAssignedCount(int count) {
    return '$count assigned';
  }

  @override
  String get projectCrewTitle => 'Crew';

  @override
  String projectCrewCount(int count) {
    return 'Crew ($count)';
  }

  @override
  String employeeProjectsCount(int count) {
    return 'Projects ($count)';
  }

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
  String get vehicleOwnershipType => 'Ownership';

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
  String get toolFormAddTitle => 'Add a tool';

  @override
  String get toolFormEditTitle => 'Edit tool';

  @override
  String get toolFormName => 'Name';

  @override
  String get toolFormCategory => 'Category';

  @override
  String get toolFormAdded => 'Tool added.';

  @override
  String get toolFormSaved => 'Tool updated.';

  @override
  String get toolDeleteTitle => 'Delete this tool?';

  @override
  String toolDeleteBody(String name) {
    return '$name will be removed. This cannot be undone.';
  }

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
  String get vehicleQrCode => 'QR code';

  @override
  String get vehicleOwnershipTypeOwned => 'Owned';

  @override
  String get vehicleOwnershipTypeRented => 'Rented';

  @override
  String get vehicleRentalProvider => 'Rented from';

  @override
  String get vehicleRentalMonthlyAmount => 'Monthly rate';

  @override
  String vehicleLoanedOutTo(String name) {
    return 'Loaned out to $name';
  }

  @override
  String get rentalRatesTitle => 'Rental / lease';

  @override
  String get rentalRatesAdd => 'Add a rate';

  @override
  String get rentalRatesEditTitle => 'Edit rate';

  @override
  String get rentalRatesEmpty => 'No rate on file yet.';

  @override
  String get rentalRatesProvider => 'Provider (optional)';

  @override
  String get rentalRatesNote => 'Note (optional)';

  @override
  String get rentalRatesMonthlyAmount => 'Monthly amount';

  @override
  String get rentalRatesStartDate => 'From';

  @override
  String get rentalRatesEndDate => 'To (optional, open-ended if blank)';

  @override
  String get rentalRatesOpenEnded => 'Open';

  @override
  String get rentalRatesDeleteTitle => 'Delete this rate?';

  @override
  String get rentalRatesDeleteBody => 'This cannot be undone.';

  @override
  String get rentalRatesSaved => 'Rental rate saved.';

  @override
  String get rentalOutTitle => 'Loaned out';

  @override
  String get rentalOutAdd => 'Loan it out';

  @override
  String get rentalOutEditTitle => 'Edit loan';

  @override
  String get rentalOutEmpty => 'Not currently loaned out to anyone.';

  @override
  String get rentalOutRenterName => 'Renter\'s name';

  @override
  String get rentalOutCustomer => 'Linked customer (optional)';

  @override
  String get rentalOutNoCustomer => 'No linked customer';

  @override
  String get rentalOutDailyRate => 'Daily rate';

  @override
  String get rentalOutStartDate => 'From';

  @override
  String get rentalOutStillOut => 'Still out';

  @override
  String get rentalOutReturn => 'Mark returned';

  @override
  String get rentalOutReturned => 'Marked as returned.';

  @override
  String get rentalOutDeleteTitle => 'Delete this loan?';

  @override
  String get rentalOutDeleteBody => 'This cannot be undone.';

  @override
  String get rentalOutSaved => 'Loan saved.';

  @override
  String get scanTitle => 'Scan or look up';

  @override
  String get scanHint =>
      'Scan the QR label on the tool or vehicle, or enter its code below.';

  @override
  String get scanAction => 'Scan QR code';

  @override
  String get scanToggleFlash => 'Toggle flashlight';

  @override
  String get scanCodeLabel => 'QR code';

  @override
  String get scanToolFound => 'Tool';

  @override
  String get scanVehicleFound => 'Vehicle';

  @override
  String get scanCheckOutToMe => 'Check out to me';

  @override
  String get scanReturn => 'Return';

  @override
  String get scanCheckedOutToYou => 'Checked out to you';

  @override
  String scanCheckedOutToOther(String name) {
    return 'Checked out to $name';
  }

  @override
  String get scanNotCheckedOut => 'Not currently checked out';

  @override
  String get scanCheckOutSuccess => 'Checked out to you.';

  @override
  String get scanReturnSuccess => 'Returned.';

  @override
  String get scanCameraPermissionDenied =>
      'Camera permission is required to scan a QR code.';

  @override
  String get scanTransferToEmployee => 'Transfer to employee';

  @override
  String get scanTransferToProject => 'Transfer to site';

  @override
  String get scanPickEmployee => 'Employee';

  @override
  String get scanPickProject => 'Site';

  @override
  String get scanTransferConfirm => 'Transfer';

  @override
  String scanTransferSuccess(String name) {
    return 'Transferred to $name.';
  }

  @override
  String scanTransferProjectSuccess(String name) {
    return 'Placed on $name.';
  }

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
  String get materialFormAddTitle => 'Add a material';

  @override
  String get materialFormEditTitle => 'Edit material';

  @override
  String get materialFormName => 'Name';

  @override
  String get materialFormUnit => 'Unit';

  @override
  String get materialFormUnitPrice => 'Unit price (optional)';

  @override
  String get materialFormAdded => 'Material added.';

  @override
  String get materialFormSaved => 'Material updated.';

  @override
  String get materialDeleteTitle => 'Delete this material?';

  @override
  String materialDeleteBody(String name) {
    return '$name will be removed. This cannot be undone.';
  }

  @override
  String get materialWarehouseNote => 'Warehouse stock, not tied to a project';

  @override
  String get materialLastUpdated => 'Last updated';

  @override
  String get materialNoAssignment => 'No assignment';

  @override
  String get employeeFormAddTitle => 'Add an employee';

  @override
  String get employeeFormEditTitle => 'Edit employee';

  @override
  String get employeeFormNumber => 'Employee number';

  @override
  String get employeeFormFirstName => 'First name';

  @override
  String get employeeFormLastName => 'Last name';

  @override
  String get employeeFormEmploymentDate => 'Employment date';

  @override
  String get employeeFormAdded => 'Employee added.';

  @override
  String get employeeFormSaved => 'Employee updated.';

  @override
  String get employeeDeleteTitle => 'Delete this employee?';

  @override
  String employeeDeleteBody(String name) {
    return '$name will be removed. This cannot be undone.';
  }

  @override
  String get employeeType => 'Type';

  @override
  String get employeeTypeEmployee => 'Employee';

  @override
  String get employeeTypeSubcontractor => 'Subcontractor';

  @override
  String get projectFormAddTitle => 'Add a project';

  @override
  String get projectFormEditTitle => 'Edit project';

  @override
  String get projectFormName => 'Project name';

  @override
  String get projectFormDescription => 'Description (optional)';

  @override
  String get projectFormParentProject => 'Parent project (optional)';

  @override
  String get projectFormNoParent => 'No parent — a main project';

  @override
  String get projectFormNoCustomer => 'No customer';

  @override
  String get projectFormCountryCode => 'Country code (e.g. BA)';

  @override
  String get projectFormShiftStartTime => 'Shift start time (optional)';

  @override
  String get projectFormContractValue => 'Contract value (optional)';

  @override
  String get projectFormLatitude => 'Latitude';

  @override
  String get projectFormLongitude => 'Longitude';

  @override
  String get projectFormAdded => 'Project added.';

  @override
  String get projectFormSaved => 'Project updated.';

  @override
  String get projectDeleteTitle => 'Delete this project?';

  @override
  String projectDeleteBody(String name) {
    return '$name will be removed. This cannot be undone.';
  }

  @override
  String get notificationsEmpty => 'No notifications yet.';

  @override
  String get notificationsUnreadEmpty => 'Nothing unread.';

  @override
  String get notificationsUnread => 'Unread';

  @override
  String notificationsUnreadCount(int count) {
    return 'Unread ($count)';
  }

  @override
  String get notificationsMarkAllRead => 'Mark all read';

  @override
  String notificationsAcknowledgedOn(String date) {
    return 'Confirmed on $date';
  }

  @override
  String get notificationsOpenRelated => 'Open';

  @override
  String get notificationProjectAssignedTitle => 'New project assigned';

  @override
  String notificationProjectAssignedBody(String projectName) {
    return 'You have been assigned to project \"$projectName\".';
  }

  @override
  String notificationProjectAssignedBodyWithAddress(
    String projectName,
    String address,
  ) {
    return 'You have been assigned to project \"$projectName\", at $address.';
  }

  @override
  String notificationProjectAssignedBodyFull(
    String projectName,
    String address,
    String shiftStartTime,
  ) {
    return 'You have been assigned to project \"$projectName\", at $address. Shift starts at $shiftStartTime.';
  }

  @override
  String get notificationEmployeeAssignedTitle =>
      'Employee assigned to your project';

  @override
  String notificationEmployeeAssignedBody(
    String employeeName,
    String projectName,
  ) {
    return '$employeeName has been assigned to project \"$projectName\".';
  }

  @override
  String get notificationVehicleAssignedTitle => 'Vehicle assigned';

  @override
  String notificationVehicleAssignedBody(
    String brand,
    String model,
    String registration,
  ) {
    return 'Vehicle $brand $model ($registration) has been assigned to you.';
  }

  @override
  String get notificationToolAssignedTitle => 'Tool assigned';

  @override
  String notificationToolAssignedBody(String toolName) {
    return 'Tool \"$toolName\" has been assigned to you.';
  }

  @override
  String get notificationDocumentExpiringTitle => 'Document expiring soon';

  @override
  String get notificationDocumentExpiredTitle => 'Document has expired';

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
  String get notificationTaskAssignedTitle => 'Task assigned to you';

  @override
  String get notificationDefectAssignedTitle => 'Defect assigned to you';

  @override
  String notificationWorkItemAssignedBodyWithDueDate(
    String title,
    String dueDate,
  ) {
    return '$title — due $dueDate';
  }

  @override
  String get notificationWorkItemOverdueTitle => 'Overdue';

  @override
  String get notificationWorkItemDueSoonTitle => 'Due soon';

  @override
  String notificationWorkItemDueBody(String title, String dueDate) {
    return '$title ($dueDate)';
  }

  @override
  String get notificationShiftAutoClosedTitle => 'Shift closed automatically';

  @override
  String notificationShiftAutoClosedBody(String shiftDate) {
    return 'You did not clock out on $shiftDate, so the shift was closed automatically and is waiting for review.';
  }

  @override
  String get notificationAbsenceEditProposedTitle =>
      'Change proposed for approved leave';

  @override
  String notificationAbsenceEditProposedBody(String startDate, String endDate) {
    return '$startDate–$endDate — please confirm or decline.';
  }

  @override
  String get notificationAbsenceEditConfirmedTitle => 'Leave change confirmed';

  @override
  String notificationAbsenceEditConfirmedBody(
    String startDate,
    String endDate,
  ) {
    return '$startDate–$endDate';
  }

  @override
  String get notificationAbsenceEditDeclinedTitle => 'Leave change declined';

  @override
  String get notificationAbsenceEditDeclinedBody =>
      'The other side declined your proposed change.';

  @override
  String get notificationWeeklyReportDueTitle =>
      'Weekly hours not yet submitted';

  @override
  String notificationWeeklyReportDueBody(
    String projectName,
    String isoWeek,
    String isoYear,
  ) {
    return '$projectName — KW$isoWeek/$isoYear';
  }

  @override
  String get notificationEmployeeClockedInTitle => 'Clocked in';

  @override
  String notificationEmployeeClockedInBody(
    String employeeName,
    String projectName,
  ) {
    return '$employeeName clocked in at $projectName.';
  }

  @override
  String get notificationEmployeeClockedOutTitle => 'Clocked out';

  @override
  String notificationEmployeeClockedOutBody(
    String employeeName,
    String projectName,
    String hours,
    String minutes,
  ) {
    return '$employeeName clocked out from $projectName after ${hours}h ${minutes}m.';
  }

  @override
  String get notificationUnassignedClockInTitle =>
      'Clock-in at an unassigned site';

  @override
  String notificationUnassignedClockInBody(
    String employeeName,
    String projectName,
  ) {
    return '$employeeName clocked in at $projectName, but is not currently posted there.';
  }

  @override
  String get notificationDefectReportedTitle => 'New defect reported';

  @override
  String notificationDefectReportedBody(String reporterName, String title) {
    return '$reporterName reported: $title';
  }

  @override
  String get notificationAbsenceRequestedTitle => 'Time off requested';

  @override
  String notificationAbsenceRequestedBody(
    String employeeName,
    String startDate,
    String endDate,
  ) {
    return '$employeeName asked for time off, from $startDate to $endDate.';
  }

  @override
  String get notificationDocumentRetentionEndedTitle =>
      'Document retention period ended';

  @override
  String notificationDocumentRetentionEndedBody(
    String fileName,
    String retainUntil,
  ) {
    return '$fileName no longer has to be kept (was until $retainUntil). Delete it yourself if it is no longer needed.';
  }

  @override
  String notificationDocumentRetentionEndedBodyWithOwner(
    String fileName,
    String ownerName,
    String retainUntil,
  ) {
    return '$fileName — $ownerName no longer has to be kept (was until $retainUntil). Delete it yourself if it is no longer needed.';
  }

  @override
  String get notificationTypeDirectMessage => 'Direct message';

  @override
  String get notificationTypeDocumentExpiring => 'Document expiring';

  @override
  String get notificationTypeTaskAssigned => 'Task assigned';

  @override
  String get notificationTypeDefectAssigned => 'Defect assigned';

  @override
  String get notificationTypeWorkItemDue => 'Work due';

  @override
  String get notificationTypeShiftAutoClosed => 'Shift auto-closed';

  @override
  String get notificationTypeBulletinPosted => 'Bulletin post';

  @override
  String get notificationTypeAbsenceEditProposed => 'Leave change';

  @override
  String get notificationTypeWeeklyReportDue => 'Weekly report due';

  @override
  String get notificationTypeEmployeeClockedIn => 'Clocked in';

  @override
  String get notificationTypeEmployeeClockedOut => 'Clocked out';

  @override
  String get notificationTypeUnassignedProjectClockIn => 'Unassigned clock-in';

  @override
  String get notificationTypeDefectReported => 'Defect reported';

  @override
  String get notificationTypeAbsenceRequested => 'Leave requested';

  @override
  String get notificationTypeDocumentRetentionEnded =>
      'Document retention ended';

  @override
  String get announceTitle => 'Send an announcement';

  @override
  String get announceSubject => 'Subject';

  @override
  String get announceMessage => 'Message';

  @override
  String get announceAudienceRole => 'Role';

  @override
  String get announceEveryRole => 'Every role';

  @override
  String get announceAudienceProject => 'Project';

  @override
  String get announceEveryProject => 'Every project';

  @override
  String get announceAudienceGroup => 'Group';

  @override
  String get announceEveryGroup => 'Every group';

  @override
  String get announceRequiresAcknowledgment =>
      'Require confirmation before recipients can do anything else';

  @override
  String get announceHint =>
      'A phone notification cannot be recalled — the audience is the one thing worth checking twice.';

  @override
  String get announceSend => 'Send';

  @override
  String announceSent(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: 'Sent to $count people.',
      one: 'Sent to 1 person.',
      zero: 'Sent to nobody — nobody matched.',
    );
    return '$_temp0';
  }

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
  String get vehicleStatusRentedOut => 'Loaned out';

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
  String get toolStatusRentedOut => 'Loaned out';

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
  String get shiftAutoClosed => 'Auto-closed';

  @override
  String get shiftLocationMismatch => 'Clocked in away from the site';

  @override
  String get shiftTimeMismatch => 'Clocked in outside the expected shift time';

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
  String get attachmentsAddDocument => 'Add a document';

  @override
  String get attachmentsPickFile => 'Choose a file';

  @override
  String get attachmentsChangeFile => 'Choose a different file';

  @override
  String get attachmentsCategory => 'Category';

  @override
  String get attachmentsDescription => 'Description (optional)';

  @override
  String get attachmentsExpiryDate => 'Expiry date (optional)';

  @override
  String get attachmentsRetainUntil => 'Retain until (optional)';

  @override
  String get attachmentsSaved => 'Document added.';

  @override
  String get attachmentsDeleteTitle => 'Delete this document?';

  @override
  String attachmentsDeleteBody(String name) {
    return '$name will be removed. This cannot be undone.';
  }

  @override
  String attachmentsRetainedCannotDelete(String date) {
    return 'Kept until $date — cannot be deleted yet.';
  }

  @override
  String get attachmentsOpen => 'Open';

  @override
  String get attachmentsOpeningExternally => 'Opening in another app…';

  @override
  String get attachmentsOpenExternalFailed =>
      'No app on this phone can open this file type.';

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
  String get navWeeklyReports => 'Weekly reports';

  @override
  String get navBulletin => 'Bulletin board';

  @override
  String get bulletinTitle => 'Bulletin board';

  @override
  String get bulletinEmpty => 'Nothing posted right now.';

  @override
  String bulletinPostedBy(String name) {
    return 'Posted by $name';
  }

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
  String get weeklyReportsTitle => 'Weekly site reports';

  @override
  String get weeklyReportsDescription =>
      'Send the week\'s signed hours, Aufmaß, or other proof of work for a site you are posted to.';

  @override
  String get weeklyReportsSubmit => 'Submit report';

  @override
  String get weeklyReportsProject => 'Site';

  @override
  String get weeklyReportsProjectsFailed => 'Could not load your sites.';

  @override
  String get weeklyReportsIsoYear => 'Year';

  @override
  String get weeklyReportsIsoWeek => 'Week';

  @override
  String get weeklyReportsType => 'Kind';

  @override
  String get weeklyReportsHours => 'Hours';

  @override
  String get weeklyReportsNote => 'Note (optional)';

  @override
  String get weeklyReportsAttachFile => 'Attach file';

  @override
  String get weeklyReportsFileTooLarge =>
      'That file is larger than the 20 MB limit.';

  @override
  String get weeklyReportsSend => 'Send';

  @override
  String get notifyEmployeeAction => 'Notify';

  @override
  String get notifyEmployeeTitle => 'Send a message';

  @override
  String get notifyEmployeeSubject => 'Subject';

  @override
  String get notifyEmployeeMessage => 'Message';

  @override
  String get notifyEmployeeRequireAck => 'Require confirmation';

  @override
  String get notifyEmployeeRequireAckHint =>
      'They cannot use anything else in the app until they confirm they saw this.';

  @override
  String get notifyEmployeeSend => 'Send';

  @override
  String get notifyEmployeeSent => 'Message sent.';

  @override
  String get teamTodayAction => 'Team today';

  @override
  String get teamTodayTitle => 'Team today';

  @override
  String get teamTodayEmpty => 'Nobody has clocked in today yet.';

  @override
  String teamTodayStillWorking(String time) {
    return 'Since $time — still working';
  }

  @override
  String get weeklyReportsSent => 'Report sent.';

  @override
  String get weeklyReportTypeSignedHours => 'Signed hours';

  @override
  String get weeklyReportTypeAufmass => 'Aufmaß';

  @override
  String get weeklyReportTypeOther => 'Other';

  @override
  String get weeklyReportStatusSubmitted => 'Awaiting review';

  @override
  String get weeklyReportStatusProcessed => 'Processed';

  @override
  String get weeklyReportsHistoryTitle => 'Sent so far';

  @override
  String get weeklyReportsHistoryEmpty => 'Nothing sent yet.';

  @override
  String weeklyReportsIsoWeekLabel(int isoWeek, int isoYear) {
    return 'KW$isoWeek/$isoYear';
  }

  @override
  String weeklyReportsProcessedOn(String date) {
    return 'Processed $date';
  }

  @override
  String get navVehicleExpenses => 'Vehicle costs';

  @override
  String get navToolExpenses => 'Tool costs';

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
  String get toolExpensesTitle => 'Tool costs';

  @override
  String get toolExpensesEmpty => 'No costs recorded yet.';

  @override
  String get toolExpensesRecord => 'Record a cost';

  @override
  String get toolExpensesTool => 'Tool';

  @override
  String get toolExpensesKind => 'Kind';

  @override
  String get toolExpensesAmount => 'Amount';

  @override
  String get toolExpensesSupplier => 'Where';

  @override
  String get toolExpensesNote => 'Note';

  @override
  String get toolExpensesSend => 'Record';

  @override
  String get toolExpensesSent => 'Cost recorded.';

  @override
  String get toolExpensesNeedsTool => 'Pick the tool.';

  @override
  String get toolExpensesNeedsAmount => 'Enter what it cost.';

  @override
  String get toolExpenseKindRepair => 'Repair';

  @override
  String get toolExpenseKindMaintenance => 'Maintenance';

  @override
  String get toolExpenseKindCalibration => 'Calibration';

  @override
  String get toolExpenseKindOther => 'Other';

  @override
  String get companySettingsTitle => 'Company profile';

  @override
  String get companySettingsLogo => 'Logo';

  @override
  String get companySettingsUploadLogo => 'Upload logo';

  @override
  String get companySettingsRemoveLogo => 'Remove logo';

  @override
  String companySettingsLogoTooLarge(int limit) {
    return 'The logo is larger than the $limit MB limit.';
  }

  @override
  String get companySettingsName => 'Company name';

  @override
  String get companySettingsAddress => 'Address';

  @override
  String get companySettingsTaxId => 'Tax ID';

  @override
  String get companySettingsRegistrationNumber => 'Registration number';

  @override
  String get companySettingsVatNumber => 'VAT number';

  @override
  String get companySettingsPhone => 'Phone';

  @override
  String get companySettingsEmail => 'Email';

  @override
  String get companySettingsWeeklyReportsForwardEmail =>
      'Forward weekly reports to';

  @override
  String get companySettingsWeeklyReportsForwardEmailHint =>
      'Optional — a copy of every submitted weekly report is emailed here.';

  @override
  String get companySettingsSaved => 'Company profile saved.';

  @override
  String get ledgersTitle => 'Ledger';

  @override
  String get ledgersEmpty => 'No ledgers yet.';

  @override
  String get ledgersSearchHint => 'Name…';

  @override
  String get ledgersSectionsTitle => 'Sections';

  @override
  String get ledgersSectionEmpty => 'No rows in this section.';

  @override
  String ledgersRowCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count rows',
      one: '1 row',
    );
    return '$_temp0';
  }

  @override
  String get ledgersNetTotal => 'Net total';

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

  @override
  String get ackBannerHeading => 'Confirmation needed';

  @override
  String get ackConfirmButton => 'I\'ve seen this';

  @override
  String get ackGatedMessage => 'Confirm the pending notification first';
}
