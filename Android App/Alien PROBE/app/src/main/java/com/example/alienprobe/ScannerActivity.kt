package com.example.alienprobe

import android.content.Intent
import android.os.Bundle
import android.widget.Button
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity

//import RFIDTag class
var tagList: List<String>? = null
class ScannerActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_scanner)

        //setup the reader
        var reader = AlienScanner(this)
        Toast.makeText(this, reader.respond(), Toast.LENGTH_SHORT).show()

        val buttonClick = findViewById<Button>(R.id.btnViewScanToMain)
        buttonClick.setOnClickListener {
            val intent = Intent(this, MainActivity::class.java)
            startActivity(intent)
        }
        val clearClick = findViewById<Button>(R.id.btnScannerClear)
        clearClick.setOnClickListener {
            try {
                reader.openReader()
            } catch (e: Exception) {
                Toast.makeText(this, "Error", Toast.LENGTH_SHORT).show()
            }
        }

        // Get reference to the LinearLayout inside ScrollView
        val linearLayout = findViewById<LinearLayout>(R.id.linearLayout)

        tagList?.let {
            for (tag in it) {
                val textView = TextView(this).apply {
                    text = tag
                    // Optional: add styling here if needed
                }
                linearLayout.addView(textView)
            }
        } ?: run {
            // If tagList is null or empty, display a placeholder or error message
            val textView = TextView(this).apply {
                text = "No tags found."
                // Optional: add styling here if needed
            }
            linearLayout.addView(textView)
        }

    }
}