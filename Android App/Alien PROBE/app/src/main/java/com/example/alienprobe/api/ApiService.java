package com.example.alienprobe.api;

import com.example.alienprobe.java.Vehicle;

import java.util.List;

import retrofit2.Call;
import retrofit2.http.GET;
import retrofit2.http.Query;

public interface ApiService {
    @GET("Lookup/GetVehicleListByPermit")
    Call<List<Vehicle>> getVehicleListByPermit(@Query("PermitNumber") int permitId);
}
