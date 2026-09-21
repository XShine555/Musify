import org.jetbrains.kotlin.gradle.ExperimentalKotlinGradlePluginApi
import org.jetbrains.kotlin.gradle.dsl.JvmTarget
import org.jetbrains.kotlin.gradle.plugin.mpp.KotlinNativeTarget

plugins {
    alias(libs.plugins.kotlinMultiplatform)
    alias(libs.plugins.androidApplication)
    alias(libs.plugins.composeMultiplatform)
    alias(libs.plugins.composeCompiler)
    alias(libs.plugins.kotlinSerialization)
}

// Backend connection settings, read from gradle.properties so every target
// (Android, Desktop, iOS) is generated with the same values without
// duplicating them in three places. See mobile/README.md.
val apiBaseUrl: String = providers.gradleProperty("musify.apiBaseUrl").get()
val authIssuer: String = providers.gradleProperty("musify.authIssuer").get()
val authClientId: String = providers.gradleProperty("musify.authClientId").get()

// JavaFX Media's os/arch classifier — the demo only needs to run on this box, but
// this keeps `./gradlew :composeApp:run` working on any dev machine unmodified.
val javafxClassifier: String = run {
    val os = org.gradle.internal.os.OperatingSystem.current()
    val arch = System.getProperty("os.arch")
    when {
        os.isWindows -> "win"
        os.isMacOsX -> if (arch.contains("aarch64")) "mac-aarch64" else "mac"
        else -> if (arch.contains("aarch64")) "linux-aarch64" else "linux"
    }
}

kotlin {
    androidTarget {
        @OptIn(ExperimentalKotlinGradlePluginApi::class)
        compilerOptions {
            jvmTarget.set(JvmTarget.JVM_11)
        }
    }

    jvm("desktop")

    listOf(
        iosX64(),
        iosArm64(),
        iosSimulatorArm64()
    ).forEach { iosTarget: KotlinNativeTarget ->
        iosTarget.binaries.framework {
            baseName = "ComposeApp"
            isStatic = true
        }
    }

    sourceSets {
        val commonMain by getting {
            dependencies {
                implementation(compose.runtime)
                implementation(compose.foundation)
                implementation(compose.material3)
                implementation(compose.ui)
                implementation(compose.components.resources)
                implementation(compose.components.uiToolingPreview)

                implementation(libs.kotlinx.coroutines.core)
                implementation(libs.kotlinx.serialization.json)

                implementation(libs.ktor.client.core)
                implementation(libs.ktor.client.content.negotiation)
                implementation(libs.ktor.serialization.kotlinx.json)
                implementation(libs.ktor.client.logging)

                implementation(libs.coil.compose)
                implementation(libs.coil.network.ktor)
            }
        }

        val androidMain by getting {
            dependencies {
                implementation(libs.ktor.client.okhttp)
                implementation(libs.androidx.activity.compose)
                implementation(libs.androidx.browser)
                implementation(libs.androidx.datastore.preferences)
                implementation(libs.androidx.media3.exoplayer)
                implementation(libs.androidx.media3.common)
            }
        }

        val desktopMain by getting {
            dependencies {
                implementation(compose.desktop.currentOs)
                implementation(libs.ktor.client.cio)
                // MediaPlayer only, via Platform.startup() — no javafx-swing/AWT needed.
                // OpenJFX's sibling-module POM dependencies aren't classifier-aware in
                // Gradle, so javafx-base/-graphics must be pinned explicitly too, or
                // Gradle resolves their classifier-less (empty) stub jars instead.
                val javafxVersion = libs.versions.javafx.get()
                implementation("org.openjfx:javafx-base:$javafxVersion:$javafxClassifier")
                implementation("org.openjfx:javafx-graphics:$javafxVersion:$javafxClassifier")
                implementation("org.openjfx:javafx-media:$javafxVersion:$javafxClassifier")
            }
        }

        val iosMain by creating {
            dependsOn(commonMain)
            dependencies {
                implementation(libs.ktor.client.darwin)
            }
        }
        getByName("iosX64Main").dependsOn(iosMain)
        getByName("iosArm64Main").dependsOn(iosMain)
        getByName("iosSimulatorArm64Main").dependsOn(iosMain)
    }
}

android {
    namespace = "com.musify.app"
    compileSdk = libs.versions.compileSdk.get().toInt()

    defaultConfig {
        applicationId = "com.musify.app"
        minSdk = libs.versions.minSdk.get().toInt()
        targetSdk = libs.versions.targetSdk.get().toInt()
        versionCode = 1
        versionName = "1.0"

        buildConfigField("String", "API_BASE_URL", "\"$apiBaseUrl\"")
        buildConfigField("String", "AUTH_ISSUER", "\"$authIssuer\"")
        buildConfigField("String", "AUTH_CLIENT_ID", "\"$authClientId\"")
    }
    buildFeatures {
        buildConfig = true
        compose = true
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_11
        targetCompatibility = JavaVersion.VERSION_11
    }
    packaging {
        resources.excludes.add("/META-INF/{AL2.0,LGPL2.1}")
    }
}

compose.desktop {
    application {
        mainClass = "com.musify.app.MainKt"
        jvmArgs += listOf(
            "-Dmusify.apiBaseUrl=$apiBaseUrl",
            "-Dmusify.authIssuer=$authIssuer",
            "-Dmusify.authClientId=$authClientId"
        )
        nativeDistributions {
            targetFormats(org.jetbrains.compose.desktop.application.dsl.TargetFormat.Msi)
            packageName = "MusifyMobile"
            packageVersion = "1.0.0"
        }
    }
}
