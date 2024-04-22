package com.example.alienprobe;

import android.content.Context;
import android.content.SharedPreferences;
import android.util.Log;
import android.widget.Toast;

import androidx.annotation.NonNull;

import com.alien.enterpriseRFID.reader.AlienClass1Reader;

import java.io.IOException;
import java.net.Socket;
import java.util.*;
import java.util.stream.Collectors;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

//PASS CONTEXT WHEN CREATING THIS BY USING 'this'
public class AlienScanner {
    public static String readerIP;
    public static Integer readerPort;
    public static String readerUserName;
    public static String readerPassword;
    public static AlienClass1Reader reader = new AlienClass1Reader();
    private final Context context;

    public AlienScanner(Context context) {
        this.context = context; loadPreferences(context);
    }
    public void openReader(){
        try {
            reader.setConnection(readerIP, readerPort);
            reader.setUsername(readerUserName);
            reader.setPassword(readerPassword);
            reader.open();
            System.out.println("Connection established with RFID reader.");
        } catch (Exception e) {
            System.out.println("error");
        }
    }
    public void closeReader(){
        reader.close();
        System.out.println("Connection Closed.");
    }


    public List<RFIDTag> GetTagList() {
        List<RFIDTag> outputTags = new ArrayList<>();

        callAPI();
        new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    Socket socket = new Socket(readerIP, readerPort);
                    reader.setUsername(readerUserName);
                    reader.setPassword(readerPassword);

                    if (reader.isValidateOpen()) {
                        reader.open();
                        System.out.println("connection opened");

                        Thread.sleep(100);

                        String commandOutput = reader.doReaderCommand("t");

                        System.out.println(commandOutput);
                        List<String> outputLines = Arrays.stream(commandOutput.split("\\r?\\n"))
                                .collect(Collectors.toList());

                        for (String line : outputLines) {
                            // Assuming each line represents an RFID tag
                            RFIDTag tag = new RFIDTag(line);

                            outputTags.add(tag);
                        }
                        reader.close();
                        System.out.println("connection closed");
                        socket.close();
                    }

                } catch (Exception e) {
                    System.out.println(e);
                    e.printStackTrace();
                }
            }
        }).start();
        return outputTags;
    }

    public void callAPI() {
        fetchVehicles(12345, new VehicleCallback() {
            @Override
            public void onVehicleFetched(Vehicle vehicle) {
                // Handle the fetched vehicle
                Log.d("Vehicle fetched: ", vehicle.getMake() + " " + vehicle.getModel() + " " + vehicle.getPlate());
            }

            @Override
            public void onError(String message) {
                // Handle error
                Toast.makeText(context, message, Toast.LENGTH_SHORT).show();
            }
        });
    }

    public interface VehicleCallback {
        void onVehicleFetched(Vehicle vehicle);
        void onError(String message);
    }

    private void fetchVehicles(int permitId, VehicleCallback callback) {
        RetrofitClient.getApiService().getVehicleListByPermit(permitId).enqueue(new Callback<List<Vehicle>>() {
            @Override
            public void onResponse(@NonNull Call<List<Vehicle>> call, @NonNull Response<List<Vehicle>> response) {
                if (response.isSuccessful() && response.body() != null && !response.body().isEmpty()) {
                    Vehicle firstVehicle = response.body().get(0);
                    callback.onVehicleFetched(firstVehicle);
                } else {
                    String errorMessage = "Failed to fetch vehicles: No data received";
                    if (response.errorBody() != null) {
                        try {
                            errorMessage = response.errorBody().string();
                        } catch (IOException e) {
                            e.printStackTrace();
                        }
                    }
                    callback.onError(errorMessage);
                }
            }

            @Override
            public void onFailure(Call<List<Vehicle>> call, Throwable t) {
                callback.onError("Error fetching vehicles: " + t.getMessage());
            }
        });
    }

    private void loadPreferences(Context context) {
        SharedPreferences sharedPreferences = context.getSharedPreferences("AppPreferences", Context.MODE_PRIVATE);

        readerIP = sharedPreferences.getString("IP", "DefaultIP");
        readerPort = sharedPreferences.getInt("Port", 23); // Assuming default port 23
        readerUserName = sharedPreferences.getString("Username", "DefaultUsername");
        readerPassword = sharedPreferences.getString("Password", "DefaultPassword");

        reader.setConnection(readerIP, readerPort);
        reader.setUsername(readerUserName);
        reader.setPassword(readerPassword);
    }
    public String respond(){
        return "Hello";
    }
    public static void main(String[] args) {
    }
}
