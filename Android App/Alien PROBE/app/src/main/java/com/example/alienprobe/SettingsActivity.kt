package com.example.alienprobe

import android.content.Context
import android.content.Intent
import androidx.appcompat.app.AppCompatActivity
import android.os.Bundle
import android.widget.Button
import android.widget.Toast
import com.example.alienprobe.databinding.SettingsLayoutBinding

class SettingsActivity : AppCompatActivity() {

    // Lateinit var for binding
    private lateinit var binding: SettingsLayoutBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.settings_layout)

        // Initialize binding
        binding = SettingsLayoutBinding.inflate(layoutInflater)
        setContentView(binding.root)

        loadPreferences()

        //back button to main
        val buttonClick = findViewById<Button>(R.id.btnViewSettingsToMain)
        buttonClick.setOnClickListener {
            val intent = Intent(this, MainActivity::class.java)
            startActivity(intent)
        }

        //button listener for save
        binding.saveButton.setOnClickListener {
            savePreferences(
                binding.readerUsernameInput.text.toString(),
                binding.readerPasswordInput.text.toString(),
                binding.readerIPInput.text.toString(),
                binding.readerPortInput.text.toString().toIntOrNull() ?: 0
            )
            Toast.makeText(this,"Preferences Saved",Toast.LENGTH_SHORT).show()
            loadPreferences() // Load and display the updated preferences
        }
    }

    private fun savePreferences(
        username: String,
        password: String,
        ip: String,
        port: Int,
        )
    {
        // Open Shared Preferences editor to save values
        val sharedPreferences = getSharedPreferences("AppPreferences", Context.MODE_PRIVATE)
        val editor = sharedPreferences.edit()

        editor.putString("Username",username)
        editor.putString("Password",password)
        editor.putString("IP",ip)
        editor.putInt("Port",port)

        editor.apply()
    }
    private fun loadPreferences() {
        val sharedPreferences = getSharedPreferences("AppPreferences", Context.MODE_PRIVATE)

        // Fetch the saved preferences
        val savedUsername = sharedPreferences.getString("Username", "DefaultUsername")
        val savedPassword = sharedPreferences.getString("Password", "DefaultPassword")
        val savedIP = sharedPreferences.getString("IP", "DefaultIP")
        val savedPort = sharedPreferences.getInt("Port", 0)

        binding.displayUsername.text = savedUsername
        binding.displayPassword.text = savedPassword
        binding.displayIP.text = savedIP
        binding.displayPort.text = savedPort.toString()
    }
}
