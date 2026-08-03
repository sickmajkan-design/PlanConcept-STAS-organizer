import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_en.dart';
import 'app_localizations_sr.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations)!;
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('en'),
    Locale('sr'),
  ];

  /// No description provided for @appName.
  ///
  /// In en, this message translates to:
  /// **'Construction Organizer'**
  String get appName;

  /// No description provided for @commonCancel.
  ///
  /// In en, this message translates to:
  /// **'Cancel'**
  String get commonCancel;

  /// No description provided for @commonRetry.
  ///
  /// In en, this message translates to:
  /// **'Try again'**
  String get commonRetry;

  /// No description provided for @commonLoadMore.
  ///
  /// In en, this message translates to:
  /// **'Load more'**
  String get commonLoadMore;

  /// No description provided for @commonSignIn.
  ///
  /// In en, this message translates to:
  /// **'Sign in'**
  String get commonSignIn;

  /// No description provided for @commonSignOut.
  ///
  /// In en, this message translates to:
  /// **'Sign out'**
  String get commonSignOut;

  /// No description provided for @commonSignOutQuestion.
  ///
  /// In en, this message translates to:
  /// **'Sign out?'**
  String get commonSignOutQuestion;

  /// No description provided for @commonSignOutBody.
  ///
  /// In en, this message translates to:
  /// **'You will need to sign in again to use the app.'**
  String get commonSignOutBody;

  /// No description provided for @commonNotSet.
  ///
  /// In en, this message translates to:
  /// **'—'**
  String get commonNotSet;

  /// No description provided for @commonDetails.
  ///
  /// In en, this message translates to:
  /// **'Details'**
  String get commonDetails;

  /// No description provided for @commonAssignment.
  ///
  /// In en, this message translates to:
  /// **'Assignment'**
  String get commonAssignment;

  /// No description provided for @commonContact.
  ///
  /// In en, this message translates to:
  /// **'Contact'**
  String get commonContact;

  /// No description provided for @commonEmployment.
  ///
  /// In en, this message translates to:
  /// **'Employment'**
  String get commonEmployment;

  /// No description provided for @commonAccount.
  ///
  /// In en, this message translates to:
  /// **'Account'**
  String get commonAccount;

  /// No description provided for @commonResources.
  ///
  /// In en, this message translates to:
  /// **'Resources'**
  String get commonResources;

  /// No description provided for @commonAlerts.
  ///
  /// In en, this message translates to:
  /// **'Alerts'**
  String get commonAlerts;

  /// No description provided for @authSignInSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Sign in to your work account'**
  String get authSignInSubtitle;

  /// No description provided for @authEmail.
  ///
  /// In en, this message translates to:
  /// **'Email'**
  String get authEmail;

  /// No description provided for @authPassword.
  ///
  /// In en, this message translates to:
  /// **'Password'**
  String get authPassword;

  /// No description provided for @authCurrentPassword.
  ///
  /// In en, this message translates to:
  /// **'Current password'**
  String get authCurrentPassword;

  /// No description provided for @authNewPassword.
  ///
  /// In en, this message translates to:
  /// **'New password'**
  String get authNewPassword;

  /// No description provided for @authConfirmPassword.
  ///
  /// In en, this message translates to:
  /// **'Confirm new password'**
  String get authConfirmPassword;

  /// No description provided for @authShowPassword.
  ///
  /// In en, this message translates to:
  /// **'Show password'**
  String get authShowPassword;

  /// No description provided for @authHidePassword.
  ///
  /// In en, this message translates to:
  /// **'Hide password'**
  String get authHidePassword;

  /// No description provided for @authForgotPassword.
  ///
  /// In en, this message translates to:
  /// **'Forgot password?'**
  String get authForgotPassword;

  /// No description provided for @authResetPassword.
  ///
  /// In en, this message translates to:
  /// **'Reset password'**
  String get authResetPassword;

  /// No description provided for @authResetIntro.
  ///
  /// In en, this message translates to:
  /// **'Enter the email address of your work account. If an account exists, we will send a link to choose a new password.'**
  String get authResetIntro;

  /// No description provided for @authResetSent.
  ///
  /// In en, this message translates to:
  /// **'If that address belongs to an account, a reset link is on its way.'**
  String get authResetSent;

  /// No description provided for @authSendResetLink.
  ///
  /// In en, this message translates to:
  /// **'Send reset link'**
  String get authSendResetLink;

  /// No description provided for @authSendAgain.
  ///
  /// In en, this message translates to:
  /// **'Send again'**
  String get authSendAgain;

  /// No description provided for @authChangePassword.
  ///
  /// In en, this message translates to:
  /// **'Change password'**
  String get authChangePassword;

  /// No description provided for @authPasswordChanged.
  ///
  /// In en, this message translates to:
  /// **'Password changed'**
  String get authPasswordChanged;

  /// No description provided for @authPasswordChangedBody.
  ///
  /// In en, this message translates to:
  /// **'Your password has been updated. For security, all your signed-in devices have been signed out.'**
  String get authPasswordChangedBody;

  /// No description provided for @authSignInAgain.
  ///
  /// In en, this message translates to:
  /// **'Sign in again'**
  String get authSignInAgain;

  /// No description provided for @authSessionExpired.
  ///
  /// In en, this message translates to:
  /// **'Your session has expired. Please sign in again.'**
  String get authSessionExpired;

  /// No description provided for @validationEmailRequired.
  ///
  /// In en, this message translates to:
  /// **'Email is required.'**
  String get validationEmailRequired;

  /// No description provided for @validationEmailInvalid.
  ///
  /// In en, this message translates to:
  /// **'Enter a valid email address.'**
  String get validationEmailInvalid;

  /// No description provided for @validationPasswordRequired.
  ///
  /// In en, this message translates to:
  /// **'Password is required.'**
  String get validationPasswordRequired;

  /// No description provided for @validationPasswordUpper.
  ///
  /// In en, this message translates to:
  /// **'Password must contain an upper-case letter.'**
  String get validationPasswordUpper;

  /// No description provided for @validationPasswordLower.
  ///
  /// In en, this message translates to:
  /// **'Password must contain a lower-case letter.'**
  String get validationPasswordLower;

  /// No description provided for @validationPasswordDigit.
  ///
  /// In en, this message translates to:
  /// **'Password must contain a digit.'**
  String get validationPasswordDigit;

  /// No description provided for @validationPasswordsDiffer.
  ///
  /// In en, this message translates to:
  /// **'The passwords do not match.'**
  String get validationPasswordsDiffer;

  /// No description provided for @validationConfirmPassword.
  ///
  /// In en, this message translates to:
  /// **'Confirm the new password.'**
  String get validationConfirmPassword;

  /// No description provided for @errorNoConnection.
  ///
  /// In en, this message translates to:
  /// **'No connection to the server. Check your network and try again.'**
  String get errorNoConnection;

  /// No description provided for @errorTimeout.
  ///
  /// In en, this message translates to:
  /// **'The server took too long to respond. Please try again.'**
  String get errorTimeout;

  /// No description provided for @errorCancelled.
  ///
  /// In en, this message translates to:
  /// **'The request was cancelled.'**
  String get errorCancelled;

  /// No description provided for @errorCertificate.
  ///
  /// In en, this message translates to:
  /// **'The server certificate could not be verified.'**
  String get errorCertificate;

  /// No description provided for @errorServer.
  ///
  /// In en, this message translates to:
  /// **'The server encountered an error. Please try again later.'**
  String get errorServer;

  /// No description provided for @errorNotFound.
  ///
  /// In en, this message translates to:
  /// **'The requested item could not be found.'**
  String get errorNotFound;

  /// No description provided for @errorForbidden.
  ///
  /// In en, this message translates to:
  /// **'You do not have permission to perform this action.'**
  String get errorForbidden;

  /// No description provided for @errorBadRequest.
  ///
  /// In en, this message translates to:
  /// **'The request was rejected. Please check the entered data.'**
  String get errorBadRequest;

  /// No description provided for @errorConflict.
  ///
  /// In en, this message translates to:
  /// **'The action conflicts with the current data.'**
  String get errorConflict;

  /// No description provided for @errorUnknown.
  ///
  /// In en, this message translates to:
  /// **'Something went wrong. Please try again.'**
  String get errorUnknown;

  /// No description provided for @navEmployees.
  ///
  /// In en, this message translates to:
  /// **'Employees'**
  String get navEmployees;

  /// No description provided for @navProjects.
  ///
  /// In en, this message translates to:
  /// **'Projects'**
  String get navProjects;

  /// No description provided for @navVehicles.
  ///
  /// In en, this message translates to:
  /// **'Vehicles'**
  String get navVehicles;

  /// No description provided for @navTools.
  ///
  /// In en, this message translates to:
  /// **'Tools'**
  String get navTools;

  /// No description provided for @navMaterials.
  ///
  /// In en, this message translates to:
  /// **'Materials'**
  String get navMaterials;

  /// No description provided for @navNotifications.
  ///
  /// In en, this message translates to:
  /// **'Notifications'**
  String get navNotifications;

  /// No description provided for @employeesSearchHint.
  ///
  /// In en, this message translates to:
  /// **'Name, number, position…'**
  String get employeesSearchHint;

  /// No description provided for @employeesEmpty.
  ///
  /// In en, this message translates to:
  /// **'No employees match your search.'**
  String get employeesEmpty;

  /// No description provided for @employeeNumber.
  ///
  /// In en, this message translates to:
  /// **'Employee number'**
  String get employeeNumber;

  /// No description provided for @employeePosition.
  ///
  /// In en, this message translates to:
  /// **'Position'**
  String get employeePosition;

  /// No description provided for @employeePhone.
  ///
  /// In en, this message translates to:
  /// **'Phone'**
  String get employeePhone;

  /// No description provided for @employeeEmail.
  ///
  /// In en, this message translates to:
  /// **'Email'**
  String get employeeEmail;

  /// No description provided for @employeeAddress.
  ///
  /// In en, this message translates to:
  /// **'Address'**
  String get employeeAddress;

  /// No description provided for @employeeDateOfBirth.
  ///
  /// In en, this message translates to:
  /// **'Date of birth'**
  String get employeeDateOfBirth;

  /// No description provided for @employeeEmployedSince.
  ///
  /// In en, this message translates to:
  /// **'Employed since'**
  String get employeeEmployedSince;

  /// No description provided for @employeeAppAccount.
  ///
  /// In en, this message translates to:
  /// **'App account'**
  String get employeeAppAccount;

  /// No description provided for @employeeNoProjects.
  ///
  /// In en, this message translates to:
  /// **'Not assigned to any project'**
  String get employeeNoProjects;

  /// No description provided for @projectsSearchHint.
  ///
  /// In en, this message translates to:
  /// **'Name, client, address…'**
  String get projectsSearchHint;

  /// No description provided for @projectsEmpty.
  ///
  /// In en, this message translates to:
  /// **'No projects match your search.'**
  String get projectsEmpty;

  /// No description provided for @projectClient.
  ///
  /// In en, this message translates to:
  /// **'Client'**
  String get projectClient;

  /// No description provided for @projectAddress.
  ///
  /// In en, this message translates to:
  /// **'Address'**
  String get projectAddress;

  /// No description provided for @projectStartDate.
  ///
  /// In en, this message translates to:
  /// **'Start date'**
  String get projectStartDate;

  /// No description provided for @projectEndDate.
  ///
  /// In en, this message translates to:
  /// **'End date'**
  String get projectEndDate;

  /// No description provided for @projectCoordinates.
  ///
  /// In en, this message translates to:
  /// **'Coordinates'**
  String get projectCoordinates;

  /// No description provided for @projectCrewEmpty.
  ///
  /// In en, this message translates to:
  /// **'Nobody assigned yet'**
  String get projectCrewEmpty;

  /// No description provided for @projectAssignedOn.
  ///
  /// In en, this message translates to:
  /// **'Assigned {date}'**
  String projectAssignedOn(String date);

  /// No description provided for @projectMemberSubtitle.
  ///
  /// In en, this message translates to:
  /// **'{position} · {number}'**
  String projectMemberSubtitle(String position, String number);

  /// No description provided for @vehiclesSearchHint.
  ///
  /// In en, this message translates to:
  /// **'Brand, model, registration…'**
  String get vehiclesSearchHint;

  /// No description provided for @vehiclesEmpty.
  ///
  /// In en, this message translates to:
  /// **'No vehicles match your search.'**
  String get vehiclesEmpty;

  /// No description provided for @vehicleRegistration.
  ///
  /// In en, this message translates to:
  /// **'Registration number'**
  String get vehicleRegistration;

  /// No description provided for @vehicleFuelType.
  ///
  /// In en, this message translates to:
  /// **'Fuel type'**
  String get vehicleFuelType;

  /// No description provided for @vehicleUnassigned.
  ///
  /// In en, this message translates to:
  /// **'Not assigned to any employee'**
  String get vehicleUnassigned;

  /// No description provided for @toolsSearchHint.
  ///
  /// In en, this message translates to:
  /// **'Name, category, serial number…'**
  String get toolsSearchHint;

  /// No description provided for @toolsEmpty.
  ///
  /// In en, this message translates to:
  /// **'No tools match your search.'**
  String get toolsEmpty;

  /// No description provided for @toolSerialNumber.
  ///
  /// In en, this message translates to:
  /// **'Serial number'**
  String get toolSerialNumber;

  /// No description provided for @toolQrCode.
  ///
  /// In en, this message translates to:
  /// **'QR code'**
  String get toolQrCode;

  /// No description provided for @toolUncategorised.
  ///
  /// In en, this message translates to:
  /// **'Uncategorised'**
  String get toolUncategorised;

  /// No description provided for @toolNotHeld.
  ///
  /// In en, this message translates to:
  /// **'Not held by an employee'**
  String get toolNotHeld;

  /// No description provided for @toolNotOnProject.
  ///
  /// In en, this message translates to:
  /// **'Not assigned to any project'**
  String get toolNotOnProject;

  /// No description provided for @toolLookUp.
  ///
  /// In en, this message translates to:
  /// **'Look up a tool'**
  String get toolLookUp;

  /// No description provided for @toolLookUpAction.
  ///
  /// In en, this message translates to:
  /// **'Look up'**
  String get toolLookUpAction;

  /// No description provided for @toolLookUpByQr.
  ///
  /// In en, this message translates to:
  /// **'Look up by QR code'**
  String get toolLookUpByQr;

  /// No description provided for @toolByQrCode.
  ///
  /// In en, this message translates to:
  /// **'By QR code'**
  String get toolByQrCode;

  /// No description provided for @toolCategoryLine.
  ///
  /// In en, this message translates to:
  /// **'Category: {category}'**
  String toolCategoryLine(String category);

  /// No description provided for @toolSerialLine.
  ///
  /// In en, this message translates to:
  /// **'Serial number: {serial}'**
  String toolSerialLine(String serial);

  /// No description provided for @materialsSearchHint.
  ///
  /// In en, this message translates to:
  /// **'Name, warehouse…'**
  String get materialsSearchHint;

  /// No description provided for @materialsEmpty.
  ///
  /// In en, this message translates to:
  /// **'No materials match your search.'**
  String get materialsEmpty;

  /// No description provided for @materialStock.
  ///
  /// In en, this message translates to:
  /// **'Stock'**
  String get materialStock;

  /// No description provided for @materialWarehouse.
  ///
  /// In en, this message translates to:
  /// **'Warehouse'**
  String get materialWarehouse;

  /// No description provided for @materialWarehouseStock.
  ///
  /// In en, this message translates to:
  /// **'Warehouse stock'**
  String get materialWarehouseStock;

  /// No description provided for @materialWarehouseOnly.
  ///
  /// In en, this message translates to:
  /// **'Warehouse stock only'**
  String get materialWarehouseOnly;

  /// No description provided for @materialWarehouseNote.
  ///
  /// In en, this message translates to:
  /// **'Warehouse stock, not tied to a project'**
  String get materialWarehouseNote;

  /// No description provided for @materialLastUpdated.
  ///
  /// In en, this message translates to:
  /// **'Last updated'**
  String get materialLastUpdated;

  /// No description provided for @materialNoAssignment.
  ///
  /// In en, this message translates to:
  /// **'No assignment'**
  String get materialNoAssignment;

  /// No description provided for @notificationsEmpty.
  ///
  /// In en, this message translates to:
  /// **'No notifications yet.'**
  String get notificationsEmpty;

  /// No description provided for @notificationsUnreadEmpty.
  ///
  /// In en, this message translates to:
  /// **'Nothing unread.'**
  String get notificationsUnreadEmpty;

  /// No description provided for @notificationsUnread.
  ///
  /// In en, this message translates to:
  /// **'Unread'**
  String get notificationsUnread;

  /// No description provided for @notificationsMarkAllRead.
  ///
  /// In en, this message translates to:
  /// **'Mark all read'**
  String get notificationsMarkAllRead;

  /// No description provided for @notificationsDisabled.
  ///
  /// In en, this message translates to:
  /// **'Notifications are turned off for this app.'**
  String get notificationsDisabled;

  /// No description provided for @notificationsNotConfigured.
  ///
  /// In en, this message translates to:
  /// **'Push notifications are not configured in this build.'**
  String get notificationsNotConfigured;

  /// No description provided for @notificationsNotConfiguredBody.
  ///
  /// In en, this message translates to:
  /// **'Push delivery is not configured in this build. Notifications are still listed here.'**
  String get notificationsNotConfiguredBody;

  /// No description provided for @notificationsBlockedBody.
  ///
  /// In en, this message translates to:
  /// **'Push notifications are turned off for this app. You can still read them here.'**
  String get notificationsBlockedBody;

  /// No description provided for @notificationsOpenSettings.
  ///
  /// In en, this message translates to:
  /// **'Open app settings'**
  String get notificationsOpenSettings;

  /// No description provided for @notificationsTokenFailed.
  ///
  /// In en, this message translates to:
  /// **'Could not obtain a device token.'**
  String get notificationsTokenFailed;

  /// No description provided for @notificationsFirebaseFailed.
  ///
  /// In en, this message translates to:
  /// **'Firebase messaging failed.'**
  String get notificationsFirebaseFailed;

  /// No description provided for @locationSharingOn.
  ///
  /// In en, this message translates to:
  /// **'Location sharing is on'**
  String get locationSharingOn;

  /// No description provided for @locationSharingOnBody.
  ///
  /// In en, this message translates to:
  /// **'Your position is sent to the office every minute while you are signed in.'**
  String get locationSharingOnBody;

  /// No description provided for @locationStarting.
  ///
  /// In en, this message translates to:
  /// **'Starting location sharing…'**
  String get locationStarting;

  /// No description provided for @locationProblem.
  ///
  /// In en, this message translates to:
  /// **'Location sharing has a problem'**
  String get locationProblem;

  /// No description provided for @locationNotShared.
  ///
  /// In en, this message translates to:
  /// **'Your position is not being shared with the office.'**
  String get locationNotShared;

  /// No description provided for @locationServicesOff.
  ///
  /// In en, this message translates to:
  /// **'Location services are switched off'**
  String get locationServicesOff;

  /// No description provided for @locationPermissionDenied.
  ///
  /// In en, this message translates to:
  /// **'Location permission not granted'**
  String get locationPermissionDenied;

  /// No description provided for @locationPermissionBlocked.
  ///
  /// In en, this message translates to:
  /// **'Location permission is blocked'**
  String get locationPermissionBlocked;

  /// No description provided for @locationAllow.
  ///
  /// In en, this message translates to:
  /// **'Allow location'**
  String get locationAllow;

  /// No description provided for @locationOpenSettings.
  ///
  /// In en, this message translates to:
  /// **'Open location settings'**
  String get locationOpenSettings;

  /// No description provided for @locationNoFix.
  ///
  /// In en, this message translates to:
  /// **'No GPS fix yet.'**
  String get locationNoFix;

  /// No description provided for @locationReadFailed.
  ///
  /// In en, this message translates to:
  /// **'Could not read the device location.'**
  String get locationReadFailed;

  /// No description provided for @locationQueued.
  ///
  /// In en, this message translates to:
  /// **'Queued — {reason}'**
  String locationQueued(String reason);

  /// No description provided for @locationServiceNotificationTitle.
  ///
  /// In en, this message translates to:
  /// **'Sharing your location'**
  String get locationServiceNotificationTitle;

  /// No description provided for @locationServiceNotificationBody.
  ///
  /// In en, this message translates to:
  /// **'The office can see which site you are on. Sign out to stop.'**
  String get locationServiceNotificationBody;

  /// No description provided for @locationServiceChannelName.
  ///
  /// In en, this message translates to:
  /// **'Location sharing'**
  String get locationServiceChannelName;

  /// No description provided for @locationPending.
  ///
  /// In en, this message translates to:
  /// **'{count, plural, =0{Everything sent} one{{count} reading waiting to be sent} other{{count} readings waiting to be sent}}'**
  String locationPending(int count);

  /// No description provided for @locationLastSent.
  ///
  /// In en, this message translates to:
  /// **'Last sent {when}.'**
  String locationLastSent(String when);

  /// No description provided for @locationOpenAppSettings.
  ///
  /// In en, this message translates to:
  /// **'Open app settings'**
  String get locationOpenAppSettings;

  /// No description provided for @roleSuperAdmin.
  ///
  /// In en, this message translates to:
  /// **'Super Admin'**
  String get roleSuperAdmin;

  /// No description provided for @roleAdmin.
  ///
  /// In en, this message translates to:
  /// **'Admin'**
  String get roleAdmin;

  /// No description provided for @roleProjectManager.
  ///
  /// In en, this message translates to:
  /// **'Project Manager'**
  String get roleProjectManager;

  /// No description provided for @roleForeman.
  ///
  /// In en, this message translates to:
  /// **'Foreman'**
  String get roleForeman;

  /// No description provided for @roleWorker.
  ///
  /// In en, this message translates to:
  /// **'Worker'**
  String get roleWorker;

  /// No description provided for @employeeStatusActive.
  ///
  /// In en, this message translates to:
  /// **'Active'**
  String get employeeStatusActive;

  /// No description provided for @employeeStatusOnLeave.
  ///
  /// In en, this message translates to:
  /// **'On leave'**
  String get employeeStatusOnLeave;

  /// No description provided for @employeeStatusSuspended.
  ///
  /// In en, this message translates to:
  /// **'Suspended'**
  String get employeeStatusSuspended;

  /// No description provided for @employeeStatusTerminated.
  ///
  /// In en, this message translates to:
  /// **'Terminated'**
  String get employeeStatusTerminated;

  /// No description provided for @projectStatusPlanned.
  ///
  /// In en, this message translates to:
  /// **'Planned'**
  String get projectStatusPlanned;

  /// No description provided for @projectStatusActive.
  ///
  /// In en, this message translates to:
  /// **'Active'**
  String get projectStatusActive;

  /// No description provided for @projectStatusOnHold.
  ///
  /// In en, this message translates to:
  /// **'On hold'**
  String get projectStatusOnHold;

  /// No description provided for @projectStatusCompleted.
  ///
  /// In en, this message translates to:
  /// **'Completed'**
  String get projectStatusCompleted;

  /// No description provided for @projectStatusCancelled.
  ///
  /// In en, this message translates to:
  /// **'Cancelled'**
  String get projectStatusCancelled;

  /// No description provided for @vehicleStatusAvailable.
  ///
  /// In en, this message translates to:
  /// **'Available'**
  String get vehicleStatusAvailable;

  /// No description provided for @vehicleStatusAssigned.
  ///
  /// In en, this message translates to:
  /// **'Assigned'**
  String get vehicleStatusAssigned;

  /// No description provided for @vehicleStatusInService.
  ///
  /// In en, this message translates to:
  /// **'In service'**
  String get vehicleStatusInService;

  /// No description provided for @vehicleStatusOutOfService.
  ///
  /// In en, this message translates to:
  /// **'Out of service'**
  String get vehicleStatusOutOfService;

  /// No description provided for @toolStatusAvailable.
  ///
  /// In en, this message translates to:
  /// **'Available'**
  String get toolStatusAvailable;

  /// No description provided for @toolStatusAssigned.
  ///
  /// In en, this message translates to:
  /// **'Assigned'**
  String get toolStatusAssigned;

  /// No description provided for @toolStatusUnderRepair.
  ///
  /// In en, this message translates to:
  /// **'Under repair'**
  String get toolStatusUnderRepair;

  /// No description provided for @toolStatusLost.
  ///
  /// In en, this message translates to:
  /// **'Lost'**
  String get toolStatusLost;

  /// No description provided for @toolStatusRetired.
  ///
  /// In en, this message translates to:
  /// **'Retired'**
  String get toolStatusRetired;

  /// No description provided for @fuelPetrol.
  ///
  /// In en, this message translates to:
  /// **'Petrol'**
  String get fuelPetrol;

  /// No description provided for @fuelDiesel.
  ///
  /// In en, this message translates to:
  /// **'Diesel'**
  String get fuelDiesel;

  /// No description provided for @fuelElectric.
  ///
  /// In en, this message translates to:
  /// **'Electric'**
  String get fuelElectric;

  /// No description provided for @fuelHybrid.
  ///
  /// In en, this message translates to:
  /// **'Hybrid'**
  String get fuelHybrid;

  /// No description provided for @fuelLpg.
  ///
  /// In en, this message translates to:
  /// **'LPG'**
  String get fuelLpg;

  /// No description provided for @notificationTypeEmployeeAssigned.
  ///
  /// In en, this message translates to:
  /// **'Employee assigned'**
  String get notificationTypeEmployeeAssigned;

  /// No description provided for @notificationTypeProjectAssigned.
  ///
  /// In en, this message translates to:
  /// **'Project assigned'**
  String get notificationTypeProjectAssigned;

  /// No description provided for @notificationTypeToolAssigned.
  ///
  /// In en, this message translates to:
  /// **'Tool assigned'**
  String get notificationTypeToolAssigned;

  /// No description provided for @notificationTypeVehicleAssigned.
  ///
  /// In en, this message translates to:
  /// **'Vehicle assigned'**
  String get notificationTypeVehicleAssigned;

  /// No description provided for @notificationTypeAnnouncement.
  ///
  /// In en, this message translates to:
  /// **'Announcement'**
  String get notificationTypeAnnouncement;

  /// No description provided for @settingsLanguage.
  ///
  /// In en, this message translates to:
  /// **'Language'**
  String get settingsLanguage;

  /// No description provided for @settingsLanguageSerbian.
  ///
  /// In en, this message translates to:
  /// **'Srpski'**
  String get settingsLanguageSerbian;

  /// No description provided for @settingsLanguageEnglish.
  ///
  /// In en, this message translates to:
  /// **'English'**
  String get settingsLanguageEnglish;

  /// No description provided for @navTimeEntries.
  ///
  /// In en, this message translates to:
  /// **'Work time'**
  String get navTimeEntries;

  /// No description provided for @shiftTitle.
  ///
  /// In en, this message translates to:
  /// **'My work time'**
  String get shiftTitle;

  /// No description provided for @shiftRunning.
  ///
  /// In en, this message translates to:
  /// **'You are clocked in'**
  String get shiftRunning;

  /// No description provided for @shiftOff.
  ///
  /// In en, this message translates to:
  /// **'You are not clocked in'**
  String get shiftOff;

  /// No description provided for @shiftSince.
  ///
  /// In en, this message translates to:
  /// **'Since {time}'**
  String shiftSince(String time);

  /// No description provided for @shiftElapsed.
  ///
  /// In en, this message translates to:
  /// **'{hours} h {minutes} min'**
  String shiftElapsed(int hours, int minutes);

  /// No description provided for @shiftClockIn.
  ///
  /// In en, this message translates to:
  /// **'Clock in'**
  String get shiftClockIn;

  /// No description provided for @shiftClockOut.
  ///
  /// In en, this message translates to:
  /// **'Clock out'**
  String get shiftClockOut;

  /// No description provided for @shiftClockOutTitle.
  ///
  /// In en, this message translates to:
  /// **'End the shift'**
  String get shiftClockOutTitle;

  /// No description provided for @shiftBreakLabel.
  ///
  /// In en, this message translates to:
  /// **'Unpaid break (minutes)'**
  String get shiftBreakLabel;

  /// No description provided for @shiftBreakHint.
  ///
  /// In en, this message translates to:
  /// **'Leave at 0 if you did not take one.'**
  String get shiftBreakHint;

  /// No description provided for @shiftProject.
  ///
  /// In en, this message translates to:
  /// **'Site'**
  String get shiftProject;

  /// No description provided for @shiftNoProject.
  ///
  /// In en, this message translates to:
  /// **'No site'**
  String get shiftNoProject;

  /// No description provided for @shiftWorkType.
  ///
  /// In en, this message translates to:
  /// **'Type of work'**
  String get shiftWorkType;

  /// No description provided for @shiftConfirm.
  ///
  /// In en, this message translates to:
  /// **'Confirm'**
  String get shiftConfirm;

  /// No description provided for @shiftHistory.
  ///
  /// In en, this message translates to:
  /// **'Recent entries'**
  String get shiftHistory;

  /// No description provided for @shiftHistoryEmpty.
  ///
  /// In en, this message translates to:
  /// **'No hours recorded yet.'**
  String get shiftHistoryEmpty;

  /// No description provided for @shiftWorked.
  ///
  /// In en, this message translates to:
  /// **'Worked'**
  String get shiftWorked;

  /// No description provided for @shiftBreak.
  ///
  /// In en, this message translates to:
  /// **'Break'**
  String get shiftBreak;

  /// No description provided for @shiftBreakMinutes.
  ///
  /// In en, this message translates to:
  /// **'{count, plural, =0{No break} one{{count} minute} few{{count} minutes} other{{count} minutes}}'**
  String shiftBreakMinutes(int count);

  /// No description provided for @shiftSentBack.
  ///
  /// In en, this message translates to:
  /// **'Sent back: {reason}'**
  String shiftSentBack(String reason);

  /// No description provided for @shiftNotAnEmployee.
  ///
  /// In en, this message translates to:
  /// **'This account is not linked to an employee, so it cannot record work time.'**
  String get shiftNotAnEmployee;

  /// No description provided for @timeEntryStatusInProgress.
  ///
  /// In en, this message translates to:
  /// **'Running'**
  String get timeEntryStatusInProgress;

  /// No description provided for @timeEntryStatusSubmitted.
  ///
  /// In en, this message translates to:
  /// **'Awaiting review'**
  String get timeEntryStatusSubmitted;

  /// No description provided for @timeEntryStatusApproved.
  ///
  /// In en, this message translates to:
  /// **'Approved'**
  String get timeEntryStatusApproved;

  /// No description provided for @timeEntryStatusRejected.
  ///
  /// In en, this message translates to:
  /// **'Sent back'**
  String get timeEntryStatusRejected;

  /// No description provided for @workTypeRegular.
  ///
  /// In en, this message translates to:
  /// **'Regular'**
  String get workTypeRegular;

  /// No description provided for @workTypeOvertime.
  ///
  /// In en, this message translates to:
  /// **'Overtime'**
  String get workTypeOvertime;

  /// No description provided for @workTypeWeekend.
  ///
  /// In en, this message translates to:
  /// **'Weekend'**
  String get workTypeWeekend;

  /// No description provided for @workTypePublicHoliday.
  ///
  /// In en, this message translates to:
  /// **'Public holiday'**
  String get workTypePublicHoliday;

  /// No description provided for @workTypeTravel.
  ///
  /// In en, this message translates to:
  /// **'Travel'**
  String get workTypeTravel;

  /// No description provided for @attachmentsTitle.
  ///
  /// In en, this message translates to:
  /// **'Documents'**
  String get attachmentsTitle;

  /// No description provided for @attachmentsEmpty.
  ///
  /// In en, this message translates to:
  /// **'No documents on this record.'**
  String get attachmentsEmpty;

  /// No description provided for @attachmentsExpired.
  ///
  /// In en, this message translates to:
  /// **'Expired'**
  String get attachmentsExpired;

  /// No description provided for @attachmentsExpiresOn.
  ///
  /// In en, this message translates to:
  /// **'Valid until {date}'**
  String attachmentsExpiresOn(String date);

  /// No description provided for @attachmentsOpenFailed.
  ///
  /// In en, this message translates to:
  /// **'The file could not be opened.'**
  String get attachmentsOpenFailed;

  /// No description provided for @attachmentsAddPhoto.
  ///
  /// In en, this message translates to:
  /// **'Add a photo'**
  String get attachmentsAddPhoto;

  /// No description provided for @attachmentsTakePhoto.
  ///
  /// In en, this message translates to:
  /// **'Take a photo'**
  String get attachmentsTakePhoto;

  /// No description provided for @attachmentsFromGallery.
  ///
  /// In en, this message translates to:
  /// **'Choose from gallery'**
  String get attachmentsFromGallery;

  /// No description provided for @attachmentsPhotoNote.
  ///
  /// In en, this message translates to:
  /// **'Note (optional)'**
  String get attachmentsPhotoNote;

  /// No description provided for @attachmentsUploading.
  ///
  /// In en, this message translates to:
  /// **'Uploading…'**
  String get attachmentsUploading;

  /// No description provided for @attachmentsUploaded.
  ///
  /// In en, this message translates to:
  /// **'Photo added.'**
  String get attachmentsUploaded;

  /// No description provided for @attachmentsTooLarge.
  ///
  /// In en, this message translates to:
  /// **'The photo is larger than the {limit} MB limit.'**
  String attachmentsTooLarge(int limit);

  /// No description provided for @attachmentsNotAnImage.
  ///
  /// In en, this message translates to:
  /// **'Only a document or image can be attached.'**
  String get attachmentsNotAnImage;

  /// No description provided for @attachmentCategoryContract.
  ///
  /// In en, this message translates to:
  /// **'Contract'**
  String get attachmentCategoryContract;

  /// No description provided for @attachmentCategoryCertificate.
  ///
  /// In en, this message translates to:
  /// **'Certificate'**
  String get attachmentCategoryCertificate;

  /// No description provided for @attachmentCategoryMedicalCheck.
  ///
  /// In en, this message translates to:
  /// **'Medical check'**
  String get attachmentCategoryMedicalCheck;

  /// No description provided for @attachmentCategoryLicence.
  ///
  /// In en, this message translates to:
  /// **'Licence'**
  String get attachmentCategoryLicence;

  /// No description provided for @attachmentCategoryInsurance.
  ///
  /// In en, this message translates to:
  /// **'Insurance'**
  String get attachmentCategoryInsurance;

  /// No description provided for @attachmentCategorySiteDocument.
  ///
  /// In en, this message translates to:
  /// **'Site document'**
  String get attachmentCategorySiteDocument;

  /// No description provided for @attachmentCategoryPhoto.
  ///
  /// In en, this message translates to:
  /// **'Photo'**
  String get attachmentCategoryPhoto;

  /// No description provided for @attachmentCategoryOther.
  ///
  /// In en, this message translates to:
  /// **'Other'**
  String get attachmentCategoryOther;

  /// No description provided for @navWorkItems.
  ///
  /// In en, this message translates to:
  /// **'My work'**
  String get navWorkItems;

  /// No description provided for @workItemsEmpty.
  ///
  /// In en, this message translates to:
  /// **'Nothing on your list.'**
  String get workItemsEmpty;

  /// No description provided for @workItemsIncludeFinished.
  ///
  /// In en, this message translates to:
  /// **'Include finished'**
  String get workItemsIncludeFinished;

  /// No description provided for @workItemsDue.
  ///
  /// In en, this message translates to:
  /// **'Due {date}'**
  String workItemsDue(String date);

  /// No description provided for @workItemsOverdue.
  ///
  /// In en, this message translates to:
  /// **'Overdue'**
  String get workItemsOverdue;

  /// No description provided for @workItemsNoDueDate.
  ///
  /// In en, this message translates to:
  /// **'No deadline'**
  String get workItemsNoDueDate;

  /// No description provided for @workItemsNoProject.
  ///
  /// In en, this message translates to:
  /// **'No site'**
  String get workItemsNoProject;

  /// No description provided for @workItemsReportDefect.
  ///
  /// In en, this message translates to:
  /// **'Report a defect'**
  String get workItemsReportDefect;

  /// No description provided for @workItemsDefectTitle.
  ///
  /// In en, this message translates to:
  /// **'What is wrong'**
  String get workItemsDefectTitle;

  /// No description provided for @workItemsDefectDescription.
  ///
  /// In en, this message translates to:
  /// **'Details (optional)'**
  String get workItemsDefectDescription;

  /// No description provided for @workItemsDefectSend.
  ///
  /// In en, this message translates to:
  /// **'Report'**
  String get workItemsDefectSend;

  /// No description provided for @workItemsDefectSent.
  ///
  /// In en, this message translates to:
  /// **'Defect reported.'**
  String get workItemsDefectSent;

  /// No description provided for @workItemsDefectNeedsTitle.
  ///
  /// In en, this message translates to:
  /// **'Describe the problem in a few words.'**
  String get workItemsDefectNeedsTitle;

  /// No description provided for @workItemsPhotoCount.
  ///
  /// In en, this message translates to:
  /// **'{count, plural, =0{No photos} one{{count} photo} other{{count} photos}}'**
  String workItemsPhotoCount(int count);

  /// No description provided for @workItemKindTask.
  ///
  /// In en, this message translates to:
  /// **'Task'**
  String get workItemKindTask;

  /// No description provided for @workItemKindDefect.
  ///
  /// In en, this message translates to:
  /// **'Defect'**
  String get workItemKindDefect;

  /// No description provided for @workItemStatusOpen.
  ///
  /// In en, this message translates to:
  /// **'Open'**
  String get workItemStatusOpen;

  /// No description provided for @workItemStatusInProgress.
  ///
  /// In en, this message translates to:
  /// **'In progress'**
  String get workItemStatusInProgress;

  /// No description provided for @workItemStatusResolved.
  ///
  /// In en, this message translates to:
  /// **'Done, to check'**
  String get workItemStatusResolved;

  /// No description provided for @workItemStatusClosed.
  ///
  /// In en, this message translates to:
  /// **'Closed'**
  String get workItemStatusClosed;

  /// No description provided for @workItemStatusCancelled.
  ///
  /// In en, this message translates to:
  /// **'Cancelled'**
  String get workItemStatusCancelled;

  /// No description provided for @workItemPriorityLow.
  ///
  /// In en, this message translates to:
  /// **'Low'**
  String get workItemPriorityLow;

  /// No description provided for @workItemPriorityNormal.
  ///
  /// In en, this message translates to:
  /// **'Normal'**
  String get workItemPriorityNormal;

  /// No description provided for @workItemPriorityHigh.
  ///
  /// In en, this message translates to:
  /// **'High'**
  String get workItemPriorityHigh;

  /// No description provided for @workItemPriorityUrgent.
  ///
  /// In en, this message translates to:
  /// **'Urgent'**
  String get workItemPriorityUrgent;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['en', 'sr'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'en':
      return AppLocalizationsEn();
    case 'sr':
      return AppLocalizationsSr();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
