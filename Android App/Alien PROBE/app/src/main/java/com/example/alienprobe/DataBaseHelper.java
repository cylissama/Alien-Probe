package com.example.alienprobe;

import android.content.ContentValues;
import android.content.Context;
import android.database.sqlite.SQLiteConstraintException;
import android.database.sqlite.SQLiteDatabase;
import android.database.sqlite.SQLiteOpenHelper;

import androidx.annotation.Nullable;

public class DataBaseHelper extends SQLiteOpenHelper {
    public static final String COLUMN_ID = "ID";
    public static final String RFIDTAG_TABLE = "RFIDTAG_TABLE";
    public static final String COLUMN_EPC_STRING = "EPC_STRING";

    public DataBaseHelper(@Nullable Context context) {
        super(context, "RF" + COLUMN_ID + "Tag.db", null, 1);
    }

    @Override
    public void onCreate(SQLiteDatabase db) {
        String createTableStatement = "CREATE TABLE " + RFIDTAG_TABLE + " (" + COLUMN_ID + " INTEGER PRIMARY KEY AUTOINCREMENT, " + COLUMN_EPC_STRING + " TEXT NOT NULL UNIQUE)";

        db.execSQL(createTableStatement);
    }

    @Override
    public void onUpgrade(SQLiteDatabase db, int oldVersion, int newVersion) {

    }

    public boolean addOne(TagModel tag) {
        SQLiteDatabase db = this.getWritableDatabase();
        ContentValues cv = new ContentValues();

        cv.put(COLUMN_EPC_STRING, TagModel.getEPC());

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
}
