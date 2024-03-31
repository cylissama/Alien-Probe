package com.example.alienprobe;

public class TagModel {
    private int id;
    private static String epc;

    public TagModel(int id, String epc) {
        this.id = id;
        this.epc = epc;
    }

    @Override
    public String toString() {
        return "tagModel{" +
                "id=" + id +
                ", epc='" + epc + '\'' +
                '}';
    }

    public int getId() {
        return id;
    }

    public void setId(int id) {
        this.id = id;
    }

    public static String getEPC() {
        return epc;
    }

    public void setEpc(String epc) {
        this.epc = epc;
    }
}