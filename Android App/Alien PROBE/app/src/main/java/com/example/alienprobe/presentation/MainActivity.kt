package com.example.alienprobe.presentation

import android.Manifest
import android.content.Context
import android.content.Intent
import android.content.SharedPreferences
import android.content.pm.PackageManager
import android.os.Bundle
import android.widget.Button
import android.widget.Toast
import androidx.activity.ComponentActivity
import androidx.activity.result.ActivityResultLauncher
import androidx.activity.result.contract.ActivityResultContracts
import androidx.core.content.ContextCompat
import com.example.alienprobe.R

class MainActivity : ComponentActivity() {
    private lateinit var permissionLauncher: ActivityResultLauncher<Array<String>>
    private lateinit var sharedPreferences: SharedPreferences
    companion object {
        const val PERMISSIONS_GRANTED_KEY = "permissions_granted"
    }
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.main)

        initializePermissions()

        setupListeners()
    }
    private fun obtainPrefs() {
        sharedPreferences = getSharedPreferences("MyAppPreferences", Context.MODE_PRIVATE)
    }
    private fun setupListeners() {
        val scannerStartButton = findViewById<Button>(R.id.btnViewScanner)
        scannerStartButton.setOnClickListener {
            startActivity(Intent(this, ScannerActivity::class.java))
        }
        val settingsStartButton = findViewById<Button>(R.id.btnViewSetup)
        settingsStartButton.setOnClickListener {
            startActivity(Intent(this, SetupActivity::class.java))
        }
        val aboutStartButton = findViewById<Button>(R.id.aboutButton)
        aboutStartButton.setOnClickListener {
            startActivity(Intent(this, AboutActivity::class.java))
        }
    }
    private fun initializePermissions() {
        permissionLauncher = registerForActivityResult(
            ActivityResultContracts.RequestMultiplePermissions()
        ) { permissions ->
            if (permissions.values.all { it }) {
                Toast.makeText(this, "Permissions are granted.", Toast.LENGTH_SHORT).show()
                sharedPreferences.edit().putBoolean(PERMISSIONS_GRANTED_KEY, true).apply()
            } else {
                Toast.makeText(this, "Some permissions are not granted.", Toast.LENGTH_SHORT).show()
                sharedPreferences.edit().putBoolean(PERMISSIONS_GRANTED_KEY, false).apply()
            }
        }
        val requiredPermissions = mutableListOf<String>()
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION) != PackageManager.PERMISSION_GRANTED) {
            requiredPermissions.add(Manifest.permission.ACCESS_FINE_LOCATION)
        }
        if (requiredPermissions.isNotEmpty()) {
            permissionLauncher.launch(requiredPermissions.toTypedArray())
        }
    }
}