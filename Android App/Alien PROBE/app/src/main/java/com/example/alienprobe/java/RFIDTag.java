//Tag Model for ScrollView

package com.example.alienprobe.java;

public class RFIDTag {
    private String epc;
    private Vehicle vehicle;
    // Constructor
    public RFIDTag(String epc, Vehicle vehicle) {
        this.epc = epc;
        this.vehicle = vehicle;
    }
    public RFIDTag(String epc) {
        this.epc = epc;
    }
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
