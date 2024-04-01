package com.example.alienprobe;

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

        cv.put(COLUMN_EPC_STRING, tag.getEPC());

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
            // Loop through the cursor (result set) and create new TagModel objects. Put them into the return list.
            do {
                int tagID = cursor.getInt(0);
                System.out.println(tagID);
                String epcString = cursor.getString(1);
                System.out.println(epcString);

                TagModel tmpTag = new TagModel(tagID, epcString);
                System.out.println("New tag create with id: " + tmpTag.getId() + " epc: " + tmpTag.getEPC());

                returnList.add(i,tmpTag);
                System.out.println(returnList);
                i++;
            } while (cursor.moveToNext());
        }
        else {
        }

        cursor.close();
        db.close();
        System.out.println(returnList);
        return returnList;
    }
}
