package com.example.alienprobe

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.database.sqlite.SQLiteConstraintException
import android.media.MediaPlayer
import android.net.Uri
import android.os.Bundle
import android.provider.Settings
import android.util.Log
import android.widget.Button
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast
import android.widget.ToggleButton
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import androidx.appcompat.app.AlertDialog

//import RFIDTag class
var tagList: MutableList<RFIDTag> = mutableListOf()

class ScannerActivity : AppCompatActivity() {

    companion object {
        private const val LOCATION_PERMISSION_REQUEST_CODE = 1
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.scanner)

        checkAndRequestLocationPermissions()

        setupUI()

    }
    private fun setupUI() {
        //make sure to initialize this here or else scannerActivity will crash
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
                        val long: Double = 0.0
                        val lat: Double = 0.0
                        val tagModel: TagModel = TagModel(-1, "${tag.getEpc()}", long, lat)
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
    private fun checkAndRequestLocationPermissions() {
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION) != PackageManager.PERMISSION_GRANTED ||
            ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_COARSE_LOCATION) != PackageManager.PERMISSION_GRANTED) {

            ActivityCompat.requestPermissions(this, arrayOf(Manifest.permission.ACCESS_FINE_LOCATION, Manifest.permission.ACCESS_COARSE_LOCATION), LOCATION_PERMISSION_REQUEST_CODE)
        }
    }

    override fun onRequestPermissionsResult(requestCode: Int, permissions: Array<out String>, grantResults: IntArray) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)
        when (requestCode) {
            LOCATION_PERMISSION_REQUEST_CODE -> {
                if (grantResults.isNotEmpty() && grantResults[0] == PackageManager.PERMISSION_GRANTED) {
                    // Permission was granted. Continue with location-related functionality
                } else {
                    // Permission was denied. Provide an explanation to the user and guide them to enable it through settings
                    if (ActivityCompat.shouldShowRequestPermissionRationale(this, Manifest.permission.ACCESS_FINE_LOCATION)) {
                        showPermissionDeniedExplanation()
                    } else {
                        // User also checked "Don't ask again". Guide them to app settings.
                        guideUserToAppSettings()
                    }
                }
            }
        }
    }
    private fun showPermissionDeniedExplanation() {
        AlertDialog.Builder(this)
            .setMessage("This app requires location permissions to scan and associate tags with their locations. Please allow location access.")
            .setPositiveButton("OK") { dialog, which ->
                checkAndRequestLocationPermissions()
            }
            .setNegativeButton("Cancel", null)
            .create()
            .show()
    }
    private fun guideUserToAppSettings() {
        Toast.makeText(this, "Please enable location permissions in app settings", Toast.LENGTH_LONG).show()
        val intent = Intent(Settings.ACTION_APPLICATION_DETAILS_SETTINGS, Uri.parse("package:$packageName"))
        startActivity(intent)
    }
}