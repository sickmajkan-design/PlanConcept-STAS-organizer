package com.planconcept.construction_mobile

import android.view.WindowManager
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

class MainActivity : FlutterActivity() {
    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)

        // Keeps the screen out of screenshots, recordings and the recent-apps thumbnail while a
        // sensitive document is open; see SecureScreen in the Dart code.
        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, "construction/secure_screen")
            .setMethodCallHandler { call, result ->
                if (call.method == "set") {
                    val secure = call.arguments as? Boolean ?: false

                    if (secure) {
                        window.setFlags(
                            WindowManager.LayoutParams.FLAG_SECURE,
                            WindowManager.LayoutParams.FLAG_SECURE
                        )
                    } else {
                        window.clearFlags(WindowManager.LayoutParams.FLAG_SECURE)
                    }

                    result.success(null)
                } else {
                    result.notImplemented()
                }
            }
    }
}
