package com.musify.app.auth

import kotlinx.cinterop.ExperimentalForeignApi
import kotlinx.cinterop.addressOf
import kotlinx.cinterop.convert
import kotlinx.cinterop.refTo
import kotlinx.cinterop.usePinned
import platform.CoreCrypto.CC_SHA256
import platform.CoreCrypto.CC_SHA256_DIGEST_LENGTH
import platform.Foundation.NSDate
import platform.Foundation.timeIntervalSince1970

@OptIn(ExperimentalForeignApi::class)
actual fun sha256(input: ByteArray): ByteArray {
    val digest = UByteArray(CC_SHA256_DIGEST_LENGTH)
    input.usePinned { pinned ->
        CC_SHA256(pinned.addressOf(0), input.size.convert(), digest.refTo(0))
    }
    return digest.toByteArray()
}

actual fun nowEpochSeconds(): Long = NSDate().timeIntervalSince1970.toLong()
