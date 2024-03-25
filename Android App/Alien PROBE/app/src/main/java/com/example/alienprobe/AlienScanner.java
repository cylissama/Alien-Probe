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
        reader.setConnection("161.6.141.148", 23); // Replace with your reader's IP address
        reader.setUsername("alien"); // Add your reader's username
        reader.setPassword("password");
    }
    public void openReader(){
        try {
            reader.setConnection("161.6.141.148", 23); // Replace with your reader's IP address
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
    List<String> outputLines;
    public List<String> GetTagList(){
        new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    Socket socket = new Socket("161.6.141.148", 23);
                    PrintWriter out = new PrintWriter(socket.getOutputStream(), true);
                    BufferedReader input = new BufferedReader(new InputStreamReader(socket.getInputStream()));

                    String commandOutput = reader.doReaderCommand("t");
                    List<String> outputLines = Arrays.stream(commandOutput.split("\\r?\\n"))
                            .collect(Collectors.toList());
                    System.out.println(outputLines);

                    socket.close();
                } catch (Exception e) {
                    System.out.println(e);
                    e.printStackTrace();
                }
            }
        }).start();

        return outputLines;
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
