package com.example.alienprobe

import android.Manifest
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Bundle
import android.view.Gravity
import android.widget.Button
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.ActivityCompat
import com.example.alienprobe.databinding.SettingsBinding
import com.google.android.gms.location.FusedLocationProviderClient
import com.google.android.gms.location.LocationServices

class SettingsActivity : AppCompatActivity() {

    // Lateinit var for binding
    private lateinit var binding: SettingsBinding
    private lateinit var fusedLocationClient: FusedLocationProviderClient

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.settings)

        // Initialize binding
        binding = SettingsBinding.inflate(layoutInflater)
        setContentView(binding.root)
        loadPreferences()

        fusedLocationClient = LocationServices.getFusedLocationProviderClient(this)

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
    private fun getLocation() {
        if (ActivityCompat.checkSelfPermission(
                this,
                Manifest.permission.ACCESS_FINE_LOCATION
            ) != PackageManager.PERMISSION_GRANTED && ActivityCompat.checkSelfPermission(
                this,
                Manifest.permission.ACCESS_COARSE_LOCATION
            ) != PackageManager.PERMISSION_GRANTED
        ) {
            // TODO: Consider calling
            //    ActivityCompat#requestPermissions
            // here to request the missing permissions, and then overriding
            //   public void onRequestPermissionsResult(int requestCode, String[] permissions,
            //                                          int[] grantResults)
            // to handle the case where the user grants the permission. See the documentation
            // for ActivityCompat#requestPermissions for more details.
            return
        }
        fusedLocationClient.lastLocation.addOnSuccessListener(this) { location ->
            // Got last known location. In some rare situations, this can be null.
            location?.let {
                // Logic to handle location object
            }
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