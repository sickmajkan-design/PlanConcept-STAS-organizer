import '../../l10n/app_localizations.dart';
import '../utils/formatting.dart';

/// Which enum a value belongs to.
///
/// The kind has to be supplied rather than inferred from the value, because
/// the same value means different things: `Available` is a vehicle status and
/// a tool status, and Serbian inflects them differently — a vehicle is
/// "slobodno", a tool "slobodan". English used one word for both, which is why
/// this never came up before.
enum EnumKind {
  role,
  employeeStatus,
  projectStatus,
  vehicleStatus,
  toolStatus,
  fuelType,
  notificationType,
  timeEntryStatus,
  workType,
  attachmentCategory,
}

/// Translates one of the API's enum values.
///
/// A value this build has not been taught yet falls back to the readable
/// English form rather than showing a raw key, so a status added on the server
/// does not blank out a screen.
String enumLabel(AppLocalizations l10n, EnumKind kind, String? value) {
  if (value == null || value.isEmpty) {
    return l10n.commonNotSet;
  }

  final label = switch ((kind, value)) {
    (EnumKind.role, 'SuperAdmin') => l10n.roleSuperAdmin,
    (EnumKind.role, 'Admin') => l10n.roleAdmin,
    (EnumKind.role, 'ProjectManager') => l10n.roleProjectManager,
    (EnumKind.role, 'Foreman') => l10n.roleForeman,
    (EnumKind.role, 'Worker') => l10n.roleWorker,
    (EnumKind.employeeStatus, 'Active') => l10n.employeeStatusActive,
    (EnumKind.employeeStatus, 'OnLeave') => l10n.employeeStatusOnLeave,
    (EnumKind.employeeStatus, 'Suspended') => l10n.employeeStatusSuspended,
    (EnumKind.employeeStatus, 'Terminated') => l10n.employeeStatusTerminated,
    (EnumKind.projectStatus, 'Planned') => l10n.projectStatusPlanned,
    (EnumKind.projectStatus, 'Active') => l10n.projectStatusActive,
    (EnumKind.projectStatus, 'OnHold') => l10n.projectStatusOnHold,
    (EnumKind.projectStatus, 'Completed') => l10n.projectStatusCompleted,
    (EnumKind.projectStatus, 'Cancelled') => l10n.projectStatusCancelled,
    (EnumKind.vehicleStatus, 'Available') => l10n.vehicleStatusAvailable,
    (EnumKind.vehicleStatus, 'Assigned') => l10n.vehicleStatusAssigned,
    (EnumKind.vehicleStatus, 'InService') => l10n.vehicleStatusInService,
    (EnumKind.vehicleStatus, 'OutOfService') => l10n.vehicleStatusOutOfService,
    (EnumKind.toolStatus, 'Available') => l10n.toolStatusAvailable,
    (EnumKind.toolStatus, 'Assigned') => l10n.toolStatusAssigned,
    (EnumKind.toolStatus, 'UnderRepair') => l10n.toolStatusUnderRepair,
    (EnumKind.toolStatus, 'Lost') => l10n.toolStatusLost,
    (EnumKind.toolStatus, 'Retired') => l10n.toolStatusRetired,
    (EnumKind.fuelType, 'Petrol') => l10n.fuelPetrol,
    (EnumKind.fuelType, 'Diesel') => l10n.fuelDiesel,
    (EnumKind.fuelType, 'Electric') => l10n.fuelElectric,
    (EnumKind.fuelType, 'Hybrid') => l10n.fuelHybrid,
    (EnumKind.fuelType, 'Lpg') => l10n.fuelLpg,
    (EnumKind.notificationType, 'EmployeeAssigned') => l10n.notificationTypeEmployeeAssigned,
    (EnumKind.notificationType, 'ProjectAssigned') => l10n.notificationTypeProjectAssigned,
    (EnumKind.notificationType, 'ToolAssigned') => l10n.notificationTypeToolAssigned,
    (EnumKind.notificationType, 'VehicleAssigned') => l10n.notificationTypeVehicleAssigned,
    (EnumKind.notificationType, 'Announcement') => l10n.notificationTypeAnnouncement,
    (EnumKind.timeEntryStatus, 'InProgress') => l10n.timeEntryStatusInProgress,
    (EnumKind.timeEntryStatus, 'Submitted') => l10n.timeEntryStatusSubmitted,
    (EnumKind.timeEntryStatus, 'Approved') => l10n.timeEntryStatusApproved,
    (EnumKind.timeEntryStatus, 'Rejected') => l10n.timeEntryStatusRejected,
    (EnumKind.workType, 'Regular') => l10n.workTypeRegular,
    (EnumKind.workType, 'Overtime') => l10n.workTypeOvertime,
    (EnumKind.workType, 'Weekend') => l10n.workTypeWeekend,
    (EnumKind.workType, 'PublicHoliday') => l10n.workTypePublicHoliday,
    (EnumKind.workType, 'Travel') => l10n.workTypeTravel,
    (EnumKind.attachmentCategory, 'Contract') => l10n.attachmentCategoryContract,
    (EnumKind.attachmentCategory, 'Certificate') => l10n.attachmentCategoryCertificate,
    (EnumKind.attachmentCategory, 'MedicalCheck') => l10n.attachmentCategoryMedicalCheck,
    (EnumKind.attachmentCategory, 'Licence') => l10n.attachmentCategoryLicence,
    (EnumKind.attachmentCategory, 'Insurance') => l10n.attachmentCategoryInsurance,
    (EnumKind.attachmentCategory, 'SiteDocument') => l10n.attachmentCategorySiteDocument,
    (EnumKind.attachmentCategory, 'Photo') => l10n.attachmentCategoryPhoto,
    (EnumKind.attachmentCategory, 'Other') => l10n.attachmentCategoryOther,
    _ => null,
  };

  return label ?? humanizeEnum(value);
}
