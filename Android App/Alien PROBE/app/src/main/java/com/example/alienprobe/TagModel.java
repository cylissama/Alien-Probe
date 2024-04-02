//Tag DB Model


package com.example.alienprobe;

public class TagModel {
    private int id;
    private String epc;
    private double longitude;
    private double latitude;

    public TagModel(int id, String epc, double longitude, double latitude) {
        this.id = id;
        this.epc = epc;
        this.longitude = longitude;
        this.latitude = latitude;
    }

    @Override
    public String toString() {
        return "tagModel{" +
                "id=" + id +
                ", epc='" + epc + ':' +
                '}';
    }

    public int getId() {
        return this.id;
    }

    public void setId(int id) {
        this.id = id;
    }

    public String getEPC() {
        return this.epc;
    }

    public void setEpc(String epc) {
        this.epc = epc;
    }

    public double getLatitude() { return this.latitude; }

    public double getLongitude() { return this.longitude; }
}