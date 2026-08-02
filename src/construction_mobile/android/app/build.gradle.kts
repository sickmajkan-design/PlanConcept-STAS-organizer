import java.util.Properties

plugins {
    id("com.android.application")
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

// Release signing credentials, kept outside the repository.
//
// `android/key.properties` and the keystore it points at are both git-ignored.
// Losing that keystore means losing the ability to update the app on Play at
// all — Google will not accept a rebuild signed with a different key — so it
// belongs in the organisation's password manager, not on one laptop.
// See docs/PROVISIONING.md.
val releaseKeystore: Properties? =
    rootProject.file("key.properties").takeIf { it.exists() }?.let { file ->
        Properties().apply { file.inputStream().use(::load) }
    }

// Firebase credentials are per-organisation and never committed, so the plugin
// that reads them is applied only when the file is actually there. Applying it
// unconditionally would fail the build for anyone who clones the repository,
// and CI has no Firebase project at all.
if (file("google-services.json").exists()) {
    apply(plugin = "com.google.gms.google-services")
} else {
    logger.lifecycle(
        "google-services.json not found — building without Firebase. " +
            "Push notifications will report themselves as unconfigured. " +
            "See docs/PROVISIONING.md."
    )
}

android {
    namespace = "com.planconcept.construction_mobile"
    compileSdk = flutter.compileSdkVersion
    ndkVersion = flutter.ndkVersion

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    defaultConfig {
        // TODO: Specify your own unique Application ID (https://developer.android.com/studio/build/application-id.html).
        applicationId = "com.planconcept.construction_mobile"
        // You can update the following values to match your application needs.
        // For more information, see: https://flutter.dev/to/review-gradle-config.
        minSdk = flutter.minSdkVersion
        targetSdk = flutter.targetSdkVersion
        versionCode = flutter.versionCode
        versionName = flutter.versionName
    }

    signingConfigs {
        releaseKeystore?.let { keystore ->
            // Fail here rather than three minutes into a build, and name the
            // missing key so the fix is obvious.
            val required = listOf("storeFile", "storePassword", "keyAlias", "keyPassword")
            val missing = required.filter { keystore.getProperty(it).isNullOrBlank() }

            require(missing.isEmpty()) {
                "android/key.properties is missing: ${missing.joinToString(", ")}"
            }

            // Relative paths resolve against android/, where key.properties is.
            val store = rootProject.file(keystore.getProperty("storeFile"))

            require(store.exists()) {
                "Keystore not found at ${store.absolutePath} (storeFile in android/key.properties)"
            }

            create("release") {
                storeFile = store
                storePassword = keystore.getProperty("storePassword")
                keyAlias = keystore.getProperty("keyAlias")
                keyPassword = keystore.getProperty("keyPassword")
            }
        }
    }

    buildTypes {
        release {
            // Signed with the real key when key.properties is present, and with
            // the debug key otherwise so a fresh clone and CI can still run
            // `flutter build apk --release`. A debug-signed build cannot be
            // uploaded to Play, so this cannot silently ship unsigned: the
            // upload is what fails, not the build.
            signingConfig = if (releaseKeystore != null) {
                signingConfigs.getByName("release")
            } else {
                signingConfigs.getByName("debug")
            }
        }
    }
}

kotlin {
    compilerOptions {
        jvmTarget = org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17
    }
}

flutter {
    source = "../.."
}
