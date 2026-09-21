package com.musify.app

import io.ktor.client.HttpClient
import kotlinx.serialization.json.Json

val jsonConfig: Json = Json {
    ignoreUnknownKeys = true
    isLenient = true
}

/** Each platform installs its own engine (OkHttp/CIO/Darwin) with the same plugins. */
expect fun createHttpClient(): HttpClient

val httpClient: HttpClient by lazy { createHttpClient() }
