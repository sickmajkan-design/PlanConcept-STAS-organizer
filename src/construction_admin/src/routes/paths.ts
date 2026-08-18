export const paths = {
  login: '/login',
  forgotPassword: '/forgot-password',
  resetPassword: '/reset-password',
  changePassword: '/change-password',

  home: '/',
  employees: '/employees',
  employeeDetail: (id: string) => `/employees/${id}`,
  employeeNew: '/employees/new',
  employeeEdit: (id: string) => `/employees/${id}/edit`,

  projects: '/projects',
  projectDetail: (id: string) => `/projects/${id}`,
  projectNew: '/projects/new',
  projectEdit: (id: string) => `/projects/${id}/edit`,

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

  costs: '/costs',
  stockMovements: '/stock-movements',
  vehicleExpenses: '/vehicle-expenses',
  rates: '/rates',
  financeEntries: '/finance-entries',

  expiringDocuments: '/documents/expiring',

  notifications: '/notifications',

  users: '/users',
  userNew: '/users/new',
  userEdit: (id: string) => `/users/${id}/edit`,

  map: '/map',
} as const;
