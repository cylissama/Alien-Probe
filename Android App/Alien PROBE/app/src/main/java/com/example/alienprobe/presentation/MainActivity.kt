package com.example.alienprobe.presentation

import android.os.Bundle
import androidx.activity.ComponentActivity
import android.content.Intent
import android.widget.Button
import com.example.alienprobe.R

class MainActivity : ComponentActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.main)

        setupListeners()
    }

    private fun setupListeners() {
        val scannerStartButton = findViewById<Button>(R.id.btnViewScanner)
        scannerStartButton.setOnClickListener {
            val intent = Intent(this, ScannerActivity::class.java)
            startActivity(intent)
        }

        val settingsStartButton = findViewById<Button>(R.id.btnViewSetup)
        settingsStartButton.setOnClickListener {
            val intent = Intent(this, SettingsActivity::class.java)
            startActivity(intent)
        }

        val aboutStartButton = findViewById<Button>(R.id.aboutButton)
        aboutStartButton.setOnClickListener {
            val intent = Intent(this, AboutActivity::class.java)
            startActivity(intent)
        }
    }
}
