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

  /// No description provided for @commonDelete.
  ///
  /// In en, this message translates to:
  /// **'Delete'**
  String get commonDelete;

  /// No description provided for @commonEdit.
  ///
  /// In en, this message translates to:
  /// **'Edit'**
  String get commonEdit;

  /// No description provided for @commonAdd.
  ///
  /// In en, this message translates to:
  /// **'Add'**
  String get commonAdd;

  /// No description provided for @commonStatus.
  ///
  /// In en, this message translates to:
  /// **'Status'**
  String get commonStatus;

  /// No description provided for @vehicleFormAddTitle.
  ///
  /// In en, this message translates to:
  /// **'Add a vehicle'**
  String get vehicleFormAddTitle;

  /// No description provided for @vehicleFormEditTitle.
  ///
  /// In en, this message translates to:
  /// **'Edit vehicle'**
  String get vehicleFormEditTitle;

  /// No description provided for @vehicleFormBrand.
  ///
  /// In en, this message translates to:
  /// **'Brand'**
  String get vehicleFormBrand;

  /// No description provided for @vehicleFormModel.
  ///
  /// In en, this message translates to:
  /// **'Model'**
  String get vehicleFormModel;

  /// No description provided for @vehicleFormAdded.
  ///
  /// In en, this message translates to:
  /// **'Vehicle added.'**
  String get vehicleFormAdded;

  /// No description provided for @vehicleFormSaved.
  ///
  /// In en, this message translates to:
  /// **'Vehicle updated.'**
  String get vehicleFormSaved;

  /// No description provided for @vehicleDeleteTitle.
  ///
  /// In en, this message translates to:
  /// **'Delete this vehicle?'**
  String get vehicleDeleteTitle;

  /// No description provided for @vehicleDeleteBody.
  ///
  /// In en, this message translates to:
  /// **'{name} will be removed. This cannot be undone.'**
  String vehicleDeleteBody(String name);

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

  /// No description provided for @commonTotalCount.
  ///
  /// In en, this message translates to:
  /// **'{count} total'**
  String commonTotalCount(int count);

  /// No description provided for @commonCountOfTotal.
  ///
  /// In en, this message translates to:
  /// **'{shown} of {total}'**
  String commonCountOfTotal(int shown, int total);

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

  /// No description provided for @commonCompany.
  ///
  /// In en, this message translates to:
  /// **'Company'**
  String get commonCompany;

  /// No description provided for @commonEmployee.
  ///
  /// In en, this message translates to:
  /// **'Employee'**
  String get commonEmployee;

  /// No description provided for @commonMaterial.
  ///
  /// In en, this message translates to:
  /// **'Material'**
  String get commonMaterial;

  /// No description provided for @commonTool.
  ///
  /// In en, this message translates to:
  /// **'Tool'**
  String get commonTool;

  /// No description provided for @commonVehicle.
  ///
  /// In en, this message translates to:
  /// **'Vehicle'**
  String get commonVehicle;

  /// No description provided for @commonProject.
  ///
  /// In en, this message translates to:
  /// **'Project'**
  String get commonProject;

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

  /// No description provided for @validationPasswordMinLength.
  ///
  /// In en, this message translates to:
  /// **'Password must be at least 8 characters long.'**
  String get validationPasswordMinLength;

  /// No description provided for @validationFieldRequired.
  ///
  /// In en, this message translates to:
  /// **'{field} is required.'**
  String validationFieldRequired(String field);

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

  /// No description provided for @navHome.
  ///
  /// In en, this message translates to:
  /// **'Home'**
  String get navHome;

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

  /// No description provided for @projectSubOf.
  ///
  /// In en, this message translates to:
  /// **'Part of {name}'**
  String projectSubOf(String name);

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

  /// No description provided for @projectAssignedCount.
  ///
  /// In en, this message translates to:
  /// **'{count} assigned'**
  String projectAssignedCount(int count);

  /// No description provided for @projectCrewTitle.
  ///
  /// In en, this message translates to:
  /// **'Crew'**
  String get projectCrewTitle;

  /// No description provided for @projectCrewCount.
  ///
  /// In en, this message translates to:
  /// **'Crew ({count})'**
  String projectCrewCount(int count);

  /// No description provided for @employeeProjectsCount.
  ///
  /// In en, this message translates to:
  /// **'Projects ({count})'**
  String employeeProjectsCount(int count);

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

  /// No description provided for @vehicleOwnershipType.
  ///
  /// In en, this message translates to:
  /// **'Ownership'**
  String get vehicleOwnershipType;

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

  /// No description provided for @toolFormAddTitle.
  ///
  /// In en, this message translates to:
  /// **'Add a tool'**
  String get toolFormAddTitle;

  /// No description provided for @toolFormEditTitle.
  ///
  /// In en, this message translates to:
  /// **'Edit tool'**
  String get toolFormEditTitle;

  /// No description provided for @toolFormName.
  ///
  /// In en, this message translates to:
  /// **'Name'**
  String get toolFormName;

  /// No description provided for @toolFormCategory.
  ///
  /// In en, this message translates to:
  /// **'Category'**
  String get toolFormCategory;

  /// No description provided for @toolFormAdded.
  ///
  /// In en, this message translates to:
  /// **'Tool added.'**
  String get toolFormAdded;

  /// No description provided for @toolFormSaved.
  ///
  /// In en, this message translates to:
  /// **'Tool updated.'**
  String get toolFormSaved;

  /// No description provided for @toolDeleteTitle.
  ///
  /// In en, this message translates to:
  /// **'Delete this tool?'**
  String get toolDeleteTitle;

  /// No description provided for @toolDeleteBody.
  ///
  /// In en, this message translates to:
  /// **'{name} will be removed. This cannot be undone.'**
  String toolDeleteBody(String name);

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

  /// No description provided for @vehicleQrCode.
  ///
  /// In en, this message translates to:
  /// **'QR code'**
  String get vehicleQrCode;

  /// No description provided for @vehicleOwnershipTypeOwned.
  ///
  /// In en, this message translates to:
  /// **'Owned'**
  String get vehicleOwnershipTypeOwned;

  /// No description provided for @vehicleOwnershipTypeRented.
  ///
  /// In en, this message translates to:
  /// **'Rented'**
  String get vehicleOwnershipTypeRented;

  /// No description provided for @vehicleRentalProvider.
  ///
  /// In en, this message translates to:
  /// **'Rented from'**
  String get vehicleRentalProvider;

  /// No description provided for @vehicleRentalMonthlyAmount.
  ///
  /// In en, this message translates to:
  /// **'Monthly rate'**
  String get vehicleRentalMonthlyAmount;

  /// No description provided for @vehicleLoanedOutTo.
  ///
  /// In en, this message translates to:
  /// **'Loaned out to {name}'**
  String vehicleLoanedOutTo(String name);

  /// No description provided for @rentalRatesTitle.
  ///
  /// In en, this message translates to:
  /// **'Rental / lease'**
  String get rentalRatesTitle;

  /// No description provided for @rentalRatesAdd.
  ///
  /// In en, this message translates to:
  /// **'Add a rate'**
  String get rentalRatesAdd;

  /// No description provided for @rentalRatesEditTitle.
  ///
  /// In en, this message translates to:
  /// **'Edit rate'**
  String get rentalRatesEditTitle;

  /// No description provided for @rentalRatesEmpty.
  ///
  /// In en, this message translates to:
  /// **'No rate on file yet.'**
  String get rentalRatesEmpty;

  /// No description provided for @rentalRatesProvider.
  ///
  /// In en, this message translates to:
  /// **'Provider (optional)'**
  String get rentalRatesProvider;

  /// No description provided for @rentalRatesNote.
  ///
  /// In en, this message translates to:
  /// **'Note (optional)'**
  String get rentalRatesNote;

  /// No description provided for @rentalRatesMonthlyAmount.
  ///
  /// In en, this message translates to:
  /// **'Monthly amount'**
  String get rentalRatesMonthlyAmount;

  /// No description provided for @rentalRatesStartDate.
  ///
  /// In en, this message translates to:
  /// **'From'**
  String get rentalRatesStartDate;

  /// No description provided for @rentalRatesEndDate.
  ///
  /// In en, this message translates to:
  /// **'To (optional, open-ended if blank)'**
  String get rentalRatesEndDate;

  /// No description provided for @rentalRatesOpenEnded.
  ///
  /// In en, this message translates to:
  /// **'Open'**
  String get rentalRatesOpenEnded;

  /// No description provided for @rentalRatesDeleteTitle.
  ///
  /// In en, this message translates to:
  /// **'Delete this rate?'**
  String get rentalRatesDeleteTitle;

  /// No description provided for @rentalRatesDeleteBody.
  ///
  /// In en, this message translates to:
  /// **'This cannot be undone.'**
  String get rentalRatesDeleteBody;

  /// No description provided for @rentalRatesSaved.
  ///
  /// In en, this message translates to:
  /// **'Rental rate saved.'**
  String get rentalRatesSaved;

  /// No description provided for @rentalOutTitle.
  ///
  /// In en, this message translates to:
  /// **'Loaned out'**
  String get rentalOutTitle;

  /// No description provided for @rentalOutAdd.
  ///
  /// In en, this message translates to:
  /// **'Loan it out'**
  String get rentalOutAdd;

  /// No description provided for @rentalOutEditTitle.
  ///
  /// In en, this message translates to:
  /// **'Edit loan'**
  String get rentalOutEditTitle;

  /// No description provided for @rentalOutEmpty.
  ///
  /// In en, this message translates to:
  /// **'Not currently loaned out to anyone.'**
  String get rentalOutEmpty;

  /// No description provided for @rentalOutRenterName.
  ///
  /// In en, this message translates to:
  /// **'Renter\'s name'**
  String get rentalOutRenterName;

  /// No description provided for @rentalOutCustomer.
  ///
  /// In en, this message translates to:
  /// **'Linked customer (optional)'**
  String get rentalOutCustomer;

  /// No description provided for @rentalOutNoCustomer.
  ///
  /// In en, this message translates to:
  /// **'No linked customer'**
  String get rentalOutNoCustomer;

  /// No description provided for @rentalOutDailyRate.
  ///
  /// In en, this message translates to:
  /// **'Daily rate'**
  String get rentalOutDailyRate;

  /// No description provided for @rentalOutStartDate.
  ///
  /// In en, this message translates to:
  /// **'From'**
  String get rentalOutStartDate;

  /// No description provided for @rentalOutStillOut.
  ///
  /// In en, this message translates to:
  /// **'Still out'**
  String get rentalOutStillOut;

  /// No description provided for @rentalOutReturn.
  ///
  /// In en, this message translates to:
  /// **'Mark returned'**
  String get rentalOutReturn;

  /// No description provided for @rentalOutReturned.
  ///
  /// In en, this message translates to:
  /// **'Marked as returned.'**
  String get rentalOutReturned;

  /// No description provided for @rentalOutDeleteTitle.
  ///
  /// In en, this message translates to:
  /// **'Delete this loan?'**
  String get rentalOutDeleteTitle;

  /// No description provided for @rentalOutDeleteBody.
  ///
  /// In en, this message translates to:
  /// **'This cannot be undone.'**
  String get rentalOutDeleteBody;

  /// No description provided for @rentalOutSaved.
  ///
  /// In en, this message translates to:
  /// **'Loan saved.'**
  String get rentalOutSaved;

  /// No description provided for @scanTitle.
  ///
  /// In en, this message translates to:
  /// **'Scan or look up'**
  String get scanTitle;

  /// No description provided for @scanHint.
  ///
  /// In en, this message translates to:
  /// **'Scan the QR label on the tool or vehicle, or enter its code below.'**
  String get scanHint;

  /// No description provided for @scanAction.
  ///
  /// In en, this message translates to:
  /// **'Scan QR code'**
  String get scanAction;

  /// No description provided for @scanToggleFlash.
  ///
  /// In en, this message translates to:
  /// **'Toggle flashlight'**
  String get scanToggleFlash;

  /// No description provided for @scanCodeLabel.
  ///
  /// In en, this message translates to:
  /// **'QR code'**
  String get scanCodeLabel;

  /// No description provided for @scanToolFound.
  ///
  /// In en, this message translates to:
  /// **'Tool'**
  String get scanToolFound;

  /// No description provided for @scanVehicleFound.
  ///
  /// In en, this message translates to:
  /// **'Vehicle'**
  String get scanVehicleFound;

  /// No description provided for @scanCheckOutToMe.
  ///
  /// In en, this message translates to:
  /// **'Check out to me'**
  String get scanCheckOutToMe;

  /// No description provided for @scanReturn.
  ///
  /// In en, this message translates to:
  /// **'Return'**
  String get scanReturn;

  /// No description provided for @scanCheckedOutToYou.
  ///
  /// In en, this message translates to:
  /// **'Checked out to you'**
  String get scanCheckedOutToYou;

  /// No description provided for @scanCheckedOutToOther.
  ///
  /// In en, this message translates to:
  /// **'Checked out to {name}'**
  String scanCheckedOutToOther(String name);

  /// No description provided for @scanNotCheckedOut.
  ///
  /// In en, this message translates to:
  /// **'Not currently checked out'**
  String get scanNotCheckedOut;

  /// No description provided for @scanCheckOutSuccess.
  ///
  /// In en, this message translates to:
  /// **'Checked out to you.'**
  String get scanCheckOutSuccess;

  /// No description provided for @scanReturnSuccess.
  ///
  /// In en, this message translates to:
  /// **'Returned.'**
  String get scanReturnSuccess;

  /// No description provided for @scanCameraPermissionDenied.
  ///
  /// In en, this message translates to:
  /// **'Camera permission is required to scan a QR code.'**
  String get scanCameraPermissionDenied;

  /// No description provided for @scanTransferToEmployee.
  ///
  /// In en, this message translates to:
  /// **'Transfer to employee'**
  String get scanTransferToEmployee;

  /// No description provided for @scanTransferToProject.
  ///
  /// In en, this message translates to:
  /// **'Transfer to site'**
  String get scanTransferToProject;

  /// No description provided for @scanPickEmployee.
  ///
  /// In en, this message translates to:
  /// **'Employee'**
  String get scanPickEmployee;

  /// No description provided for @scanPickProject.
  ///
  /// In en, this message translates to:
  /// **'Site'**
  String get scanPickProject;

  /// No description provided for @scanTransferConfirm.
  ///
  /// In en, this message translates to:
  /// **'Transfer'**
  String get scanTransferConfirm;

  /// No description provided for @scanTransferSuccess.
  ///
  /// In en, this message translates to:
  /// **'Transferred to {name}.'**
  String scanTransferSuccess(String name);

  /// No description provided for @scanTransferProjectSuccess.
  ///
  /// In en, this message translates to:
  /// **'Placed on {name}.'**
  String scanTransferProjectSuccess(String name);

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

  /// No description provided for @materialFormAddTitle.
  ///
  /// In en, this message translates to:
  /// **'Add a material'**
  String get materialFormAddTitle;

  /// No description provided for @materialFormEditTitle.
  ///
  /// In en, this message translates to:
  /// **'Edit material'**
  String get materialFormEditTitle;

  /// No description provided for @materialFormName.
  ///
  /// In en, this message translates to:
  /// **'Name'**
  String get materialFormName;

  /// No description provided for @materialFormUnit.
  ///
  /// In en, this message translates to:
  /// **'Unit'**
  String get materialFormUnit;

  /// No description provided for @materialFormUnitPrice.
  ///
  /// In en, this message translates to:
  /// **'Unit price (optional)'**
  String get materialFormUnitPrice;

  /// No description provided for @materialFormAdded.
  ///
  /// In en, this message translates to:
  /// **'Material added.'**
  String get materialFormAdded;

  /// No description provided for @materialFormSaved.
  ///
  /// In en, this message translates to:
  /// **'Material updated.'**
  String get materialFormSaved;

  /// No description provided for @materialDeleteTitle.
  ///
  /// In en, this message translates to:
  /// **'Delete this material?'**
  String get materialDeleteTitle;

  /// No description provided for @materialDeleteBody.
  ///
  /// In en, this message translates to:
  /// **'{name} will be removed. This cannot be undone.'**
  String materialDeleteBody(String name);

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

  /// No description provided for @employeeFormAddTitle.
  ///
  /// In en, this message translates to:
  /// **'Add an employee'**
  String get employeeFormAddTitle;

  /// No description provided for @employeeFormEditTitle.
  ///
  /// In en, this message translates to:
  /// **'Edit employee'**
  String get employeeFormEditTitle;

  /// No description provided for @employeeFormNumber.
  ///
  /// In en, this message translates to:
  /// **'Employee number'**
  String get employeeFormNumber;

  /// No description provided for @employeeFormFirstName.
  ///
  /// In en, this message translates to:
  /// **'First name'**
  String get employeeFormFirstName;

  /// No description provided for @employeeFormLastName.
  ///
  /// In en, this message translates to:
  /// **'Last name'**
  String get employeeFormLastName;

  /// No description provided for @employeeFormEmploymentDate.
  ///
  /// In en, this message translates to:
  /// **'Employment date'**
  String get employeeFormEmploymentDate;

  /// No description provided for @employeeFormAdded.
  ///
  /// In en, this message translates to:
  /// **'Employee added.'**
  String get employeeFormAdded;

  /// No description provided for @employeeFormSaved.
  ///
  /// In en, this message translates to:
  /// **'Employee updated.'**
  String get employeeFormSaved;

  /// No description provided for @employeeDeleteTitle.
  ///
  /// In en, this message translates to:
  /// **'Delete this employee?'**
  String get employeeDeleteTitle;

  /// No description provided for @employeeDeleteBody.
  ///
  /// In en, this message translates to:
  /// **'{name} will be removed. This cannot be undone.'**
  String employeeDeleteBody(String name);

  /// No description provided for @employeeType.
  ///
  /// In en, this message translates to:
  /// **'Type'**
  String get employeeType;

  /// No description provided for @employeeTypeEmployee.
  ///
  /// In en, this message translates to:
  /// **'Employee'**
  String get employeeTypeEmployee;

  /// No description provided for @employeeTypeSubcontractor.
  ///
  /// In en, this message translates to:
  /// **'Subcontractor'**
  String get employeeTypeSubcontractor;

  /// No description provided for @projectFormAddTitle.
  ///
  /// In en, this message translates to:
  /// **'Add a project'**
  String get projectFormAddTitle;

  /// No description provided for @projectFormEditTitle.
  ///
  /// In en, this message translates to:
  /// **'Edit project'**
  String get projectFormEditTitle;

  /// No description provided for @projectFormName.
  ///
  /// In en, this message translates to:
  /// **'Project name'**
  String get projectFormName;

  /// No description provided for @projectFormDescription.
  ///
  /// In en, this message translates to:
  /// **'Description (optional)'**
  String get projectFormDescription;

  /// No description provided for @projectFormParentProject.
  ///
  /// In en, this message translates to:
  /// **'Parent project (optional)'**
  String get projectFormParentProject;

  /// No description provided for @projectFormNoParent.
  ///
  /// In en, this message translates to:
  /// **'No parent — a main project'**
  String get projectFormNoParent;

  /// No description provided for @projectFormNoCustomer.
  ///
  /// In en, this message translates to:
  /// **'No customer'**
  String get projectFormNoCustomer;

  /// No description provided for @projectFormCountryCode.
  ///
  /// In en, this message translates to:
  /// **'Country code (e.g. BA)'**
  String get projectFormCountryCode;

  /// No description provided for @projectFormShiftStartTime.
  ///
  /// In en, this message translates to:
  /// **'Shift start time (optional)'**
  String get projectFormShiftStartTime;

  /// No description provided for @projectFormContractValue.
  ///
  /// In en, this message translates to:
  /// **'Contract value (optional)'**
  String get projectFormContractValue;

  /// No description provided for @projectFormLatitude.
  ///
  /// In en, this message translates to:
  /// **'Latitude'**
  String get projectFormLatitude;

  /// No description provided for @projectFormLongitude.
  ///
  /// In en, this message translates to:
  /// **'Longitude'**
  String get projectFormLongitude;

  /// No description provided for @projectFormAdded.
  ///
  /// In en, this message translates to:
  /// **'Project added.'**
  String get projectFormAdded;

  /// No description provided for @projectFormSaved.
  ///
  /// In en, this message translates to:
  /// **'Project updated.'**
  String get projectFormSaved;

  /// No description provided for @projectDeleteTitle.
  ///
  /// In en, this message translates to:
  /// **'Delete this project?'**
  String get projectDeleteTitle;

  /// No description provided for @projectDeleteBody.
  ///
  /// In en, this message translates to:
  /// **'{name} will be removed. This cannot be undone.'**
  String projectDeleteBody(String name);

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

  /// No description provided for @notificationsUnreadCount.
  ///
  /// In en, this message translates to:
  /// **'Unread ({count})'**
  String notificationsUnreadCount(int count);

  /// No description provided for @notificationsMarkAllRead.
  ///
  /// In en, this message translates to:
  /// **'Mark all read'**
  String get notificationsMarkAllRead;

  /// No description provided for @notificationsAcknowledgedOn.
  ///
  /// In en, this message translates to:
  /// **'Confirmed on {date}'**
  String notificationsAcknowledgedOn(String date);

  /// No description provided for @notificationsOpenRelated.
  ///
  /// In en, this message translates to:
  /// **'Open'**
  String get notificationsOpenRelated;

  /// No description provided for @notificationProjectAssignedTitle.
  ///
  /// In en, this message translates to:
  /// **'New project assigned'**
  String get notificationProjectAssignedTitle;

  /// No description provided for @notificationProjectAssignedBody.
  ///
  /// In en, this message translates to:
  /// **'You have been assigned to project \"{projectName}\".'**
  String notificationProjectAssignedBody(String projectName);

  /// No description provided for @notificationProjectAssignedBodyWithAddress.
  ///
  /// In en, this message translates to:
  /// **'You have been assigned to project \"{projectName}\", at {address}.'**
  String notificationProjectAssignedBodyWithAddress(
    String projectName,
    String address,
  );

  /// No description provided for @notificationProjectAssignedBodyFull.
  ///
  /// In en, this message translates to:
  /// **'You have been assigned to project \"{projectName}\", at {address}. Shift starts at {shiftStartTime}.'**
  String notificationProjectAssignedBodyFull(
    String projectName,
    String address,
    String shiftStartTime,
  );

  /// No description provided for @notificationEmployeeAssignedTitle.
  ///
  /// In en, this message translates to:
  /// **'Employee assigned to your project'**
  String get notificationEmployeeAssignedTitle;

  /// No description provided for @notificationEmployeeAssignedBody.
  ///
  /// In en, this message translates to:
  /// **'{employeeName} has been assigned to project \"{projectName}\".'**
  String notificationEmployeeAssignedBody(
    String employeeName,
    String projectName,
  );

  /// No description provided for @notificationVehicleAssignedTitle.
  ///
  /// In en, this message translates to:
  /// **'Vehicle assigned'**
  String get notificationVehicleAssignedTitle;

  /// No description provided for @notificationVehicleAssignedBody.
  ///
  /// In en, this message translates to:
  /// **'Vehicle {brand} {model} ({registration}) has been assigned to you.'**
  String notificationVehicleAssignedBody(
    String brand,
    String model,
    String registration,
  );

  /// No description provided for @notificationToolAssignedTitle.
  ///
  /// In en, this message translates to:
  /// **'Tool assigned'**
  String get notificationToolAssignedTitle;

  /// No description provided for @notificationToolAssignedBody.
  ///
  /// In en, this message translates to:
  /// **'Tool \"{toolName}\" has been assigned to you.'**
  String notificationToolAssignedBody(String toolName);

  /// No description provided for @notificationDocumentExpiringTitle.
  ///
  /// In en, this message translates to:
  /// **'Document expiring soon'**
  String get notificationDocumentExpiringTitle;

  /// No description provided for @notificationDocumentExpiredTitle.
  ///
  /// In en, this message translates to:
  /// **'Document has expired'**
  String get notificationDocumentExpiredTitle;

  /// No description provided for @notificationDocumentExpiringBody.
  ///
  /// In en, this message translates to:
  /// **'{fileName} ({expiresAt})'**
  String notificationDocumentExpiringBody(String fileName, String expiresAt);

  /// No description provided for @notificationDocumentExpiringBodyWithOwner.
  ///
  /// In en, this message translates to:
  /// **'{fileName} — {ownerName} ({expiresAt})'**
  String notificationDocumentExpiringBodyWithOwner(
    String fileName,
    String ownerName,
    String expiresAt,
  );

  /// No description provided for @notificationTaskAssignedTitle.
  ///
  /// In en, this message translates to:
  /// **'Task assigned to you'**
  String get notificationTaskAssignedTitle;

  /// No description provided for @notificationDefectAssignedTitle.
  ///
  /// In en, this message translates to:
  /// **'Defect assigned to you'**
  String get notificationDefectAssignedTitle;

  /// No description provided for @notificationWorkItemAssignedBodyWithDueDate.
  ///
  /// In en, this message translates to:
  /// **'{title} — due {dueDate}'**
  String notificationWorkItemAssignedBodyWithDueDate(
    String title,
    String dueDate,
  );

  /// No description provided for @notificationWorkItemOverdueTitle.
  ///
  /// In en, this message translates to:
  /// **'Overdue'**
  String get notificationWorkItemOverdueTitle;

  /// No description provided for @notificationWorkItemDueSoonTitle.
  ///
  /// In en, this message translates to:
  /// **'Due soon'**
  String get notificationWorkItemDueSoonTitle;

  /// No description provided for @notificationWorkItemDueBody.
  ///
  /// In en, this message translates to:
  /// **'{title} ({dueDate})'**
  String notificationWorkItemDueBody(String title, String dueDate);

  /// No description provided for @notificationShiftAutoClosedTitle.
  ///
  /// In en, this message translates to:
  /// **'Shift closed automatically'**
  String get notificationShiftAutoClosedTitle;

  /// No description provided for @notificationShiftAutoClosedBody.
  ///
  /// In en, this message translates to:
  /// **'You did not clock out on {shiftDate}, so the shift was closed automatically and is waiting for review.'**
  String notificationShiftAutoClosedBody(String shiftDate);

  /// No description provided for @notificationAbsenceEditProposedTitle.
  ///
  /// In en, this message translates to:
  /// **'Change proposed for approved leave'**
  String get notificationAbsenceEditProposedTitle;

  /// No description provided for @notificationAbsenceEditProposedBody.
  ///
  /// In en, this message translates to:
  /// **'{startDate}–{endDate} — please confirm or decline.'**
  String notificationAbsenceEditProposedBody(String startDate, String endDate);

  /// No description provided for @notificationAbsenceEditConfirmedTitle.
  ///
  /// In en, this message translates to:
  /// **'Leave change confirmed'**
  String get notificationAbsenceEditConfirmedTitle;

  /// No description provided for @notificationAbsenceEditConfirmedBody.
  ///
  /// In en, this message translates to:
  /// **'{startDate}–{endDate}'**
  String notificationAbsenceEditConfirmedBody(String startDate, String endDate);

  /// No description provided for @notificationAbsenceEditDeclinedTitle.
  ///
  /// In en, this message translates to:
  /// **'Leave change declined'**
  String get notificationAbsenceEditDeclinedTitle;

  /// No description provided for @notificationAbsenceEditDeclinedBody.
  ///
  /// In en, this message translates to:
  /// **'The other side declined your proposed change.'**
  String get notificationAbsenceEditDeclinedBody;

  /// No description provided for @notificationWeeklyReportDueTitle.
  ///
  /// In en, this message translates to:
  /// **'Weekly hours not yet submitted'**
  String get notificationWeeklyReportDueTitle;

  /// No description provided for @notificationWeeklyReportDueBody.
  ///
  /// In en, this message translates to:
  /// **'{projectName} — KW{isoWeek}/{isoYear}'**
  String notificationWeeklyReportDueBody(
    String projectName,
    String isoWeek,
    String isoYear,
  );

  /// No description provided for @notificationEmployeeClockedInTitle.
  ///
  /// In en, this message translates to:
  /// **'Clocked in'**
  String get notificationEmployeeClockedInTitle;

  /// No description provided for @notificationEmployeeClockedInBody.
  ///
  /// In en, this message translates to:
  /// **'{employeeName} clocked in at {projectName}.'**
  String notificationEmployeeClockedInBody(
    String employeeName,
    String projectName,
  );

  /// No description provided for @notificationEmployeeClockedOutTitle.
  ///
  /// In en, this message translates to:
  /// **'Clocked out'**
  String get notificationEmployeeClockedOutTitle;

  /// No description provided for @notificationEmployeeClockedOutBody.
  ///
  /// In en, this message translates to:
  /// **'{employeeName} clocked out from {projectName} after {hours}h {minutes}m.'**
  String notificationEmployeeClockedOutBody(
    String employeeName,
    String projectName,
    String hours,
    String minutes,
  );

  /// No description provided for @notificationUnassignedClockInTitle.
  ///
  /// In en, this message translates to:
  /// **'Clock-in at an unassigned site'**
  String get notificationUnassignedClockInTitle;

  /// No description provided for @notificationUnassignedClockInBody.
  ///
  /// In en, this message translates to:
  /// **'{employeeName} clocked in at {projectName}, but is not currently posted there.'**
  String notificationUnassignedClockInBody(
    String employeeName,
    String projectName,
  );

  /// No description provided for @notificationDefectReportedTitle.
  ///
  /// In en, this message translates to:
  /// **'New defect reported'**
  String get notificationDefectReportedTitle;

  /// No description provided for @notificationDefectReportedBody.
  ///
  /// In en, this message translates to:
  /// **'{reporterName} reported: {title}'**
  String notificationDefectReportedBody(String reporterName, String title);

  /// No description provided for @notificationAbsenceRequestedTitle.
  ///
  /// In en, this message translates to:
  /// **'Time off requested'**
  String get notificationAbsenceRequestedTitle;

  /// No description provided for @notificationAbsenceRequestedBody.
  ///
  /// In en, this message translates to:
  /// **'{employeeName} asked for time off, from {startDate} to {endDate}.'**
  String notificationAbsenceRequestedBody(
    String employeeName,
    String startDate,
    String endDate,
  );

  /// No description provided for @notificationDocumentRetentionEndedTitle.
  ///
  /// In en, this message translates to:
  /// **'Document retention period ended'**
  String get notificationDocumentRetentionEndedTitle;

  /// No description provided for @notificationDocumentRetentionEndedBody.
  ///
  /// In en, this message translates to:
  /// **'{fileName} no longer has to be kept (was until {retainUntil}). Delete it yourself if it is no longer needed.'**
  String notificationDocumentRetentionEndedBody(
    String fileName,
    String retainUntil,
  );

  /// No description provided for @notificationDocumentRetentionEndedBodyWithOwner.
  ///
  /// In en, this message translates to:
  /// **'{fileName} — {ownerName} no longer has to be kept (was until {retainUntil}). Delete it yourself if it is no longer needed.'**
  String notificationDocumentRetentionEndedBodyWithOwner(
    String fileName,
    String ownerName,
    String retainUntil,
  );

  /// No description provided for @notificationTypeDirectMessage.
  ///
  /// In en, this message translates to:
  /// **'Direct message'**
  String get notificationTypeDirectMessage;

  /// No description provided for @notificationTypeDocumentExpiring.
  ///
  /// In en, this message translates to:
  /// **'Document expiring'**
  String get notificationTypeDocumentExpiring;

  /// No description provided for @notificationTypeTaskAssigned.
  ///
  /// In en, this message translates to:
  /// **'Task assigned'**
  String get notificationTypeTaskAssigned;

  /// No description provided for @notificationTypeDefectAssigned.
  ///
  /// In en, this message translates to:
  /// **'Defect assigned'**
  String get notificationTypeDefectAssigned;

  /// No description provided for @notificationTypeWorkItemDue.
  ///
  /// In en, this message translates to:
  /// **'Work due'**
  String get notificationTypeWorkItemDue;

  /// No description provided for @notificationTypeShiftAutoClosed.
  ///
  /// In en, this message translates to:
  /// **'Shift auto-closed'**
  String get notificationTypeShiftAutoClosed;

  /// No description provided for @notificationTypeBulletinPosted.
  ///
  /// In en, this message translates to:
  /// **'Bulletin post'**
  String get notificationTypeBulletinPosted;

  /// No description provided for @notificationTypeAbsenceEditProposed.
  ///
  /// In en, this message translates to:
  /// **'Leave change'**
  String get notificationTypeAbsenceEditProposed;

  /// No description provided for @notificationTypeWeeklyReportDue.
  ///
  /// In en, this message translates to:
  /// **'Weekly report due'**
  String get notificationTypeWeeklyReportDue;

  /// No description provided for @notificationTypeEmployeeClockedIn.
  ///
  /// In en, this message translates to:
  /// **'Clocked in'**
  String get notificationTypeEmployeeClockedIn;

  /// No description provided for @notificationTypeEmployeeClockedOut.
  ///
  /// In en, this message translates to:
  /// **'Clocked out'**
  String get notificationTypeEmployeeClockedOut;

  /// No description provided for @notificationTypeUnassignedProjectClockIn.
  ///
  /// In en, this message translates to:
  /// **'Unassigned clock-in'**
  String get notificationTypeUnassignedProjectClockIn;

  /// No description provided for @notificationTypeDefectReported.
  ///
  /// In en, this message translates to:
  /// **'Defect reported'**
  String get notificationTypeDefectReported;

  /// No description provided for @notificationTypeAbsenceRequested.
  ///
  /// In en, this message translates to:
  /// **'Leave requested'**
  String get notificationTypeAbsenceRequested;

  /// No description provided for @notificationTypeDocumentRetentionEnded.
  ///
  /// In en, this message translates to:
  /// **'Document retention ended'**
  String get notificationTypeDocumentRetentionEnded;

  /// No description provided for @announceTitle.
  ///
  /// In en, this message translates to:
  /// **'Send an announcement'**
  String get announceTitle;

  /// No description provided for @announceSubject.
  ///
  /// In en, this message translates to:
  /// **'Subject'**
  String get announceSubject;

  /// No description provided for @announceMessage.
  ///
  /// In en, this message translates to:
  /// **'Message'**
  String get announceMessage;

  /// No description provided for @announceAudienceRole.
  ///
  /// In en, this message translates to:
  /// **'Role'**
  String get announceAudienceRole;

  /// No description provided for @announceEveryRole.
  ///
  /// In en, this message translates to:
  /// **'Every role'**
  String get announceEveryRole;

  /// No description provided for @announceAudienceProject.
  ///
  /// In en, this message translates to:
  /// **'Project'**
  String get announceAudienceProject;

  /// No description provided for @announceEveryProject.
  ///
  /// In en, this message translates to:
  /// **'Every project'**
  String get announceEveryProject;

  /// No description provided for @announceAudienceGroup.
  ///
  /// In en, this message translates to:
  /// **'Group'**
  String get announceAudienceGroup;

  /// No description provided for @announceEveryGroup.
  ///
  /// In en, this message translates to:
  /// **'Every group'**
  String get announceEveryGroup;

  /// No description provided for @announceRequiresAcknowledgment.
  ///
  /// In en, this message translates to:
  /// **'Require confirmation before recipients can do anything else'**
  String get announceRequiresAcknowledgment;

  /// No description provided for @announceHint.
  ///
  /// In en, this message translates to:
  /// **'A phone notification cannot be recalled — the audience is the one thing worth checking twice.'**
  String get announceHint;

  /// No description provided for @announceSend.
  ///
  /// In en, this message translates to:
  /// **'Send'**
  String get announceSend;

  /// No description provided for @announceSent.
  ///
  /// In en, this message translates to:
  /// **'{count, plural, =0{Sent to nobody — nobody matched.} =1{Sent to 1 person.} other{Sent to {count} people.}}'**
  String announceSent(int count);

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

  /// No description provided for @vehicleStatusRentedOut.
  ///
  /// In en, this message translates to:
  /// **'Loaned out'**
  String get vehicleStatusRentedOut;

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

  /// No description provided for @toolStatusRentedOut.
  ///
  /// In en, this message translates to:
  /// **'Loaned out'**
  String get toolStatusRentedOut;

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

  /// No description provided for @shiftAutoClosed.
  ///
  /// In en, this message translates to:
  /// **'Auto-closed'**
  String get shiftAutoClosed;

  /// No description provided for @shiftLocationMismatch.
  ///
  /// In en, this message translates to:
  /// **'Clocked in away from the site'**
  String get shiftLocationMismatch;

  /// No description provided for @shiftTimeMismatch.
  ///
  /// In en, this message translates to:
  /// **'Clocked in outside the expected shift time'**
  String get shiftTimeMismatch;

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

  /// No description provided for @attachmentsAddDocument.
  ///
  /// In en, this message translates to:
  /// **'Add a document'**
  String get attachmentsAddDocument;

  /// No description provided for @attachmentsPickFile.
  ///
  /// In en, this message translates to:
  /// **'Choose a file'**
  String get attachmentsPickFile;

  /// No description provided for @attachmentsChangeFile.
  ///
  /// In en, this message translates to:
  /// **'Choose a different file'**
  String get attachmentsChangeFile;

  /// No description provided for @attachmentsCategory.
  ///
  /// In en, this message translates to:
  /// **'Category'**
  String get attachmentsCategory;

  /// No description provided for @attachmentsDescription.
  ///
  /// In en, this message translates to:
  /// **'Description (optional)'**
  String get attachmentsDescription;

  /// No description provided for @attachmentsExpiryDate.
  ///
  /// In en, this message translates to:
  /// **'Expiry date (optional)'**
  String get attachmentsExpiryDate;

  /// No description provided for @attachmentsRetainUntil.
  ///
  /// In en, this message translates to:
  /// **'Retain until (optional)'**
  String get attachmentsRetainUntil;

  /// No description provided for @attachmentsSaved.
  ///
  /// In en, this message translates to:
  /// **'Document added.'**
  String get attachmentsSaved;

  /// No description provided for @attachmentsDeleteTitle.
  ///
  /// In en, this message translates to:
  /// **'Delete this document?'**
  String get attachmentsDeleteTitle;

  /// No description provided for @attachmentsDeleteBody.
  ///
  /// In en, this message translates to:
  /// **'{name} will be removed. This cannot be undone.'**
  String attachmentsDeleteBody(String name);

  /// No description provided for @attachmentsRetainedCannotDelete.
  ///
  /// In en, this message translates to:
  /// **'Kept until {date} — cannot be deleted yet.'**
  String attachmentsRetainedCannotDelete(String date);

  /// No description provided for @attachmentsOpen.
  ///
  /// In en, this message translates to:
  /// **'Open'**
  String get attachmentsOpen;

  /// No description provided for @attachmentsOpeningExternally.
  ///
  /// In en, this message translates to:
  /// **'Opening in another app…'**
  String get attachmentsOpeningExternally;

  /// No description provided for @attachmentsOpenExternalFailed.
  ///
  /// In en, this message translates to:
  /// **'No app on this phone can open this file type.'**
  String get attachmentsOpenExternalFailed;

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

  /// No description provided for @navSchedule.
  ///
  /// In en, this message translates to:
  /// **'My schedule'**
  String get navSchedule;

  /// No description provided for @navAbsences.
  ///
  /// In en, this message translates to:
  /// **'Time off'**
  String get navAbsences;

  /// No description provided for @navWeeklyReports.
  ///
  /// In en, this message translates to:
  /// **'Weekly reports'**
  String get navWeeklyReports;

  /// No description provided for @navBulletin.
  ///
  /// In en, this message translates to:
  /// **'Bulletin board'**
  String get navBulletin;

  /// No description provided for @bulletinTitle.
  ///
  /// In en, this message translates to:
  /// **'Bulletin board'**
  String get bulletinTitle;

  /// No description provided for @bulletinEmpty.
  ///
  /// In en, this message translates to:
  /// **'Nothing posted right now.'**
  String get bulletinEmpty;

  /// No description provided for @bulletinPostedBy.
  ///
  /// In en, this message translates to:
  /// **'Posted by {name}'**
  String bulletinPostedBy(String name);

  /// No description provided for @scheduleTitle.
  ///
  /// In en, this message translates to:
  /// **'My schedule'**
  String get scheduleTitle;

  /// No description provided for @scheduleEmpty.
  ///
  /// In en, this message translates to:
  /// **'Nothing scheduled for the next two weeks.'**
  String get scheduleEmpty;

  /// No description provided for @scheduleToday.
  ///
  /// In en, this message translates to:
  /// **'Today'**
  String get scheduleToday;

  /// No description provided for @scheduleTomorrow.
  ///
  /// In en, this message translates to:
  /// **'Tomorrow'**
  String get scheduleTomorrow;

  /// No description provided for @scheduleContinues.
  ///
  /// In en, this message translates to:
  /// **'Runs on'**
  String get scheduleContinues;

  /// No description provided for @scheduleUpcoming.
  ///
  /// In en, this message translates to:
  /// **'Next two weeks'**
  String get scheduleUpcoming;

  /// No description provided for @scheduleDateRange.
  ///
  /// In en, this message translates to:
  /// **'{from} – {to}'**
  String scheduleDateRange(String from, String to);

  /// No description provided for @scheduleAway.
  ///
  /// In en, this message translates to:
  /// **'Away'**
  String get scheduleAway;

  /// No description provided for @scheduleOnSite.
  ///
  /// In en, this message translates to:
  /// **'On site'**
  String get scheduleOnSite;

  /// No description provided for @absencesTitle.
  ///
  /// In en, this message translates to:
  /// **'Time off'**
  String get absencesTitle;

  /// No description provided for @absencesEmpty.
  ///
  /// In en, this message translates to:
  /// **'You have not asked for any time off.'**
  String get absencesEmpty;

  /// No description provided for @absencesPendingOnly.
  ///
  /// In en, this message translates to:
  /// **'Waiting for an answer'**
  String get absencesPendingOnly;

  /// No description provided for @absencesRequest.
  ///
  /// In en, this message translates to:
  /// **'Ask for time off'**
  String get absencesRequest;

  /// No description provided for @absencesType.
  ///
  /// In en, this message translates to:
  /// **'Kind'**
  String get absencesType;

  /// No description provided for @absencesStartDate.
  ///
  /// In en, this message translates to:
  /// **'From'**
  String get absencesStartDate;

  /// No description provided for @absencesEndDate.
  ///
  /// In en, this message translates to:
  /// **'To'**
  String get absencesEndDate;

  /// No description provided for @absencesReason.
  ///
  /// In en, this message translates to:
  /// **'Reason (optional)'**
  String get absencesReason;

  /// No description provided for @absencesSend.
  ///
  /// In en, this message translates to:
  /// **'Send request'**
  String get absencesSend;

  /// No description provided for @absencesSent.
  ///
  /// In en, this message translates to:
  /// **'Request sent.'**
  String get absencesSent;

  /// No description provided for @absencesPickDates.
  ///
  /// In en, this message translates to:
  /// **'Pick the first and last day you will be away.'**
  String get absencesPickDates;

  /// No description provided for @absencesEndsBeforeStart.
  ///
  /// In en, this message translates to:
  /// **'The last day cannot be before the first.'**
  String get absencesEndsBeforeStart;

  /// No description provided for @absencesDayCount.
  ///
  /// In en, this message translates to:
  /// **'{count, plural, one{{count} day} few{{count} days} other{{count} days}}'**
  String absencesDayCount(int count);

  /// No description provided for @absencesWithdraw.
  ///
  /// In en, this message translates to:
  /// **'Withdraw'**
  String get absencesWithdraw;

  /// No description provided for @absencesWithdrawTitle.
  ///
  /// In en, this message translates to:
  /// **'Withdraw this request?'**
  String get absencesWithdrawTitle;

  /// No description provided for @absencesWithdrawBody.
  ///
  /// In en, this message translates to:
  /// **'Your supervisor will no longer see you asking for these days.'**
  String get absencesWithdrawBody;

  /// No description provided for @absencesWithdrawn.
  ///
  /// In en, this message translates to:
  /// **'Request withdrawn.'**
  String get absencesWithdrawn;

  /// No description provided for @absencesGrantedLocked.
  ///
  /// In en, this message translates to:
  /// **'Granted time off has to be cancelled by your supervisor.'**
  String get absencesGrantedLocked;

  /// No description provided for @absencesAnsweredBy.
  ///
  /// In en, this message translates to:
  /// **'Answered by {name}'**
  String absencesAnsweredBy(String name);

  /// No description provided for @absenceTypeAnnualLeave.
  ///
  /// In en, this message translates to:
  /// **'Annual leave'**
  String get absenceTypeAnnualLeave;

  /// No description provided for @absenceTypeSickLeave.
  ///
  /// In en, this message translates to:
  /// **'Sick leave'**
  String get absenceTypeSickLeave;

  /// No description provided for @absenceTypeUnpaidLeave.
  ///
  /// In en, this message translates to:
  /// **'Unpaid leave'**
  String get absenceTypeUnpaidLeave;

  /// No description provided for @absenceTypePaidSpecialLeave.
  ///
  /// In en, this message translates to:
  /// **'Paid special leave'**
  String get absenceTypePaidSpecialLeave;

  /// No description provided for @absenceTypeTraining.
  ///
  /// In en, this message translates to:
  /// **'Training'**
  String get absenceTypeTraining;

  /// No description provided for @absenceTypeOther.
  ///
  /// In en, this message translates to:
  /// **'Other'**
  String get absenceTypeOther;

  /// No description provided for @absenceStatusRequested.
  ///
  /// In en, this message translates to:
  /// **'Waiting'**
  String get absenceStatusRequested;

  /// No description provided for @absenceStatusApproved.
  ///
  /// In en, this message translates to:
  /// **'Granted'**
  String get absenceStatusApproved;

  /// No description provided for @absenceStatusRejected.
  ///
  /// In en, this message translates to:
  /// **'Refused'**
  String get absenceStatusRejected;

  /// No description provided for @absenceStatusCancelled.
  ///
  /// In en, this message translates to:
  /// **'Withdrawn'**
  String get absenceStatusCancelled;

  /// No description provided for @weeklyReportsTitle.
  ///
  /// In en, this message translates to:
  /// **'Weekly site reports'**
  String get weeklyReportsTitle;

  /// No description provided for @weeklyReportsDescription.
  ///
  /// In en, this message translates to:
  /// **'Send the week\'s signed hours, Aufmaß, or other proof of work for a site you are posted to.'**
  String get weeklyReportsDescription;

  /// No description provided for @weeklyReportsSubmit.
  ///
  /// In en, this message translates to:
  /// **'Submit report'**
  String get weeklyReportsSubmit;

  /// No description provided for @weeklyReportsProject.
  ///
  /// In en, this message translates to:
  /// **'Site'**
  String get weeklyReportsProject;

  /// No description provided for @weeklyReportsProjectsFailed.
  ///
  /// In en, this message translates to:
  /// **'Could not load your sites.'**
  String get weeklyReportsProjectsFailed;

  /// No description provided for @weeklyReportsIsoYear.
  ///
  /// In en, this message translates to:
  /// **'Year'**
  String get weeklyReportsIsoYear;

  /// No description provided for @weeklyReportsIsoWeek.
  ///
  /// In en, this message translates to:
  /// **'Week'**
  String get weeklyReportsIsoWeek;

  /// No description provided for @weeklyReportsType.
  ///
  /// In en, this message translates to:
  /// **'Kind'**
  String get weeklyReportsType;

  /// No description provided for @weeklyReportsHours.
  ///
  /// In en, this message translates to:
  /// **'Hours'**
  String get weeklyReportsHours;

  /// No description provided for @weeklyReportsNote.
  ///
  /// In en, this message translates to:
  /// **'Note (optional)'**
  String get weeklyReportsNote;

  /// No description provided for @weeklyReportsAttachFile.
  ///
  /// In en, this message translates to:
  /// **'Attach file'**
  String get weeklyReportsAttachFile;

  /// No description provided for @weeklyReportsFileTooLarge.
  ///
  /// In en, this message translates to:
  /// **'That file is larger than the 20 MB limit.'**
  String get weeklyReportsFileTooLarge;

  /// No description provided for @weeklyReportsSend.
  ///
  /// In en, this message translates to:
  /// **'Send'**
  String get weeklyReportsSend;

  /// No description provided for @notifyEmployeeAction.
  ///
  /// In en, this message translates to:
  /// **'Notify'**
  String get notifyEmployeeAction;

  /// No description provided for @notifyEmployeeTitle.
  ///
  /// In en, this message translates to:
  /// **'Send a message'**
  String get notifyEmployeeTitle;

  /// No description provided for @notifyEmployeeSubject.
  ///
  /// In en, this message translates to:
  /// **'Subject'**
  String get notifyEmployeeSubject;

  /// No description provided for @notifyEmployeeMessage.
  ///
  /// In en, this message translates to:
  /// **'Message'**
  String get notifyEmployeeMessage;

  /// No description provided for @notifyEmployeeRequireAck.
  ///
  /// In en, this message translates to:
  /// **'Require confirmation'**
  String get notifyEmployeeRequireAck;

  /// No description provided for @notifyEmployeeRequireAckHint.
  ///
  /// In en, this message translates to:
  /// **'They cannot use anything else in the app until they confirm they saw this.'**
  String get notifyEmployeeRequireAckHint;

  /// No description provided for @notifyEmployeeSend.
  ///
  /// In en, this message translates to:
  /// **'Send'**
  String get notifyEmployeeSend;

  /// No description provided for @notifyEmployeeSent.
  ///
  /// In en, this message translates to:
  /// **'Message sent.'**
  String get notifyEmployeeSent;

  /// No description provided for @teamTodayAction.
  ///
  /// In en, this message translates to:
  /// **'Team today'**
  String get teamTodayAction;

  /// No description provided for @teamTodayTitle.
  ///
  /// In en, this message translates to:
  /// **'Team today'**
  String get teamTodayTitle;

  /// No description provided for @teamTodayEmpty.
  ///
  /// In en, this message translates to:
  /// **'Nobody has clocked in today yet.'**
  String get teamTodayEmpty;

  /// No description provided for @teamTodayStillWorking.
  ///
  /// In en, this message translates to:
  /// **'Since {time} — still working'**
  String teamTodayStillWorking(String time);

  /// No description provided for @weeklyReportsSent.
  ///
  /// In en, this message translates to:
  /// **'Report sent.'**
  String get weeklyReportsSent;

  /// No description provided for @weeklyReportTypeSignedHours.
  ///
  /// In en, this message translates to:
  /// **'Signed hours'**
  String get weeklyReportTypeSignedHours;

  /// No description provided for @weeklyReportTypeAufmass.
  ///
  /// In en, this message translates to:
  /// **'Aufmaß'**
  String get weeklyReportTypeAufmass;

  /// No description provided for @weeklyReportTypeOther.
  ///
  /// In en, this message translates to:
  /// **'Other'**
  String get weeklyReportTypeOther;

  /// No description provided for @weeklyReportStatusSubmitted.
  ///
  /// In en, this message translates to:
  /// **'Awaiting review'**
  String get weeklyReportStatusSubmitted;

  /// No description provided for @weeklyReportStatusProcessed.
  ///
  /// In en, this message translates to:
  /// **'Processed'**
  String get weeklyReportStatusProcessed;

  /// No description provided for @weeklyReportsHistoryTitle.
  ///
  /// In en, this message translates to:
  /// **'Sent so far'**
  String get weeklyReportsHistoryTitle;

  /// No description provided for @weeklyReportsHistoryEmpty.
  ///
  /// In en, this message translates to:
  /// **'Nothing sent yet.'**
  String get weeklyReportsHistoryEmpty;

  /// No description provided for @weeklyReportsIsoWeekLabel.
  ///
  /// In en, this message translates to:
  /// **'KW{isoWeek}/{isoYear}'**
  String weeklyReportsIsoWeekLabel(int isoWeek, int isoYear);

  /// No description provided for @weeklyReportsProcessedOn.
  ///
  /// In en, this message translates to:
  /// **'Processed {date}'**
  String weeklyReportsProcessedOn(String date);

  /// No description provided for @navVehicleExpenses.
  ///
  /// In en, this message translates to:
  /// **'Vehicle costs'**
  String get navVehicleExpenses;

  /// No description provided for @navToolExpenses.
  ///
  /// In en, this message translates to:
  /// **'Tool costs'**
  String get navToolExpenses;

  /// No description provided for @vehicleExpensesTitle.
  ///
  /// In en, this message translates to:
  /// **'Vehicle costs'**
  String get vehicleExpensesTitle;

  /// No description provided for @vehicleExpensesEmpty.
  ///
  /// In en, this message translates to:
  /// **'No costs recorded yet.'**
  String get vehicleExpensesEmpty;

  /// No description provided for @vehicleExpensesFuelOnly.
  ///
  /// In en, this message translates to:
  /// **'Fill-ups only'**
  String get vehicleExpensesFuelOnly;

  /// No description provided for @vehicleExpensesRecord.
  ///
  /// In en, this message translates to:
  /// **'Record a cost'**
  String get vehicleExpensesRecord;

  /// No description provided for @vehicleExpensesVehicle.
  ///
  /// In en, this message translates to:
  /// **'Vehicle'**
  String get vehicleExpensesVehicle;

  /// No description provided for @vehicleExpensesKind.
  ///
  /// In en, this message translates to:
  /// **'Kind'**
  String get vehicleExpensesKind;

  /// No description provided for @vehicleExpensesAmount.
  ///
  /// In en, this message translates to:
  /// **'Amount'**
  String get vehicleExpensesAmount;

  /// No description provided for @vehicleExpensesLitres.
  ///
  /// In en, this message translates to:
  /// **'Litres'**
  String get vehicleExpensesLitres;

  /// No description provided for @vehicleExpensesOdometer.
  ///
  /// In en, this message translates to:
  /// **'Odometer (km)'**
  String get vehicleExpensesOdometer;

  /// No description provided for @vehicleExpensesSupplier.
  ///
  /// In en, this message translates to:
  /// **'Where'**
  String get vehicleExpensesSupplier;

  /// No description provided for @vehicleExpensesNote.
  ///
  /// In en, this message translates to:
  /// **'Note'**
  String get vehicleExpensesNote;

  /// No description provided for @vehicleExpensesSend.
  ///
  /// In en, this message translates to:
  /// **'Record'**
  String get vehicleExpensesSend;

  /// No description provided for @vehicleExpensesSent.
  ///
  /// In en, this message translates to:
  /// **'Cost recorded.'**
  String get vehicleExpensesSent;

  /// No description provided for @vehicleExpensesNeedsVehicle.
  ///
  /// In en, this message translates to:
  /// **'Pick the vehicle.'**
  String get vehicleExpensesNeedsVehicle;

  /// No description provided for @vehicleExpensesNeedsAmount.
  ///
  /// In en, this message translates to:
  /// **'Enter what it cost.'**
  String get vehicleExpensesNeedsAmount;

  /// No description provided for @vehicleExpensesFuelNeedsLitres.
  ///
  /// In en, this message translates to:
  /// **'Say how many litres went in.'**
  String get vehicleExpensesFuelNeedsLitres;

  /// No description provided for @vehicleExpensesOdometerHint.
  ///
  /// In en, this message translates to:
  /// **'Optional, but two readings are what give you consumption.'**
  String get vehicleExpensesOdometerHint;

  /// No description provided for @vehicleExpensesPerLitre.
  ///
  /// In en, this message translates to:
  /// **'{price} per litre'**
  String vehicleExpensesPerLitre(String price);

  /// No description provided for @vehicleExpenseKindFuel.
  ///
  /// In en, this message translates to:
  /// **'Fuel'**
  String get vehicleExpenseKindFuel;

  /// No description provided for @vehicleExpenseKindService.
  ///
  /// In en, this message translates to:
  /// **'Service'**
  String get vehicleExpenseKindService;

  /// No description provided for @vehicleExpenseKindRepair.
  ///
  /// In en, this message translates to:
  /// **'Repair'**
  String get vehicleExpenseKindRepair;

  /// No description provided for @vehicleExpenseKindInsurance.
  ///
  /// In en, this message translates to:
  /// **'Insurance'**
  String get vehicleExpenseKindInsurance;

  /// No description provided for @vehicleExpenseKindRegistration.
  ///
  /// In en, this message translates to:
  /// **'Registration'**
  String get vehicleExpenseKindRegistration;

  /// No description provided for @vehicleExpenseKindOther.
  ///
  /// In en, this message translates to:
  /// **'Other'**
  String get vehicleExpenseKindOther;

  /// No description provided for @toolExpensesTitle.
  ///
  /// In en, this message translates to:
  /// **'Tool costs'**
  String get toolExpensesTitle;

  /// No description provided for @toolExpensesEmpty.
  ///
  /// In en, this message translates to:
  /// **'No costs recorded yet.'**
  String get toolExpensesEmpty;

  /// No description provided for @toolExpensesRecord.
  ///
  /// In en, this message translates to:
  /// **'Record a cost'**
  String get toolExpensesRecord;

  /// No description provided for @toolExpensesTool.
  ///
  /// In en, this message translates to:
  /// **'Tool'**
  String get toolExpensesTool;

  /// No description provided for @toolExpensesKind.
  ///
  /// In en, this message translates to:
  /// **'Kind'**
  String get toolExpensesKind;

  /// No description provided for @toolExpensesAmount.
  ///
  /// In en, this message translates to:
  /// **'Amount'**
  String get toolExpensesAmount;

  /// No description provided for @toolExpensesSupplier.
  ///
  /// In en, this message translates to:
  /// **'Where'**
  String get toolExpensesSupplier;

  /// No description provided for @toolExpensesNote.
  ///
  /// In en, this message translates to:
  /// **'Note'**
  String get toolExpensesNote;

  /// No description provided for @toolExpensesSend.
  ///
  /// In en, this message translates to:
  /// **'Record'**
  String get toolExpensesSend;

  /// No description provided for @toolExpensesSent.
  ///
  /// In en, this message translates to:
  /// **'Cost recorded.'**
  String get toolExpensesSent;

  /// No description provided for @toolExpensesNeedsTool.
  ///
  /// In en, this message translates to:
  /// **'Pick the tool.'**
  String get toolExpensesNeedsTool;

  /// No description provided for @toolExpensesNeedsAmount.
  ///
  /// In en, this message translates to:
  /// **'Enter what it cost.'**
  String get toolExpensesNeedsAmount;

  /// No description provided for @toolExpenseKindRepair.
  ///
  /// In en, this message translates to:
  /// **'Repair'**
  String get toolExpenseKindRepair;

  /// No description provided for @toolExpenseKindMaintenance.
  ///
  /// In en, this message translates to:
  /// **'Maintenance'**
  String get toolExpenseKindMaintenance;

  /// No description provided for @toolExpenseKindCalibration.
  ///
  /// In en, this message translates to:
  /// **'Calibration'**
  String get toolExpenseKindCalibration;

  /// No description provided for @toolExpenseKindOther.
  ///
  /// In en, this message translates to:
  /// **'Other'**
  String get toolExpenseKindOther;

  /// No description provided for @companySettingsTitle.
  ///
  /// In en, this message translates to:
  /// **'Company profile'**
  String get companySettingsTitle;

  /// No description provided for @companySettingsLogo.
  ///
  /// In en, this message translates to:
  /// **'Logo'**
  String get companySettingsLogo;

  /// No description provided for @companySettingsUploadLogo.
  ///
  /// In en, this message translates to:
  /// **'Upload logo'**
  String get companySettingsUploadLogo;

  /// No description provided for @companySettingsRemoveLogo.
  ///
  /// In en, this message translates to:
  /// **'Remove logo'**
  String get companySettingsRemoveLogo;

  /// No description provided for @companySettingsLogoTooLarge.
  ///
  /// In en, this message translates to:
  /// **'The logo is larger than the {limit} MB limit.'**
  String companySettingsLogoTooLarge(int limit);

  /// No description provided for @companySettingsName.
  ///
  /// In en, this message translates to:
  /// **'Company name'**
  String get companySettingsName;

  /// No description provided for @companySettingsAddress.
  ///
  /// In en, this message translates to:
  /// **'Address'**
  String get companySettingsAddress;

  /// No description provided for @companySettingsTaxId.
  ///
  /// In en, this message translates to:
  /// **'Tax ID'**
  String get companySettingsTaxId;

  /// No description provided for @companySettingsRegistrationNumber.
  ///
  /// In en, this message translates to:
  /// **'Registration number'**
  String get companySettingsRegistrationNumber;

  /// No description provided for @companySettingsVatNumber.
  ///
  /// In en, this message translates to:
  /// **'VAT number'**
  String get companySettingsVatNumber;

  /// No description provided for @companySettingsPhone.
  ///
  /// In en, this message translates to:
  /// **'Phone'**
  String get companySettingsPhone;

  /// No description provided for @companySettingsEmail.
  ///
  /// In en, this message translates to:
  /// **'Email'**
  String get companySettingsEmail;

  /// No description provided for @companySettingsWeeklyReportsForwardEmail.
  ///
  /// In en, this message translates to:
  /// **'Forward weekly reports to'**
  String get companySettingsWeeklyReportsForwardEmail;

  /// No description provided for @companySettingsWeeklyReportsForwardEmailHint.
  ///
  /// In en, this message translates to:
  /// **'Optional — a copy of every submitted weekly report is emailed here.'**
  String get companySettingsWeeklyReportsForwardEmailHint;

  /// No description provided for @companySettingsSaved.
  ///
  /// In en, this message translates to:
  /// **'Company profile saved.'**
  String get companySettingsSaved;

  /// No description provided for @ledgersTitle.
  ///
  /// In en, this message translates to:
  /// **'Ledger'**
  String get ledgersTitle;

  /// No description provided for @ledgersEmpty.
  ///
  /// In en, this message translates to:
  /// **'No ledgers yet.'**
  String get ledgersEmpty;

  /// No description provided for @ledgersSearchHint.
  ///
  /// In en, this message translates to:
  /// **'Name…'**
  String get ledgersSearchHint;

  /// No description provided for @ledgersSectionsTitle.
  ///
  /// In en, this message translates to:
  /// **'Sections'**
  String get ledgersSectionsTitle;

  /// No description provided for @ledgersSectionEmpty.
  ///
  /// In en, this message translates to:
  /// **'No rows in this section.'**
  String get ledgersSectionEmpty;

  /// No description provided for @ledgersRowCount.
  ///
  /// In en, this message translates to:
  /// **'{count, plural, =1{1 row} other{{count} rows}}'**
  String ledgersRowCount(int count);

  /// No description provided for @ledgersNetTotal.
  ///
  /// In en, this message translates to:
  /// **'Net total'**
  String get ledgersNetTotal;

  /// No description provided for @workItemsDefectPhoto.
  ///
  /// In en, this message translates to:
  /// **'Photo'**
  String get workItemsDefectPhoto;

  /// No description provided for @workItemsDefectPhotoAdded.
  ///
  /// In en, this message translates to:
  /// **'Photo attached'**
  String get workItemsDefectPhotoAdded;

  /// No description provided for @workItemsDefectPhotoHint.
  ///
  /// In en, this message translates to:
  /// **'The picture is usually the whole report.'**
  String get workItemsDefectPhotoHint;

  /// No description provided for @workItemsDefectPhotoFailed.
  ///
  /// In en, this message translates to:
  /// **'The defect was reported, but the photo could not be attached.'**
  String get workItemsDefectPhotoFailed;

  /// No description provided for @workItemsAddPhoto.
  ///
  /// In en, this message translates to:
  /// **'Add a photo'**
  String get workItemsAddPhoto;

  /// No description provided for @failureOffline.
  ///
  /// In en, this message translates to:
  /// **'No connection to the server. Check your network and try again.'**
  String get failureOffline;

  /// No description provided for @failureTimeout.
  ///
  /// In en, this message translates to:
  /// **'The server took too long to respond. Please try again.'**
  String get failureTimeout;

  /// No description provided for @failureCancelled.
  ///
  /// In en, this message translates to:
  /// **'The request was cancelled.'**
  String get failureCancelled;

  /// No description provided for @failureCertificate.
  ///
  /// In en, this message translates to:
  /// **'The server certificate could not be verified.'**
  String get failureCertificate;

  /// No description provided for @failureBadRequest.
  ///
  /// In en, this message translates to:
  /// **'The request was rejected. Please check the entered data.'**
  String get failureBadRequest;

  /// No description provided for @failureUnauthorized.
  ///
  /// In en, this message translates to:
  /// **'Your session has expired. Please sign in again.'**
  String get failureUnauthorized;

  /// No description provided for @failureForbidden.
  ///
  /// In en, this message translates to:
  /// **'You do not have permission to perform this action.'**
  String get failureForbidden;

  /// No description provided for @failureNotFound.
  ///
  /// In en, this message translates to:
  /// **'The requested item could not be found.'**
  String get failureNotFound;

  /// No description provided for @failureConflict.
  ///
  /// In en, this message translates to:
  /// **'The action conflicts with the current data.'**
  String get failureConflict;

  /// No description provided for @failureServer.
  ///
  /// In en, this message translates to:
  /// **'The server encountered an error. Please try again later.'**
  String get failureServer;

  /// No description provided for @failureUnknown.
  ///
  /// In en, this message translates to:
  /// **'Something went wrong. Please try again.'**
  String get failureUnknown;

  /// No description provided for @crashTitle.
  ///
  /// In en, this message translates to:
  /// **'This screen could not be displayed'**
  String get crashTitle;

  /// No description provided for @crashBody.
  ///
  /// In en, this message translates to:
  /// **'Something on this screen failed while it was being drawn. Go back and try again.'**
  String get crashBody;

  /// No description provided for @offlineDataNoticeTime.
  ///
  /// In en, this message translates to:
  /// **'No connection — showing data saved at {time}.'**
  String offlineDataNoticeTime(String time);

  /// No description provided for @offlineDataNoticeDate.
  ///
  /// In en, this message translates to:
  /// **'No connection — showing data saved on {date}.'**
  String offlineDataNoticeDate(String date);

  /// No description provided for @offlineDataRetry.
  ///
  /// In en, this message translates to:
  /// **'Try again'**
  String get offlineDataRetry;

  /// No description provided for @toolLookUpHint.
  ///
  /// In en, this message translates to:
  /// **'Enter the QR code printed on the tool\'s tag.'**
  String get toolLookUpHint;

  /// No description provided for @serverAddressTitle.
  ///
  /// In en, this message translates to:
  /// **'Server address'**
  String get serverAddressTitle;

  /// No description provided for @serverAddressHint.
  ///
  /// In en, this message translates to:
  /// **'The address of your organisation\'s server. Ask whoever set it up; on the same Wi-Fi as the server it usually looks like http://192.168.1.20:5000.'**
  String get serverAddressHint;

  /// No description provided for @serverAddressLabel.
  ///
  /// In en, this message translates to:
  /// **'Address'**
  String get serverAddressLabel;

  /// No description provided for @serverAddressInvalid.
  ///
  /// In en, this message translates to:
  /// **'Enter a full address, starting with http:// or https://'**
  String get serverAddressInvalid;

  /// No description provided for @commonSave.
  ///
  /// In en, this message translates to:
  /// **'Save'**
  String get commonSave;

  /// Shown on the shift card when a clock action is queued offline.
  ///
  /// In en, this message translates to:
  /// **'Recorded on this phone. It will be sent when there is signal.'**
  String get shiftWaitingToSend;

  /// No description provided for @ackBannerHeading.
  ///
  /// In en, this message translates to:
  /// **'Confirmation needed'**
  String get ackBannerHeading;

  /// No description provided for @ackConfirmButton.
  ///
  /// In en, this message translates to:
  /// **'I\'ve seen this'**
  String get ackConfirmButton;

  /// Shown when a mutating action is blocked by an unconfirmed required notification.
  ///
  /// In en, this message translates to:
  /// **'Confirm the pending notification first'**
  String get ackGatedMessage;
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
