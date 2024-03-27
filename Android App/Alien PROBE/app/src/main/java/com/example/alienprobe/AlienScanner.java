package com.example.alienprobe;

import android.content.Context;
import android.content.SharedPreferences;
import android.os.Looper;

import com.alien.enterpriseRFID.reader.AlienClass1Reader;
import com.alien.enterpriseRFID.reader.AlienReaderConnectionException;
import com.alien.enterpriseRFID.reader.AlienReaderNotValidException;
import com.alien.enterpriseRFID.reader.AlienReaderTimeoutException;
import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.io.PrintWriter;
import java.net.Socket;
import java.util.*;
import java.util.logging.Handler;
import java.util.stream.Collectors;

//PASS CONTEXT WHEN CREATING THIS BY USING 'this'
public class AlienScanner {
    public static String readerIP;
    public static Integer readerPort;
    public static String readerUserName;
    public static String readerPassword;
    public static AlienClass1Reader reader = new AlienClass1Reader();

    public AlienScanner(Context context) {
        loadPreferences(context);
        reader.setConnection("161.6.219.3", 23); // Replace with your reader's IP address
        reader.setUsername("alien"); // Add your reader's username
        reader.setPassword("password");
    }
    public void openReader(){
        try {
            reader.setConnection("161.6.219.3", 23); // Replace with your reader's IP address
            reader.setUsername("alien"); // Add your reader's username
            reader.setPassword("password");
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
        List<RFIDTag> outputTags = new ArrayList<>(); // Initialize list to store RFIDTag objects
        new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    Socket socket = new Socket(readerIP, readerPort);
                    reader.setUsername(readerUserName); // Add your reader's username
                    reader.setPassword(readerPassword);
                    reader.open();
                    System.out.println("connection opened");

                    String commandOutput = reader.doReaderCommand("t");

                    System.out.println(commandOutput);
                    List<String> outputLines = Arrays.stream(commandOutput.split("\\r?\\n"))
                            .collect(Collectors.toList());

                    // Parse outputLines and create RFIDTag objects
                    for (String line : outputLines) {
                        // Assuming each line represents an RFID tag
                        RFIDTag tag = new RFIDTag(line); // Replace null with geolocation if available

                        outputTags.add(tag);
                    }

                    System.out.println("connection closed");
                    socket.close();
                } catch (Exception e) {
                    System.out.println(e);
                    e.printStackTrace();
                }
            }
        }).start();
        return outputTags;
    }


    private void loadPreferences(Context context) {
        // Access the shared preferences
        SharedPreferences sharedPreferences = context.getSharedPreferences("AppPreferences", Context.MODE_PRIVATE);

        // Set class fields based on the stored preferences
        this.readerIP = sharedPreferences.getString("IP", "DefaultIP");
        this.readerPort = sharedPreferences.getInt("Port", 23); // Assuming default port 23
        this.readerUserName = sharedPreferences.getString("Username", "DefaultUsername");
        this.readerPassword = sharedPreferences.getString("Password", "DefaultPassword");

        // Update the reader configuration
        reader.setConnection(this.readerIP, this.readerPort);
        reader.setUsername(this.readerUserName);
        reader.setPassword(this.readerPassword);
    }

    public String respond(){
        return "Hello";
    }

    public static void main(String[] args) {
    }
}
