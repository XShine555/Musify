package com.musify.app

import android.content.Context

/**
 * Set once from `MainActivity.onCreate`. Simpler than threading a Context
 * through every constructor for a demo this size (DataStore, ExoPlayer and
 * Custom Tabs all need one); a real app would use DI instead.
 */
object AppContextHolder {
    lateinit var appContext: Context
}
