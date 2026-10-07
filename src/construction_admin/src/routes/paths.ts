export const paths = {
  login: '/login',
  forgotPassword: '/forgot-password',
  resetPassword: '/reset-password',
  inviteRoute: '/invite/:token',
  invite: (token: string) => `/invite/${token}`,
  changePassword: '/change-password',

  home: '/',
  customerPortal: '/customer',
  bulletin: '/bulletin',
  employees: '/employees',
  employeeDetail: (id: string) => `/employees/${id}`,
  employeeNew: '/employees/new',
  employeeEdit: (id: string) => `/employees/${id}/edit`,
  hierarchy: '/hierarchy',

  projects: '/projects',
  projectDetail: (id: string) => `/projects/${id}`,
  projectNew: '/projects/new',
  /** Pre-selects Sub-project mode with this Main project as the parent. */
  projectNewSub: (parentProjectId: string) => `/projects/new?parentProjectId=${parentProjectId}`,
  projectEdit: (id: string) => `/projects/${id}/edit`,
  annualRealization: '/projects/annual-realization',

  customers: '/customers',
  customerNew: '/customers/new',
  customerEdit: (id: string) => `/customers/${id}/edit`,

  vehicles: '/vehicles',
  vehicleDetail: (id: string) => `/vehicles/${id}`,
  vehicleNew: '/vehicles/new',
  vehicleEdit: (id: string) => `/vehicles/${id}/edit`,

  tools: '/tools',
  toolDetail: (id: string) => `/tools/${id}`,
  toolNew: '/tools/new',
  toolEdit: (id: string) => `/tools/${id}/edit`,

  materials: '/materials',
  materialDetail: (id: string) => `/materials/${id}`,
  materialNew: '/materials/new',
  materialEdit: (id: string) => `/materials/${id}/edit`,

  timeEntries: '/time-entries',
  timeEntryNew: '/time-entries/new',
  timeEntryEdit: (id: string) => `/time-entries/${id}/edit`,
  timeEntrySummary: '/time-entries/summary',

  workItems: '/work-items',
  workItemNew: '/work-items/new',
  workItemEdit: (id: string) => `/work-items/${id}/edit`,

  schedule: '/schedule',
  absences: '/absences',
  articleOrders: '/article-orders',
  refunds: '/refunds',
  invoices: '/invoices',
  assignmentBoard: '/assignment-board',
  assignmentBoardClassic: '/assignment-board/classic',

  costs: '/costs',
  costRecords: '/cost-records',
  billingSettings: '/billing-settings',
  stockMovements: '/stock-movements',
  materialDeliveryImport: '/stock-movements/import',
  vehicleExpenses: '/vehicle-expenses',
  fuelImport: '/vehicle-expenses/fuel-import',
  fuelReconciliation: '/vehicle-expenses/dkv',
  toolExpenses: '/tool-expenses',
  rates: '/rates',
  publicHolidays: '/public-holidays',
  financeEntries: '/finance-entries',
  generalExpenses: '/general-expenses',
  companyRevenues: '/company-revenues',

  accommodationCosts: '/accommodation-costs',
  accommodations: '/accommodations',
  accommodationDetail: (id: string) => `/accommodations/${id}`,
  accommodationNew: '/accommodations/new',
  accommodationImport: '/accommodations/import',
  accommodationEdit: (id: string) => `/accommodations/${id}/edit`,

  ledgers: '/ledgers',
  ledgerDetail: (id: string) => `/ledgers/${id}`,

  companySettings: '/company-settings',

  expiringDocuments: '/documents/expiring',
  audit: '/audit',
  dataQuality: '/data-quality',
  presence: '/presence',

  scheduledReports: '/scheduled-reports',
  weeklyReports: '/weekly-reports',

  notifications: '/notifications',
  notificationGroups: '/notification-groups',
  /** Legacy addresses, kept so old bookmarks and links land on the merged page. */
  branches: '/branches',
  organization: '/organization',
  organizationCompany: '/organization?tab=company',
  organizationBranches: '/organization?tab=branches',
  notificationGroupNew: '/notification-groups/new',
  notificationGroupEdit: (id: string) => `/notification-groups/${id}/edit`,

  users: '/users',
  userNew: '/users/new',
  userEdit: (id: string) => `/users/${id}/edit`,

  map: '/map',
} as const;
