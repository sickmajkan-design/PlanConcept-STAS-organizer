/// Mirrors the API's `MyHousingDto`: where the caller lives, without any cost.
class MyHousing {
  const MyHousing({
    required this.name,
    required this.address,
    required this.startDate,
    required this.upcoming,
    required this.roommates,
    this.roommateContacts = const [],
    this.city,
    this.floor,
    this.rooms,
    this.landlordName,
    this.landlordPhone,
    this.note,
    this.endDate,
  });

  factory MyHousing.fromJson(Map<String, dynamic> json) {
    return MyHousing(
      name: json['name'] as String,
      address: json['address'] as String,
      city: json['city'] as String?,
      floor: json['floor'] as String?,
      rooms: json['rooms'] as int?,
      landlordName: json['landlordName'] as String?,
      landlordPhone: json['landlordPhone'] as String?,
      note: json['note'] as String?,
      startDate: DateTime.parse(json['startDate'] as String),
      endDate: json['endDate'] == null ? null : DateTime.parse(json['endDate'] as String),
      upcoming: json['upcoming'] as bool? ?? false,
      roommates: (json['roommates'] as List<dynamic>? ?? const []).cast<String>(),
      roommateContacts: (json['roommateContacts'] as List<dynamic>? ?? const [])
          .map((c) => Roommate.fromJson(c as Map<String, dynamic>))
          .toList(),
    );
  }

  final String name;
  final String address;
  final String? city;
  final String? floor;
  final int? rooms;
  final String? landlordName;
  final String? landlordPhone;
  final String? note;
  final DateTime startDate;
  final DateTime? endDate;
  final bool upcoming;
  final List<String> roommates;

  /// The same people with their phone numbers. Empty from a server that predates them.
  final List<Roommate> roommateContacts;
}

/// Someone already living where the caller is housed, with the phone to reach them on.
class Roommate {
  const Roommate({required this.name, this.phone});

  factory Roommate.fromJson(Map<String, dynamic> json) =>
      Roommate(name: json['name'] as String, phone: json['phone'] as String?);

  final String name;
  final String? phone;
}
