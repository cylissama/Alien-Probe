package com.example.alienprobe

import android.content.Intent
import android.os.Bundle
import android.widget.Button
import android.widget.ToggleButton
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import android.util.Log

//import RFIDTag class
var tagList: MutableList<RFIDTag> = mutableListOf()

class ScannerActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.scanner)

        var reader = AlienScanner(this)

        //back button
        val buttonClick = findViewById<Button>(R.id.btnViewScanToMain)
        buttonClick.setOnClickListener {
            val intent = Intent(this, MainActivity::class.java)
            startActivity(intent)
        }

        val linearLayout = findViewById<LinearLayout>(R.id.linearLayout)

        //clear button
        val clearClick = findViewById<Button>(R.id.btnScannerClear)
        clearClick.setOnClickListener {
            //clear tag list
            tagList.clear()
            linearLayout.removeAllViews()
        }

        //toggle button
        val toggleOnOff = findViewById<ToggleButton>(R.id.toggleScanner)
        toggleOnOff.setOnCheckedChangeListener { _, isChecked ->
            if(isChecked) {
                println("Switch On")
            } else {
                println("END OF THING")
                //close connection
            }
        }
        //save button
        val saveClick = findViewById<Button>(R.id.btnScannerSave)
        saveClick.setOnClickListener {

            // Create a temporary list to hold tags temporarily while checking for duplicates
            var tempTagList: MutableList<RFIDTag> = mutableListOf()
            tempTagList = reader.GetTagList()

            Thread.sleep(1000)

            // Check for duplicates before adding to the main tagList
            for (tempTag in tempTagList) {
                if (!tagList.any { it.epc == tempTag.epc }) {
                    tagList.add(tempTag)
                }
            }

            // Clear the layout
            linearLayout.removeAllViews()

            //Add tags to linear layout
            if (tagList.isNotEmpty()) {
                for (tag in tagList) {
                    val textView = TextView(this).apply {
                        text = "EPC: ${tag.getEpc()}" // Accessing EPC data from RFIDTag object
                    }
                    linearLayout.addView(textView)
                }
            } else {
                // If tagList is empty, display a placeholder or error message
                val textView = TextView(this).apply {
                    text = "No tags found."
                }
                linearLayout.addView(textView)
            }
        }
    }

    fun updateTagList(tagList: List<String>) {
        val linearLayout = findViewById<LinearLayout>(R.id.linearLayout)
        linearLayout.removeAllViews() // Clear previous views
        for (tag in tagList) {
            val textView = TextView(this).apply {
                text = tag
                // Optional: add styling here
            }
            linearLayout.addView(textView)
        }

        if (tagList.isEmpty()) {
            // If tagList is empty, display a placeholder or error message
            val textView = TextView(this).apply {
                text = "No tags found."
                // Optional: add styling here
            }
            linearLayout.addView(textView)
        }
    }
}