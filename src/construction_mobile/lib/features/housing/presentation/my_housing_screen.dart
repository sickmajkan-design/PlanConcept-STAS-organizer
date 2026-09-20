import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/failure_view.dart';
import '../data/housing_repository.dart';
import '../data/my_housing.dart';

class MyHousingScreen extends ConsumerWidget {
  const MyHousingScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final housing = ref.watch(myHousingProvider);

    Future<void> refresh() async {
      ref.invalidate(myHousingProvider);
      await ref.read(myHousingProvider.future);
    }

    Widget scrollable(Widget child) => ListView(
          children: [SizedBox(height: MediaQuery.of(context).size.height * 0.7, child: child)],
        );

    return Scaffold(
      appBar: AppBar(title: Text(l10n.myHousingTitle)),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: refresh,
          child: housing.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (error, _) => scrollable(FailureView(error: error, onRetry: refresh)),
            data: (value) => value == null
                ? scrollable(EmptyView(message: l10n.myHousingEmpty, icon: Icons.home_outlined))
                : _HousingDetails(housing: value),
          ),
        ),
      ),
    );
  }
}

class _HousingDetails extends StatelessWidget {
  const _HousingDetails({required this.housing});

  final MyHousing housing;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    final place = [
      if (housing.name != housing.address) housing.address,
      housing.city,
    ].whereType<String>().join(', ');

    final facts = [
      if (housing.floor != null && housing.floor!.isNotEmpty) l10n.myHousingFloor(housing.floor!),
      if (housing.rooms != null) l10n.myHousingRooms(housing.rooms!),
    ].join(' · ');

    final period = housing.upcoming
        ? l10n.myHousingStarts(formatDate(housing.startDate))
        : housing.endDate == null
            ? l10n.myHousingSince(formatDate(housing.startDate))
            : l10n.myHousingUntil(formatDate(housing.endDate));

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(housing.name, style: theme.textTheme.titleLarge),
                if (place.isNotEmpty) ...[
                  const SizedBox(height: 4),
                  SelectableText(place, style: theme.textTheme.bodyLarge),
                ],
                if (facts.isNotEmpty) ...[
                  const SizedBox(height: 4),
                  Text(facts, style: theme.textTheme.bodyMedium),
                ],
                const SizedBox(height: 12),
                Chip(
                  avatar: Icon(
                    housing.upcoming ? Icons.schedule_outlined : Icons.check_circle_outline,
                    size: 18,
                  ),
                  label: Text(period),
                ),
              ],
            ),
          ),
        ),
        if (housing.note != null && housing.note!.trim().isNotEmpty)
          _Section(
            title: l10n.myHousingNotes,
            icon: Icons.key_outlined,
            child: SelectableText(housing.note!),
          ),
        if (housing.landlordName != null || housing.landlordPhone != null)
          _Section(
            title: l10n.myHousingLandlord,
            icon: Icons.person_outline,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (housing.landlordName != null) Text(housing.landlordName!),
                if (housing.landlordPhone != null)
                  SelectableText(
                    housing.landlordPhone!,
                    style: theme.textTheme.titleMedium,
                  ),
              ],
            ),
          ),
        if (housing.roommates.isNotEmpty)
          _Section(
            title: l10n.myHousingRoommates,
            icon: Icons.groups_outlined,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [for (final name in housing.roommates) Text(name)],
            ),
          ),
      ],
    );
  }
}

class _Section extends StatelessWidget {
  const _Section({required this.title, required this.icon, required this.child});

  final String title;
  final IconData icon;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Padding(
      padding: const EdgeInsets.only(top: 12),
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(icon, color: theme.colorScheme.primary),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(title, style: theme.textTheme.titleSmall),
                    const SizedBox(height: 4),
                    child,
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
