/// A site the signed-in employee may clock in to today.
class ClockInSite {
  const ClockInSite({required this.id, required this.name});

  factory ClockInSite.fromJson(Map<String, dynamic> json) => ClockInSite(
        id: json['id'] as String,
        name: json['name'] as String,
      );

  final String id;
  final String name;
}
