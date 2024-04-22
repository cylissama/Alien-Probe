package com.example.alienprobe

import android.content.Intent
import android.os.Bundle
import android.util.Log
import android.widget.Button
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.LiveData
import androidx.lifecycle.Observer
import retrofit2.Call
import retrofit2.Callback
import retrofit2.Response

class AboutActivity : AppCompatActivity() {

    private lateinit var apiService: ApiService
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.about)

        setupListeners()

        val id = 101234
        val vehicles: LiveData<List<Vehicle>> = fetchVehicles(id)
        vehicles.observe(this, Observer { vehicles ->
            // This block will be called every time the 'vehicles' LiveData changes.
            vehicles?.forEach { vehicle ->
                Log.d("Vehicle", "Make: ${vehicle.make}, Model: ${vehicle.model}, Plate: ${vehicle.plate}")
            }
        })    }

    private fun setupListeners() {
        val backButton = findViewById<Button>(R.id.back_button)
        backButton.setOnClickListener {
            val intent = Intent(this, MainActivity::class.java)
            startActivity(intent)
        }
    }
}
