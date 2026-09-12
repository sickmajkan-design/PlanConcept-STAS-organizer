import 'package:flutter/material.dart';

/// The icon for a notification's `type` — shared between the inbox list and
/// the detail screen so the two never drift apart.
IconData notificationTypeIcon(String type) => switch (type) {
      'ProjectAssigned' => Icons.apartment,
      'EmployeeAssigned' => Icons.person_add_alt,
      'VehicleAssigned' => Icons.local_shipping_outlined,
      'ToolAssigned' => Icons.handyman_outlined,
      'DirectMessage' => Icons.chat_bubble_outline,
      'GeneralAnnouncement' => Icons.campaign_outlined,
      _ => Icons.notifications_outlined,
    };
