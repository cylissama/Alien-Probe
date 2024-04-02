//Tag Model for ScrollView

package com.example.alienprobe;

public class RFIDTag {
    private String epc;
    // Constructor
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

}
