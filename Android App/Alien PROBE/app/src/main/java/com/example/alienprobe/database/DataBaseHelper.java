package com.example.alienprobe.database;

import android.content.ContentValues;
import android.content.Context;
import android.database.sqlite.SQLiteConstraintException;
import android.database.sqlite.SQLiteDatabase;
import android.database.sqlite.SQLiteOpenHelper;
import android.database.Cursor;

import java.util.ArrayList;
import java.util.List;

import androidx.annotation.Nullable;

public class DataBaseHelper extends SQLiteOpenHelper {
    public static final String RFIDTAG_TABLE = "RFIDTAG_TABLE";
    public static final String COLUMN_ID = "ID";
    public static final String COLUMN_EPC_STRING = "EPC_STRING";
    public static final String COLUMN_LAT_DOUBLE = "LATITUDE";
    public static final String COLUMN_LONG_DOUBLE = "LONGITUDE";
    public static final String COLUMN_TIME = "TIME";
    public static final String COLUMN_VEHICLE = "VEHICLE";
    public DataBaseHelper(@Nullable Context context) {
        super(context, "RF" + COLUMN_ID + "Tag.db", null, 1);
    }
    @Override
    public void onCreate(SQLiteDatabase db) {
        String createTableStatement = "CREATE TABLE " + RFIDTAG_TABLE + " (" +
                COLUMN_ID + " INTEGER PRIMARY KEY AUTOINCREMENT, " +
                COLUMN_EPC_STRING + " TEXT NOT NULL UNIQUE, " +
                COLUMN_LONG_DOUBLE + " DOUBLE NOT NULL, " +
                COLUMN_LAT_DOUBLE + " DOUBLE NOT NULL, " +
                COLUMN_TIME + " TEXT NOT NULL, " +
                COLUMN_VEHICLE + " TEXT NOT NULL)";
        db.execSQL(createTableStatement);
    }
    @Override
    public void onUpgrade(SQLiteDatabase db, int oldVersion, int newVersion) {}
    public boolean addOne(TagModel tag) {
        SQLiteDatabase db = this.getWritableDatabase();
        ContentValues cv = new ContentValues();

        cv.put(COLUMN_EPC_STRING, tag.getEPC());
        cv.put(COLUMN_LAT_DOUBLE, tag.getLongitude());
        cv.put(COLUMN_LONG_DOUBLE, tag.getLatitude());
        cv.put(COLUMN_TIME, tag.getTime());
        cv.put(COLUMN_VEHICLE, tag.getVehicle().toString());

        try {
            // insertOrThrow() will throw SQLiteConstraintException if a UNIQUE constraint is violated
            db.insertOrThrow(RFIDTAG_TABLE, null, cv);
            return true;
        } catch (SQLiteConstraintException e) {
            // You could log or handle the specific SQLiteConstraintException here if needed
            return false;
        } finally {
            db.close();
        }
    }
    public boolean deleteTag(String tagId) {
        SQLiteDatabase db = this.getWritableDatabase();
        return db.delete(RFIDTAG_TABLE, "id = ?", new String[]{tagId}) > 0;
    }
    public List<TagModel> getAllTags() {
        List<TagModel> returnList = new ArrayList<>();

        // Get data from the database
        String queryString = "SELECT * FROM " + RFIDTAG_TABLE;
        SQLiteDatabase db = this.getReadableDatabase();
        Cursor cursor = db.rawQuery(queryString, null);

        int i = 0;
        if (cursor.moveToFirst()) {
            do {
                int tagID = cursor.getInt(0);
                String epcString = cursor.getString(1);
                double longitude = cursor.getDouble(2);
                double latitude = cursor.getDouble(3);
                String time = cursor.getString(4);
                String vehicle = cursor.getString(5);

                TagModel tmpTag = new TagModel(tagID, epcString, longitude, latitude, time, vehicle);
                System.out.println("New tag create with id: " + tmpTag.getId() + " epc: " + tmpTag.getEPC());

                returnList.add(i,tmpTag);
                System.out.println(returnList);
                i++;
            } while (cursor.moveToNext());
        }

        cursor.close();
        db.close();
        System.out.println(returnList);
        return returnList;
    }
}
