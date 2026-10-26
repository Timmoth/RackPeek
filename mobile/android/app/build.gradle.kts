import java.util.Properties

plugins {
    id("com.android.application")
    id("kotlin-android")
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

val releaseSigningPropertiesPath = System.getenv("RACKPEEK_ANDROID_SIGNING_PROPERTIES")
val releaseSigningProperties = Properties()

if (releaseSigningPropertiesPath != null) {
    val releaseSigningPropertiesFile = file(releaseSigningPropertiesPath)
    require(releaseSigningPropertiesFile.isFile) {
        "RACKPEEK_ANDROID_SIGNING_PROPERTIES must point to an existing properties file."
    }
    releaseSigningPropertiesFile.inputStream().use(releaseSigningProperties::load)
}

android {
    namespace = "com.rackpeek.rackpeekMobile"
    compileSdk = flutter.compileSdkVersion
    ndkVersion = flutter.ndkVersion

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = JavaVersion.VERSION_17.toString()
    }

    defaultConfig {
        // TODO: Specify your own unique Application ID (https://developer.android.com/studio/build/application-id.html).
        applicationId = "com.rackpeek.rackpeekMobile"
        // You can update the following values to match your application needs.
        // For more information, see: https://flutter.dev/to/review-gradle-config.
        minSdk = flutter.minSdkVersion
        targetSdk = flutter.targetSdkVersion
        versionCode = flutter.versionCode
        versionName = flutter.versionName
    }

    signingConfigs {
        if (releaseSigningPropertiesPath != null) {
            create("release") {
                keyAlias = releaseSigningProperties.getProperty("keyAlias")
                    ?: error("Missing keyAlias in Android signing properties.")
                keyPassword = releaseSigningProperties.getProperty("keyPassword")
                    ?: error("Missing keyPassword in Android signing properties.")
                storeFile = file(
                    releaseSigningProperties.getProperty("storeFile")
                        ?: error("Missing storeFile in Android signing properties.")
                )
                storePassword = releaseSigningProperties.getProperty("storePassword")
                    ?: error("Missing storePassword in Android signing properties.")
            }
        }
    }

    buildTypes {
        release {
            signingConfig = if (releaseSigningPropertiesPath == null) {
                signingConfigs.getByName("debug")
            } else {
                signingConfigs.getByName("release")
            }
        }
    }
}

flutter {
    source = "../.."
}
