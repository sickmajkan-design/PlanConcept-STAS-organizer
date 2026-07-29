import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/presentation/auth_controller.dart';
import '../../features/auth/presentation/change_password_screen.dart';
import '../../features/auth/presentation/forgot_password_screen.dart';
import '../../features/auth/presentation/login_screen.dart';
import '../../features/shell/presentation/home_screen.dart';
import '../../features/shell/presentation/splash_screen.dart';
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

      final isSignedIn = auth.value is Authenticated;
      final isAnonymousRoute = AppRoutes.anonymous.contains(location);

      if (!isSignedIn) {
        return isAnonymousRoute ? null : AppRoutes.login;
      }

      if (isAnonymousRoute || location == AppRoutes.splash) {
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
        path: AppRoutes.home,
        builder: (context, state) => const HomeScreen(),
      ),
      GoRoute(
        path: AppRoutes.changePassword,
        builder: (context, state) => const ChangePasswordScreen(),
      ),
    ],
  );

  ref.onDispose(() {
    router.dispose();
    authListenable.dispose();
  });

  return router;
});
