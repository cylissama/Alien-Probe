package com.example.alienprobe;

import android.content.Context;
import android.content.SharedPreferences;

import com.alien.enterpriseRFID.reader.AlienClass1Reader;
import com.alien.enterpriseRFID.reader.AbstractReader;


import java.net.Socket;
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

    private void loadPreferences(Context context) {

        SharedPreferences sharedPreferences = context.getSharedPreferences("AppPreferences", Context.MODE_PRIVATE);

        this.readerIP = sharedPreferences.getString("IP", "DefaultIP");
        this.readerPort = sharedPreferences.getInt("Port", 23); // Assuming default port 23
        this.readerUserName = sharedPreferences.getString("Username", "DefaultUsername");
        this.readerPassword = sharedPreferences.getString("Password", "DefaultPassword");

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
