package com.example.alienprobe;

import android.content.Context;
import android.content.SharedPreferences;
import com.alien.enterpriseRFID.reader.AlienClass1Reader;
import com.alien.enterpriseRFID.reader.AlienReaderConnectionException;
import com.alien.enterpriseRFID.reader.AlienReaderNotValidException;
import com.alien.enterpriseRFID.reader.AlienReaderTimeoutException;

import java.util.*;
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
    }
    public void openReader(){
        try {
            reader.setConnection(readerIP, readerPort); // Replace with your reader's IP address
            reader.setUsername(readerUserName); // Add your reader's username
            reader.setPassword(readerPassword);
            reader.open();
            System.out.println("Connection established with RFID reader.");
        } catch (AlienReaderNotValidException e) {
            throw new RuntimeException(e);
        } catch (AlienReaderTimeoutException e) {
            throw new RuntimeException(e);
        } catch (AlienReaderConnectionException e) {
            throw new RuntimeException(e);
        }
    }
    public void closeReader(){
        reader.close();
        System.out.println("Connection Closed.");

    }

    public static List<String> GetTagList(){
        try {
            String commandOutput = reader.doReaderCommand("t");
            List<String> outputLines = Arrays.stream(commandOutput.split("\\r?\\n"))
                    .collect(Collectors.toList());
            return outputLines;
        } catch (Exception e) {
            System.out.println(e);
            e.printStackTrace();
        }
        return null;
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

    public static void main(String[] args) {
    }
}
