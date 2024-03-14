package com.example.alienprobe

import android.content.Intent
import android.os.Bundle
import android.widget.Button
import androidx.appcompat.app.AppCompatActivity

//import RFIDTag class

class ScannerActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_scanner)

        val buttonClick = findViewById<Button>(R.id.btnViewMain)
        buttonClick.setOnClickListener {
            val intent = Intent(this, MainActivity::class.java)
            startActivity(intent)
        }

        //start scanning for tags
        val buttonClick = findViewById<Button>(R.id.)
        buttonClick.setOnClickListener {
            val intent = Intent(this, MainActivity::class.java)
            startActivity(intent)

            //???
            val tagGetter = AlienGetTagList();
        }

        //stop scanning for tags


    }
}