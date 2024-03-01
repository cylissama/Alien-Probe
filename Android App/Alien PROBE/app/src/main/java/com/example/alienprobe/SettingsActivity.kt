package com.example.alienprobe

import android.content.Context
import androidx.appcompat.app.AppCompatActivity
import android.os.Bundle
import com.example.alienprobe.databinding.SettingsLayoutBinding

class SettingsActivity : AppCompatActivity() {

    // Lateinit var for binding
    private lateinit var binding: SettingsLayoutBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        // Initialize binding
        binding = SettingsLayoutBinding.inflate(layoutInflater)
        setContentView(binding.root)

        // Set up click listener for the save button
        binding.saveButton.setOnClickListener {
            savePreferences()
        }
    }

    private fun savePreferences() {
        // Get user inputs
        val ip = binding.readerIPInput.text.toString()
        val port = binding.readerPortInput.text.toString().toIntOrNull() ?: 0 // Default to 0 if conversion fails
        val username = binding.readerUsernameInput.text.toString()
        val password = binding.readerPasswordInput.text.toString()

        // Open Shared Preferences editor to save values
        val sharedPreferences = getSharedPreferences("AppPreferences", Context.MODE_PRIVATE)
        with(sharedPreferences.edit()) {
            putString("IP", ip)
            putInt("Port", port)
            putString("Username", username)
            putString("Password", password)
            apply() // Apply changes asynchronously
        }
    }
}
