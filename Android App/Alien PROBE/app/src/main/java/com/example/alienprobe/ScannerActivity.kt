package com.example.alienprobe

import android.content.Intent
import android.database.sqlite.SQLiteConstraintException
import android.os.Bundle
import android.widget.Button
import android.widget.ToggleButton
import android.widget.LinearLayout
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import android.util.Log
import android.media.MediaPlayer

//import RFIDTag class
var tagList: MutableList<RFIDTag> = mutableListOf()

class ScannerActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.scanner)

        val reader = AlienScanner(this)

        val linearLayout = findViewById<LinearLayout>(R.id.linearLayout)

        /// BUTTON LISTENERS ///
        //back button
        val buttonClick = findViewById<Button>(R.id.btnViewScanToMain)
        buttonClick.setOnClickListener {
            val intent = Intent(this, MainActivity::class.java)
            startActivity(intent)
        }
        //view tags button
        val viewTags = findViewById<Button>(R.id.viewTagsButton)
        viewTags.setOnClickListener {
            val intent = Intent(this, ViewTagsActivity::class.java)
            startActivity(intent)
        }
        //clear button
        val clearClick = findViewById<Button>(R.id.btnScannerClear)
        clearClick.setOnClickListener {
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
            }
        }
        // getListButton
        val getList = findViewById<Button>(R.id.getTagListButton)
        getList.setOnClickListener {

            val mediaPlayer = MediaPlayer.create(this, R.raw.alien_blaster)
            mediaPlayer.start() // Play the sound effect
            mediaPlayer.setOnCompletionListener {
                it.release() // Release the MediaPlayer resource once the sound is completed
            }

            val dataBaseHelper: DataBaseHelper = DataBaseHelper(this)

            var tempTagList: MutableList<RFIDTag> = mutableListOf()
            tempTagList = reader.GetTagList()

            Thread.sleep(500)

            for (tempTag in tempTagList) {
                if (!tagList.any { it.epc == tempTag.epc }) {
                    tagList.add(tempTag)
                }
                //add condition to remove from list
            }

            linearLayout.removeAllViews()

            //Add tags to linear layout
            if (tagList.isNotEmpty()) {
                for (tag in tagList) {

                    //Add Tag Data to Scroll View
                    val textView = TextView(this).apply {
                        text = "EPC: ${tag.getEpc()}" // Accessing EPC data from RFIDTag object
                    }
                    linearLayout.addView(textView)

                    //Add Tag data to Database
                    try {
                        val tagModel: TagModel = TagModel(-1, "${tag.getEpc()}")
                        val success = dataBaseHelper.addOne(tagModel)
                        if (success) {
                            val textView = TextView(this).apply {
                                text = "EPC: ${tag.getEpc()}"
                            }
                            linearLayout.addView(textView)
                        }
                    } catch (e: SQLiteConstraintException) {
                        // Handle the duplicate entry case, maybe log it or inform the user
                        Log.d("Insertion", "Duplicate EPC: ${tag.getEpc()} not added.")
                    } catch (e: Exception) {
                        // Handle other exceptions
                        val textView = TextView(this).apply {
                            text = "Error adding tag: ${e.message}"
                        }
                        linearLayout.addView(textView)
                    }
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