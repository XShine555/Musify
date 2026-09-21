package com.musify.app

import androidx.compose.ui.window.Window
import androidx.compose.ui.window.application
import androidx.compose.ui.unit.DpSize
import androidx.compose.ui.window.WindowPosition
import androidx.compose.ui.window.WindowState
import androidx.compose.ui.unit.dp

fun main() = application {
    Window(
        onCloseRequest = ::exitApplication,
        title = "Musify",
        state = WindowState(size = DpSize(420.dp, 760.dp), position = WindowPosition(alignment = androidx.compose.ui.Alignment.Center))
    ) {
        App()
    }
}
