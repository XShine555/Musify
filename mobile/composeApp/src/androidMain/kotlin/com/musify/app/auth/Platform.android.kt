package com.musify.app.auth

import java.security.MessageDigest

actual fun sha256(input: ByteArray): ByteArray = MessageDigest.getInstance("SHA-256").digest(input)

actual fun nowEpochSeconds(): Long = System.currentTimeMillis() / 1000
