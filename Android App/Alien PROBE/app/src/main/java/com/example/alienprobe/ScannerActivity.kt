package com.example.alienprobe

import android.content.Intent
import android.os.Bundle
import android.widget.Button
import android.widget.LinearLayout
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity

//import RFIDTag class

class ScannerActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_scanner)

        val buttonClick = findViewById<Button>(R.id.btnViewScanToMain)
        buttonClick.setOnClickListener {
            val intent = Intent(this, MainActivity::class.java)
            startActivity(intent)
        }

        // Get reference to the LinearLayout inside ScrollView
        val linearLayout = findViewById<LinearLayout>(R.id.linearLayout)

        // Add 5 text items to the LinearLayout
        for (i in 1..25) {
            val textView = TextView(this)
            textView.text = "Item $i"
            linearLayout.addView(textView)
        }

        /*
        //start scanning for tags
        val buttonClick = findViewById<Button>(R.id.)
        buttonClick.setOnClickListener {
            val intent = Intent(this, MainActivity::class.java)
            startActivity(intent)

            //???
            val tagGetter = AlienGetTagList();
        }
        */

        //stop scanning for tags


    }
}