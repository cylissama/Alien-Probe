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
        //clear button
        val clearClick = findViewById<Button>(R.id.btnScannerClear)
        clearClick.setOnClickListener {
            try {
                reader.openReader()
            } catch (e: Exception) {
                Toast.makeText(this, "Error", Toast.LENGTH_SHORT).show()
            }
        }

        val linearLayout = findViewById<LinearLayout>(R.id.linearLayout)

        //toggle button
        val toggleOnOff = findViewById<ToggleButton>(R.id.toggleScanner)
        toggleOnOff.setOnCheckedChangeListener { _, isChecked ->
            if (isChecked) {
                println("Switch On")
            } else {
                println("END OF THING")
            }
        }
        //save button
        val saveClick = findViewById<Button>(R.id.btnScannerSave)
        saveClick.setOnClickListener {

            tagList = reader.GetTagList()

            Thread.sleep(1000)

            println("YOO")

            for(tag in tagList) {
                println(tag.epc)
                Log.d("TAG", tag.epc)
            }

            // Manually create an RFIDTag object for testing
            val testTag = RFIDTag("TestEPC")

            // Add the testTag to the tagList
            //tagList.add(testTag)

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