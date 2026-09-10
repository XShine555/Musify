package com.musify.app

import androidx.compose.ui.window.ComposeUIViewController
import platform.UIKit.UIViewController

/** Called from Swift's ContentView — see mobile/README.md for the Xcode-side wrapper. */
fun MainViewController(): UIViewController = ComposeUIViewController { App() }
