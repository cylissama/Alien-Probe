package com.example.alienprobe

import android.util.Log
import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import retrofit2.Call
import retrofit2.Callback
import retrofit2.Response

fun fetchVehicles(permitId: Int?): LiveData<List<Vehicle>> {
    val vehiclesLiveData = MutableLiveData<List<Vehicle>>()
    if (permitId != null) {
        RetrofitClient.getApiService().getVehicleListByPermit(permitId).enqueue(object :
            Callback<List<Vehicle>> {
            override fun onResponse(call: Call<List<Vehicle>>, response: Response<List<Vehicle>>) {
                if (response.isSuccessful) {
                    response.body()?.let { vehicles ->
                        Log.d("Vehicle fetched: ", vehicles[0].make + " " + vehicles[0].model + " " + vehicles[0].plate);
                        // Update LiveData with the fetched vehicles
                        vehiclesLiveData.value = vehicles
                    }
                } else {
                    Log.d("Error", "Failed to fetch vehicles: ${response.errorBody()?.string()}")
                    vehiclesLiveData.value = emptyList()
                }
            }

            override fun onFailure(call: Call<List<Vehicle>>, t: Throwable) {
                Log.d("Error", "Error fetching vehicles: ${t.message}")
                vehiclesLiveData.value = emptyList()
            }
        })
    }
    return vehiclesLiveData
}

