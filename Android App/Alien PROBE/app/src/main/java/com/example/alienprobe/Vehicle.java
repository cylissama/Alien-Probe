package com.example.alienprobe;

import com.google.gson.annotations.SerializedName;

public class Vehicle {
    @SerializedName("Id")
    private long id; // Using long because the Id looks quite large

    @SerializedName("Plate")
    private String plate;

    @SerializedName("Make")
    private String make;

    @SerializedName("Model")
    private String model;

    @SerializedName("Color")
    private String color;

    @Override
    public String toString() {
        return "Vehicle{" +
                "id=" + id +
                ", plate='" + plate + '\'' +
                ", make='" + make + '\'' +
                ", model='" + model + '\'' +
                ", color='" + color + '\'' +
                '}';
    }

    // Constructor
    public Vehicle(long id, String plate, String make, String model, String color) {
        this.id = id;
        this.plate = plate;
        this.make = make;
        this.model = model;
        this.color = color;
    }

    // Getters and Setters
    public long getId() {
        return id;
    }

    public void setId(long id) {
        this.id = id;
    }

    public String getPlate() {
        return plate;
    }

    public void setPlate(String plate) {
        this.plate = plate;
    }

    public String getMake() {
        return make;
    }

    public void setMake(String make) {
        this.make = make;
    }

    public String getModel() {
        return model;
    }

    public void setModel(String model) {
        this.model = model;
    }

    public String getColor() {
        return color;
    }

    public void setColor(String color) {
        this.color = color;
    }
}
