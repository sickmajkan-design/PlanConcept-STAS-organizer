import { CircularProgress } from '@mui/material';
import { lazy, Suspense } from 'react';
import { Navigate, Route, Routes, useLocation } from 'react-router-dom';

import { ErrorBoundary } from './components/ErrorBoundary';
import { RouteErrorFallback } from './components/RouteErrorFallback';
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
  RequireLabourCostAccess,
  RequireProjectManagerAccess,
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
const WorkItemsListPage = lazy(() =>
  import('./pages/workItems/WorkItemsListPage').then((m) => ({
    default: m.WorkItemsListPage,
  })),
);
const WorkItemFormPage = lazy(() =>
  import('./pages/workItems/WorkItemFormPage').then((m) => ({
    default: m.WorkItemFormPage,
  })),
);
const SchedulePage = lazy(() =>
  import('./pages/schedule/SchedulePage').then((m) => ({
    default: m.SchedulePage,
  })),
);
const AbsencesListPage = lazy(() =>
  import('./pages/absences/AbsencesListPage').then((m) => ({
    default: m.AbsencesListPage,
  })),
);
const CostsPage = lazy(() =>
  import('./pages/costs/CostsPage').then((m) => ({ default: m.CostsPage })),
);
const StockMovementsPage = lazy(() =>
  import('./pages/costs/StockMovementsPage').then((m) => ({
    default: m.StockMovementsPage,
  })),
);
const VehicleExpensesPage = lazy(() =>
  import('./pages/costs/VehicleExpensesPage').then((m) => ({
    default: m.VehicleExpensesPage,
  })),
);
const RatesPage = lazy(() =>
  import('./pages/costs/RatesPage').then((m) => ({ default: m.RatesPage })),
);
const FinanceEntriesPage = lazy(() =>
  import('./pages/costs/FinanceEntriesPage').then((m) => ({
    default: m.FinanceEntriesPage,
  })),
);
const AnnualRealizationPlanPage = lazy(() =>
  import('./pages/projects/AnnualRealizationPlanPage').then((m) => ({
    default: m.AnnualRealizationPlanPage,
  })),
);
const AssignmentBoardPage = lazy(() =>
  import('./pages/assignments/AssignmentBoardPage').then((m) => ({
    default: m.AssignmentBoardPage,
  })),
);
const ExpiringDocumentsPage = lazy(() =>
  import('./pages/documents/ExpiringDocumentsPage').then((m) => ({
    default: m.ExpiringDocumentsPage,
  })),
);
const TimeEntriesListPage = lazy(() =>
  import('./pages/timeEntries/TimeEntriesListPage').then((m) => ({
    default: m.TimeEntriesListPage,
  })),
);
const TimeEntryFormPage = lazy(() =>
  import('./pages/timeEntries/TimeEntryFormPage').then((m) => ({
    default: m.TimeEntryFormPage,
  })),
);
const TimeEntrySummaryPage = lazy(() =>
  import('./pages/timeEntries/TimeEntrySummaryPage').then((m) => ({
    default: m.TimeEntrySummaryPage,
  })),
);
const NotificationsPage = lazy(() =>
  import('./pages/notifications/NotificationsPage').then((m) => ({
    default: m.NotificationsPage,
  })),
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
  const location = useLocation();

  return (
    <AppLayout>
      {/* Inside the layout, so a screen that throws leaves the drawer and the
          app bar standing and the operator can navigate away from it. Keyed on
          the path so that navigating away *is* the recovery — otherwise React
          keeps the fallback mounted no matter where they go next. */}
      <ErrorBoundary
        resetKey={location.pathname}
        fallback={(props) => <RouteErrorFallback {...props} />}
      >
        <Suspense fallback={<RouteFallback />}>
          <Routes>
            <Route path={paths.home} element={<HomePage />} />
            <Route path={paths.map} element={<LiveMapPage />} />
            <Route path={paths.changePassword} element={<ChangePasswordPage />} />
            {/* Everyone with an account has an inbox, including a worker who
                only ever signs in to read one. */}
            <Route path={paths.notifications} element={<NotificationsPage />} />

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

              {/* `summary` is declared before the `:id` routes so it is matched
                  as a page rather than as an entry id. */}
              <Route path={paths.workItems} element={<WorkItemsListPage />} />
              <Route path={paths.workItemNew} element={<WorkItemFormPage />} />
              <Route path={`${paths.workItems}/:id/edit`} element={<WorkItemFormPage />} />

              <Route path={paths.costs} element={<CostsPage />} />
              <Route path={paths.stockMovements} element={<StockMovementsPage />} />
              <Route path={paths.vehicleExpenses} element={<VehicleExpensesPage />} />

              <Route path={paths.schedule} element={<SchedulePage />} />
              <Route path={paths.absences} element={<AbsencesListPage />} />

              <Route path={paths.timeEntrySummary} element={<TimeEntrySummaryPage />} />
              <Route path={paths.timeEntries} element={<TimeEntriesListPage />} />
              <Route path={paths.timeEntryNew} element={<TimeEntryFormPage />} />
              <Route path={`${paths.timeEntries}/:id/edit`} element={<TimeEntryFormPage />} />
            </Route>

            {/* Pay rates are narrower than the directory screens but wider
                than account administration: project managers price jobs. */}
            {/* Reading the roster is directory access; moving people between
                sites is a staffing call, so it gets its own narrower gate. */}
            <Route element={<RequireProjectManagerAccess />}>
              <Route path={paths.assignmentBoard} element={<AssignmentBoardPage />} />
            </Route>

            <Route element={<RequireLabourCostAccess />}>
              <Route path={paths.rates} element={<RatesPage />} />
              <Route path={paths.financeEntries} element={<FinanceEntriesPage />} />
              <Route
                path={paths.annualRealization}
                element={<AnnualRealizationPlanPage />}
              />
            </Route>

            {/* Account administration is Admin and above, a narrower set than
                the directory screens above. */}
            <Route element={<RequireAccountAdmin />}>
              {/* Expiry spans every record type and includes employee
                  documents, so it sits with account administration rather than
                  with the directory screens. */}
              <Route path={paths.expiringDocuments} element={<ExpiringDocumentsPage />} />
              <Route path={paths.users} element={<UsersListPage />} />
              <Route path={paths.userNew} element={<UserFormPage />} />
              <Route path={`${paths.users}/:id/edit`} element={<UserFormPage />} />
            </Route>

            <Route path="*" element={<Navigate to={paths.home} replace />} />
          </Routes>
        </Suspense>
      </ErrorBoundary>
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
