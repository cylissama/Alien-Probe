//Tag Model for ScrollView

package com.example.alienprobe;

public class RFIDTag {
    private String epc;
    private String time;
    private double latitude;
    private double longitude;


    // Constructor
    public RFIDTag(String line) {

        //pasre data
        String[] parsed = line.split(",");

        //add parsed string data to object
        this.epc = parsed[0];
        this.time = parsed[1];
        this.longitude = Double.parseDouble(parsed[2]);
        this.latitude = Double.parseDouble(parsed[3]);
    }
    // Getter for EPC
    @Override
    public String toString() {
        return "RFIDTag{" +
                "EPC = " + this.epc + ", " + this.time +
                ", " + this.longitude + ", " + this.latitude;
    }



    public void setEpc(String epc) {
        this.epc = epc;
    }

    public String getEPC() {
        return this.epc;
    }

    public String getTime() {
        return this.time;
    }

    public double getLatitude() { return this.latitude; }

    public double getLongitude() { return this.longitude; }

}
