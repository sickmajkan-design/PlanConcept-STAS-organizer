import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/absences/presentation/my_absences_screen.dart';
import '../../features/absences/presentation/my_schedule_screen.dart';
import '../../features/costs/presentation/vehicle_expenses_screen.dart';
import '../../features/auth/presentation/auth_controller.dart';
import '../../features/auth/presentation/change_password_screen.dart';
import '../../features/auth/presentation/forgot_password_screen.dart';
import '../../features/auth/presentation/login_screen.dart';
import '../../features/employees/presentation/employee_detail_screen.dart';
import '../../features/employees/presentation/employees_screen.dart';
import '../../features/materials/presentation/material_detail_screen.dart';
import '../../features/materials/presentation/materials_screen.dart';
import '../../features/time_entries/presentation/shift_screen.dart';
import '../../features/work_items/presentation/my_work_screen.dart';
import '../../features/notifications/presentation/notifications_screen.dart';
import '../../features/projects/presentation/project_detail_screen.dart';
import '../../features/projects/presentation/projects_screen.dart';
import '../../features/shell/presentation/app_shell.dart';
import '../../features/shell/presentation/home_screen.dart';
import '../../features/shell/presentation/splash_screen.dart';
import '../../features/tools/presentation/tool_detail_screen.dart';
import '../../features/tools/presentation/tool_lookup_screen.dart';
import '../../features/tools/presentation/tools_screen.dart';
import '../../features/vehicles/presentation/vehicle_detail_screen.dart';
import '../../features/vehicles/presentation/vehicles_screen.dart';
import 'app_routes.dart';

/// Router whose redirect follows the auth state: while the stored session is
/// being restored the splash screen stays up, signed-out users are pushed to
/// the sign-in screen, and signed-in users can never sit on it.
final routerProvider = Provider<GoRouter>((ref) {
  final authListenable = ValueNotifier<AsyncValue<AuthState>>(
    const AsyncLoading(),
  );

  ref.listen<AsyncValue<AuthState>>(
    authControllerProvider,
    (_, next) => authListenable.value = next,
    fireImmediately: true,
  );

  final router = GoRouter(
    initialLocation: AppRoutes.splash,
    refreshListenable: authListenable,
    redirect: (context, state) {
      final auth = authListenable.value;
      final location = state.matchedLocation;

      // Session still being restored from secure storage.
      if (auth is! AsyncData<AuthState>) {
        return location == AppRoutes.splash ? null : AppRoutes.splash;
      }

      final authState = auth.value;
      final isAnonymousRoute = AppRoutes.anonymous.contains(location);

      if (authState is! Authenticated) {
        return isAnonymousRoute ? null : AppRoutes.login;
      }

      if (isAnonymousRoute || location == AppRoutes.splash) {
        return AppRoutes.home;
      }

      // The API serves the directory to Foreman and above only.
      if (AppRoutes.isDirectoryLocation(location) &&
          !authState.user.canViewDirectory) {
        return AppRoutes.home;
      }

      return null;
    },
    routes: [
      GoRoute(
        path: AppRoutes.splash,
        builder: (context, state) => const SplashScreen(),
      ),
      GoRoute(
        path: AppRoutes.login,
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: AppRoutes.forgotPassword,
        builder: (context, state) => const ForgotPasswordScreen(),
      ),
      GoRoute(
        path: AppRoutes.changePassword,
        builder: (context, state) => const ChangePasswordScreen(),
      ),
      // Detail screens sit above the shell so they open full-screen, which
      // also lets a notification deep-link into one from any tab.
      GoRoute(
        path: '${AppRoutes.employees}/:id',
        builder: (context, state) => EmployeeDetailScreen(
          employeeId: state.pathParameters['id']!,
        ),
      ),
      GoRoute(
        path: '${AppRoutes.projects}/:id',
        builder: (context, state) => ProjectDetailScreen(
          projectId: state.pathParameters['id']!,
        ),
      ),
      GoRoute(
        path: AppRoutes.vehicles,
        builder: (context, state) => const VehiclesScreen(),
      ),
      GoRoute(
        path: '${AppRoutes.vehicles}/:id',
        builder: (context, state) => VehicleDetailScreen(
          vehicleId: state.pathParameters['id']!,
        ),
      ),
      GoRoute(
        path: AppRoutes.tools,
        builder: (context, state) => const ToolsScreen(),
      ),
      GoRoute(
        path: '${AppRoutes.tools}/:id',
        builder: (context, state) => ToolDetailScreen(
          toolId: state.pathParameters['id']!,
        ),
      ),
      GoRoute(
        path: AppRoutes.toolLookup,
        builder: (context, state) => const ToolLookupScreen(),
      ),
      GoRoute(
        path: AppRoutes.timeEntries,
        builder: (context, state) => const ShiftScreen(),
      ),
      GoRoute(
        path: AppRoutes.workItems,
        builder: (context, state) => const MyWorkScreen(),
      ),
      GoRoute(
        path: AppRoutes.schedule,
        builder: (context, state) => const MyScheduleScreen(),
      ),
      GoRoute(
        path: AppRoutes.absences,
        builder: (context, state) => const MyAbsencesScreen(),
      ),
      GoRoute(
        path: AppRoutes.vehicleExpenses,
        builder: (context, state) => const VehicleExpensesScreen(),
      ),
      GoRoute(
        path: AppRoutes.materials,
        builder: (context, state) => const MaterialsScreen(),
      ),
      GoRoute(
        path: '${AppRoutes.materials}/:id',
        builder: (context, state) => MaterialDetailScreen(
          materialId: state.pathParameters['id']!,
        ),
      ),
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) =>
            AppShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.home,
                builder: (context, state) => const HomeScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.employees,
                builder: (context, state) => const EmployeesScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.projects,
                builder: (context, state) => const ProjectsScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.notifications,
                builder: (context, state) => const NotificationsScreen(),
              ),
            ],
          ),
        ],
      ),
    ],
  );

  ref.onDispose(() {
    router.dispose();
    authListenable.dispose();
  });

  return router;
});
