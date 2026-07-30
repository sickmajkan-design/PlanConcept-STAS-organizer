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

  map: '/map',
} as const;
