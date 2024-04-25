// Tag Model for ScrollView
// This has the vehicle attribute but it is not used.
// The plan was to have a JSON call to the DB and then store the data in a Vehicle object attached
// to an RFID tag that would be added to the local DB

package com.example.alienprobe.java;

import androidx.annotation.NonNull;

public class RFIDTag {
    private String epc;
    private Vehicle vehicle;
    public RFIDTag(String epc) {
        this.epc = epc;
    }
    @NonNull
    @Override
    public String toString() {
        return "RFIDTag{" +
                "EPC = " + this.epc;
    }
    public void setEpc(String epc) {
        this.epc = epc;
    }
    public String getEPC() {
        return this.epc;
    }
    public Vehicle getVehicle(){ return this.vehicle; }

}
