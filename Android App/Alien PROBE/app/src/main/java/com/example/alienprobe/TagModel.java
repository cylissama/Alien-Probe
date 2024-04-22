//Tag DB Model
package com.example.alienprobe;

import androidx.annotation.NonNull;

public class TagModel {
    private int id;
    private String epc;
    private String time;
    private final double longitude;
    private final double latitude;
    private final String vehicle;

    public TagModel(int id, String epc, double longitude, double latitude, String time, String vehicle) {
        this.id = id;
        this.epc = epc;
        this.longitude = longitude;
        this.latitude = latitude;
        this.time = time;
        this.vehicle = vehicle;
    }
    @NonNull
    @Override
    public String toString() {
        return "tagModel{" +
                "id=" + id +
                ", epc='" + epc + '\'' +
                ", longitude=" + longitude +
                ", latitude=" + latitude +
                ", time=" + time +
                ", vehicle=" + vehicle +
                '}';
    }
    public int getId() {
        return this.id;
    }
    public void setId(int id) {
        this.id = id;
    }
    public void setTime(String time) { this.time = time; }
    public String getTime() {return this.time; }
    public String getEPC() {
        return this.epc;
    }
    public void setEpc(String epc) {
        this.epc = epc;
    }
    public double getLatitude() { return this.latitude; }
    public double getLongitude() { return this.longitude; }
    public String getVehicle() { return this.vehicle; }
}