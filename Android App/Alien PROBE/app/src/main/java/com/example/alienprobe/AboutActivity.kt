package com.example.alienprobe

import android.content.Intent
import android.os.Bundle
import android.util.Log
import android.widget.Button
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
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
        fetchVehicles(id) // Call the API with the hexadecimal string ID
    }

    private fun fetchVehicles(permitId: Int) {
        RetrofitClient.getApiService().getVehicleListByPermit(permitId).enqueue(object : Callback<List<Vehicle>> {
            override fun onResponse(call: Call<List<Vehicle>>, response: Response<List<Vehicle>>) {
                if (response.isSuccessful) {
                    response.body()?.let { vehicles ->
                        Toast.makeText(this@AboutActivity, "Vehicles fetched successfully", Toast.LENGTH_SHORT).show()
                        Log.d("Vehicles: ", vehicles[0].make + " " + vehicles[0].model + " " + vehicles[0].plate)
                    }
                } else {
                    Toast.makeText(this@AboutActivity, "Failed to fetch vehicles: ${response.errorBody()?.string()}", Toast.LENGTH_SHORT).show()
                }
            }

            override fun onFailure(call: Call<List<Vehicle>>, t: Throwable) {
                Toast.makeText(this@AboutActivity, "Error fetching vehicles: ${t.message}", Toast.LENGTH_SHORT).show()
                Log.d("error", "${t.message}")
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
