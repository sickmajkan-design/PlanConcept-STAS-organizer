import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import '../../attachments/data/models/attachment.dart';
import '../../auth/presentation/auth_controller.dart';

/// A toll a vehicle carries: where, until when, paid or not.
class MyVehicleToll {
  const MyVehicleToll({
    required this.type,
    required this.country,
    this.routeSegment,
    required this.paid,
    this.validUntil,
  });

  factory MyVehicleToll.fromJson(Map<String, dynamic> json) => MyVehicleToll(
    type: json['type'] as String? ?? 'Vignette',
    country: json['country'] as String? ?? '',
    routeSegment: json['routeSegment'] as String?,
    paid: json['status'] == 'Paid',
    validUntil: _date(json['validUntil']),
  );

  final String type;
  final String country;
  final String? routeSegment;
  final bool paid;
  final DateTime? validUntil;
}

/// A vehicle the caller currently drives, with only what a driver needs on the road.
class MyVehicle {
  const MyVehicle({
    required this.id,
    required this.name,
    required this.registrationNumber,
    this.registrationValidUntil,
    this.technicalInspectionValidUntil,
    this.insuranceValidUntil,
    this.tolls = const [],
    this.documents = const [],
  });

  factory MyVehicle.fromJson(Map<String, dynamic> json) => MyVehicle(
    id: json['id'] as String,
    name: json['name'] as String? ?? '',
    registrationNumber: json['registrationNumber'] as String? ?? '',
    registrationValidUntil: _date(json['registrationValidUntil']),
    technicalInspectionValidUntil: _date(json['technicalInspectionValidUntil']),
    insuranceValidUntil: _date(json['insuranceValidUntil']),
    tolls: _list(json['tolls']).map(MyVehicleToll.fromJson).toList(),
    documents: _list(json['documents']).map(Attachment.fromJson).toList(),
  );

  final String id;
  final String name;
  final String registrationNumber;
  final DateTime? registrationValidUntil;
  final DateTime? technicalInspectionValidUntil;
  final DateTime? insuranceValidUntil;
  final List<MyVehicleToll> tolls;
  final List<Attachment> documents;
}

/// Everything that is the caller's: documents the office shared with them, and their vehicles.
class MyDocuments {
  const MyDocuments({this.documents = const [], this.vehicles = const []});

  factory MyDocuments.fromJson(Map<String, dynamic> json) => MyDocuments(
    documents: _list(json['documents']).map(Attachment.fromJson).toList(),
    vehicles: _list(json['vehicles']).map(MyVehicle.fromJson).toList(),
  );

  final List<Attachment> documents;
  final List<MyVehicle> vehicles;

  bool get isEmpty => documents.isEmpty && vehicles.isEmpty;
}

List<Map<String, dynamic>> _list(Object? value) =>
    (value as List<dynamic>? ?? const []).cast<Map<String, dynamic>>();

DateTime? _date(Object? value) =>
    value is String ? DateTime.tryParse(value) : null;

class MyDocumentsRepository extends ApiRepository {
  const MyDocumentsRepository(super.dio);

  Future<MyDocuments> fetch() {
    return guard(() async {
      final response = await dio.get<Map<String, dynamic>>(
        '/api/v1/attachments/mine',
      );

      return MyDocuments.fromJson(response.data ?? const {});
    });
  }
}

final myDocumentsRepositoryProvider = Provider<MyDocumentsRepository>((ref) {
  return MyDocumentsRepository(ref.watch(apiClientProvider));
});

/// Depends on the signed-in user so the next person to sign in on this phone never sees the
/// previous one's documents.
final myDocumentsProvider = FutureProvider.autoDispose<MyDocuments>((ref) {
  ref.watch(currentUserProvider);

  return ref.watch(myDocumentsRepositoryProvider).fetch();
});
