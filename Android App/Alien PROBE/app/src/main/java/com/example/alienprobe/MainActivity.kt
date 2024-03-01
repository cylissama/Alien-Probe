package com.example.alienprobe

import AppPreferences
import PreferencesManager
import android.content.Context
import android.os.Bundle
import androidx.activity.ComponentActivity
import android.content.Intent
import android.content.SharedPreferences
import android.widget.Button

class MainActivity : ComponentActivity() {

    //setup shared prefs for the IP/Port/User/Pass
    private lateinit var sharedPreferences: SharedPreferences
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main) // Inflating activity_main.xml layout

        sharedPreferences=this.getSharedPreferences("userPreferences", Context.MODE_PRIVATE)

        val buttonClick = findViewById<Button>(R.id.btnViewScanner)
        buttonClick.setOnClickListener {
            val intent = Intent(this, ScannerActivity::class.java)
            startActivity(intent)
        }

        val settingsClick = findViewById<Button>(R.id.btnViewSetup)
        settingsClick.setOnClickListener {
            val intent = Intent(this, SettingsActivity::class.java)
            startActivity(intent)
        }

        //set this up to where it is bound to textbox input
        val appPreferences = AppPreferences(ip = "192.168.1.1", port = 8080, username = "user", password = "pass")
        val preferencesManager = PreferencesManager(this) // 'context' is your Activity or Application context
        preferencesManager.savePreferences(appPreferences)
    }
}
