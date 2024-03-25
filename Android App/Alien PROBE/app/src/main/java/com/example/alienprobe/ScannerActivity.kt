package com.example.alienprobe

import android.content.Intent
import android.os.Bundle
import android.widget.Button
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.ToggleButton
import androidx.appcompat.app.AppCompatActivity

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
        val isScanner = findViewById<ToggleButton>(R.id.toggleScanner)
        buttonClick.setOnClickListener {

            //make AlienGetTagList() object
            val tagGetter = AlienGetTagList()

            // Check whether toggle is checked or not
            if (isScanner.isChecked) {
                //open reader
                tagGetter.openReader();

                // Get the tag list (Is static so get from class not the instance/object)
                val tagList = AlienGetTagList.GetTagList()

                // add value to scrollView
                tagList?.forEach { tag ->
                    val textView = TextView(this)
                    textView.text = tag
                    linearLayout.addView(textView)
                }
            } else {
                // close the reader
                tagGetter.closeReader();
            }

        }
        */

    }
}