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
import {
  RequireAccountAdmin,
  RequireAuth,
  RequireDirectoryAccess,
  RequireGuest,
} from './routes/RequireAuth';

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
const UsersListPage = lazy(() =>
  import('./pages/users/UsersListPage').then((m) => ({ default: m.UsersListPage })),
);
const UserFormPage = lazy(() =>
  import('./pages/users/UserFormPage').then((m) => ({ default: m.UserFormPage })),
);
const LiveMapPage = lazy(() =>
  import('./pages/map/LiveMapPage').then((m) => ({ default: m.LiveMapPage })),
);
const VehiclesListPage = lazy(() =>
  import('./pages/vehicles/VehiclesListPage').then((m) => ({
    default: m.VehiclesListPage,
  })),
);
const VehicleFormPage = lazy(() =>
  import('./pages/vehicles/VehicleFormPage').then((m) => ({
    default: m.VehicleFormPage,
  })),
);
const VehicleDetailPage = lazy(() =>
  import('./pages/vehicles/VehicleDetailPage').then((m) => ({
    default: m.VehicleDetailPage,
  })),
);
const ToolsListPage = lazy(() =>
  import('./pages/tools/ToolsListPage').then((m) => ({ default: m.ToolsListPage })),
);
const ToolFormPage = lazy(() =>
  import('./pages/tools/ToolFormPage').then((m) => ({ default: m.ToolFormPage })),
);
const ToolDetailPage = lazy(() =>
  import('./pages/tools/ToolDetailPage').then((m) => ({ default: m.ToolDetailPage })),
);
const MaterialsListPage = lazy(() =>
  import('./pages/materials/MaterialsListPage').then((m) => ({
    default: m.MaterialsListPage,
  })),
);
const MaterialFormPage = lazy(() =>
  import('./pages/materials/MaterialFormPage').then((m) => ({
    default: m.MaterialFormPage,
  })),
);
const MaterialDetailPage = lazy(() =>
  import('./pages/materials/MaterialDetailPage').then((m) => ({
    default: m.MaterialDetailPage,
  })),
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

            <Route path={paths.vehicles} element={<VehiclesListPage />} />
            <Route path={paths.vehicleNew} element={<VehicleFormPage />} />
            <Route path={`${paths.vehicles}/:id`} element={<VehicleDetailPage />} />
            <Route path={`${paths.vehicles}/:id/edit`} element={<VehicleFormPage />} />

            <Route path={paths.tools} element={<ToolsListPage />} />
            <Route path={paths.toolNew} element={<ToolFormPage />} />
            <Route path={`${paths.tools}/:id`} element={<ToolDetailPage />} />
            <Route path={`${paths.tools}/:id/edit`} element={<ToolFormPage />} />

            <Route path={paths.materials} element={<MaterialsListPage />} />
            <Route path={paths.materialNew} element={<MaterialFormPage />} />
            <Route path={`${paths.materials}/:id`} element={<MaterialDetailPage />} />
            <Route path={`${paths.materials}/:id/edit`} element={<MaterialFormPage />} />
          </Route>

          {/* Account administration is Admin and above, a narrower set than
              the directory screens above. */}
          <Route element={<RequireAccountAdmin />}>
            <Route path={paths.users} element={<UsersListPage />} />
            <Route path={paths.userNew} element={<UserFormPage />} />
            <Route path={`${paths.users}/:id/edit`} element={<UserFormPage />} />
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
