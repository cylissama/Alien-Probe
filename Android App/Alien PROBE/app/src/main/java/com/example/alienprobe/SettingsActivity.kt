package com.example.alienprobe

import android.content.Context
import android.content.Intent
import androidx.appcompat.app.AppCompatActivity
import android.os.Bundle
import android.view.Gravity
import android.widget.Button
import android.widget.EditText
import android.widget.TextView
import android.widget.Toast
import com.example.alienprobe.databinding.SettingsBinding

class SettingsActivity : AppCompatActivity() {

    // Lateinit var for binding
    private lateinit var binding: SettingsBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.settings)

        // Initialize binding
        binding = SettingsBinding.inflate(layoutInflater)
        setContentView(binding.root)
        loadPreferences()
        //back button to main
        val buttonClick = findViewById<Button>(R.id.btnViewSettingsToMain)
        buttonClick.setOnClickListener {
            val intent = Intent(this, MainActivity::class.java)
            startActivity(intent)
        }
        //NEED TO CHANGE THIS TO ONLY SAVE THE MODIFIED FIELDS//
        binding.saveButton.setOnClickListener {
            savePreferences(
                binding.readerUsernameInput.text.toString(),
                binding.readerPasswordInput.text.toString(),
                binding.readerIPInput.text.toString(),
                binding.readerPortInput.text.toString().toIntOrNull() ?: 0
            )
            val toast = Toast.makeText(applicationContext, "Preferences Saved", Toast.LENGTH_LONG)
            toast.setGravity(Gravity.TOP or Gravity.CENTER_HORIZONTAL, 0, 0)
            toast.show()
            loadPreferences()
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
        val savedUsername = sharedPreferences.getString("Username", "DefaultUsername")
        val savedPassword = sharedPreferences.getString("Password", "DefaultPassword")
        val savedIP = sharedPreferences.getString("IP", "DefaultIP")
        val savedPort = sharedPreferences.getInt("Port", 0)
        val readerIPInput = findViewById<TextView>(R.id.readerIPInput)
        readerIPInput.hint = savedIP
        val readerPortInput = findViewById<TextView>(R.id.readerPortInput)
        readerPortInput.hint = savedPort.toString()
        val readerUsernameInput = findViewById<TextView>(R.id.readerUsernameInput)
        readerUsernameInput.hint = savedUsername
        val readerPasswordInput = findViewById<TextView>(R.id.readerPasswordInput)
        readerPasswordInput.hint = savedPassword
    }
}