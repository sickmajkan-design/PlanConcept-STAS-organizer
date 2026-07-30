import { CircularProgress } from '@mui/material';
import { lazy, Suspense } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';

import { AppLayout } from './layout/AppLayout';
import { ChangePasswordPage } from './pages/auth/ChangePasswordPage';
import { ForgotPasswordPage } from './pages/auth/ForgotPasswordPage';
import { LoginPage } from './pages/auth/LoginPage';
import { ResetPasswordPage } from './pages/auth/ResetPasswordPage';
import { HomePage } from './pages/HomePage';
import { paths } from './routes/paths';
import { RequireAuth, RequireDirectoryAccess, RequireGuest } from './routes/RequireAuth';

// Code-split the heaviest dependencies (DataGrid, Google Maps) out of the
// initial bundle — they only load when the operator actually visits them.
const EmployeesListPage = lazy(() =>
  import('./pages/employees/EmployeesListPage').then((m) => ({
    default: m.EmployeesListPage,
  })),
);
const EmployeeFormPage = lazy(() =>
  import('./pages/employees/EmployeeFormPage').then((m) => ({
    default: m.EmployeeFormPage,
  })),
);
const EmployeeDetailPage = lazy(() =>
  import('./pages/employees/EmployeeDetailPage').then((m) => ({
    default: m.EmployeeDetailPage,
  })),
);
const ProjectsListPage = lazy(() =>
  import('./pages/projects/ProjectsListPage').then((m) => ({
    default: m.ProjectsListPage,
  })),
);
const ProjectFormPage = lazy(() =>
  import('./pages/projects/ProjectFormPage').then((m) => ({
    default: m.ProjectFormPage,
  })),
);
const ProjectDetailPage = lazy(() =>
  import('./pages/projects/ProjectDetailPage').then((m) => ({
    default: m.ProjectDetailPage,
  })),
);
const LiveMapPage = lazy(() =>
  import('./pages/map/LiveMapPage').then((m) => ({ default: m.LiveMapPage })),
);

function RouteFallback() {
  return (
    <div style={{ display: 'flex', justifyContent: 'center', paddingTop: 96 }}>
      <CircularProgress />
    </div>
  );
}

function Layout() {
  return (
    <AppLayout>
      <Suspense fallback={<RouteFallback />}>
        <Routes>
          <Route path={paths.home} element={<HomePage />} />
          <Route path={paths.map} element={<LiveMapPage />} />
          <Route path={paths.changePassword} element={<ChangePasswordPage />} />

          <Route element={<RequireDirectoryAccess />}>
            <Route path={paths.employees} element={<EmployeesListPage />} />
            <Route path={paths.employeeNew} element={<EmployeeFormPage />} />
            <Route path={`${paths.employees}/:id`} element={<EmployeeDetailPage />} />
            <Route path={`${paths.employees}/:id/edit`} element={<EmployeeFormPage />} />

            <Route path={paths.projects} element={<ProjectsListPage />} />
            <Route path={paths.projectNew} element={<ProjectFormPage />} />
            <Route path={`${paths.projects}/:id`} element={<ProjectDetailPage />} />
            <Route path={`${paths.projects}/:id/edit`} element={<ProjectFormPage />} />
          </Route>

          <Route path="*" element={<Navigate to={paths.home} replace />} />
        </Routes>
      </Suspense>
    </AppLayout>
  );
}

export function App() {
  return (
    <Routes>
      <Route element={<RequireGuest />}>
        <Route path={paths.login} element={<LoginPage />} />
        <Route path={paths.forgotPassword} element={<ForgotPasswordPage />} />
        <Route path={paths.resetPassword} element={<ResetPasswordPage />} />
      </Route>

      <Route element={<RequireAuth />}>
        <Route path="/*" element={<Layout />} />
      </Route>
    </Routes>
  );
}
