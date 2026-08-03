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

  users: '/users',
  userNew: '/users/new',
  userEdit: (id: string) => `/users/${id}/edit`,

  map: '/map',
} as const;
