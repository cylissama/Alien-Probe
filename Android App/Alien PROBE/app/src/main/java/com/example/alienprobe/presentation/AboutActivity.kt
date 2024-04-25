package com.example.alienprobe.presentation

import android.content.Intent
import android.os.Bundle
import android.util.Log
import android.widget.Button
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.LiveData
import androidx.lifecycle.Observer
import com.example.alienprobe.api.ApiService
import com.example.alienprobe.R
import com.example.alienprobe.java.Vehicle
import com.example.alienprobe.api.fetchVehicles

class AboutActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.about)

        setupListeners()
    }
    private fun grabVehicle() {
        val id = 101234
        val vehicles: LiveData<List<Vehicle>> = fetchVehicles(id)
        vehicles.observe(this, Observer { vehicles ->
            // This block will be called every time the 'vehicles' LiveData changes.
            vehicles?.forEach { vehicle ->
                Log.d("Vehicle", "Make: ${vehicle.make}, Model: ${vehicle.model}, Plate: ${vehicle.plate}")
            }
        })
    }
    private fun setupListeners() {
        val backButton = findViewById<Button>(R.id.back_button)
        backButton.setOnClickListener {
            val intent = Intent(this, MainActivity::class.java)
            startActivity(intent)
        }
    }
}