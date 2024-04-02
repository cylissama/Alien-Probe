package com.example.alienprobe

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.database.sqlite.SQLiteConstraintException
import android.icu.text.SimpleDateFormat
import android.icu.util.Calendar
import android.location.Location
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
import com.google.android.gms.location.FusedLocationProviderClient
import com.google.android.gms.location.LocationServices
import java.util.Locale

//import RFIDTag class
var tagList: MutableList<RFIDTag> = mutableListOf()

class ScannerActivity : AppCompatActivity() {

    companion object {
        private const val LOCATION_PERMISSION_REQUEST_CODE = 1
    }

    private lateinit var fusedLocationClient: FusedLocationProviderClient
    private var lastLocation: Location? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.scanner)

        fusedLocationClient = LocationServices.getFusedLocationProviderClient(this)

        checkAndRequestLocationPermissions()

        setupUI()

    }
    private fun setupUI() {
        //get latest gps values
        getLastLocation()
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

            getLastLocation()
            val currentTime = Calendar.getInstance()
            val dateFormat = SimpleDateFormat("yyyy-MM-dd HH:mm:ss", Locale.getDefault())
            val formattedTime = dateFormat.format(currentTime.time)

            val mediaPlayer = MediaPlayer.create(this, R.raw.alien_blaster)
            mediaPlayer.start()
            mediaPlayer.setOnCompletionListener {
                it.release()
            }

            val dataBaseHelper: DataBaseHelper = DataBaseHelper(this)

            val tempTagList: MutableList<RFIDTag> = reader.GetTagList()

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
                        text = "EPC: ${tag.getEPC()}" // Accessing EPC data from RFIDTag object
                    }
                    linearLayout.addView(textView)

                    //Add Tag data to Database
                    try {
                        val long: Double = lastLocation!!.longitude
                        val lat: Double = lastLocation!!.latitude
                        val time: String = formattedTime
                        val tagModel: TagModel = TagModel(-1, "EPC: ${tag.getEPC()}", long, lat, time)
                        val success = dataBaseHelper.addOne(tagModel)
                        if (success) {
                            Toast.makeText(this, "Worked", Toast.LENGTH_LONG).show()
                        }
                    } catch (e: SQLiteConstraintException) {
                        // Handle the duplicate entry case, maybe log it or inform the user
                        Log.d("Insertion", "Duplicate EPC: ${tag.getEPC()} not added.")
                    } catch (e: Exception) {
                        // Handle other exceptions
                        val textView = TextView(this).apply {
                            text = "Error adding tag: ${e.message}"
                        }
                        linearLayout.addView(textView)
                        Log.d("Insertion", "ERROR: ${tag.getEPC()} not added.")

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
    private fun getLastLocation() {
        if (ActivityCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION) != PackageManager.PERMISSION_GRANTED) {
            // Permission check failed. Exit the method.
            return
        }
        fusedLocationClient.lastLocation.addOnSuccessListener { location: Location? ->
            // Got last known location. In some rare situations, this can be null.
            if (location != null) {
                lastLocation = location // Update the lastLocation variable with the new location
                // Optionally, use the location data immediately for some task
                // For example, updating the UI or logging
                val latitude = location.latitude
                val longitude = location.longitude
                Log.d("LocationUpdate", "New location received: Lat $latitude, Lon $longitude")
            } else {
                // Handle the case where location is null
                Log.d("LocationUpdate", "No location received")
            }
        }
    }
    private fun showLocationToast() {
        val locationMessage = if (lastLocation != null) {
            "Latitude: ${lastLocation!!.latitude}, Longitude: ${lastLocation!!.longitude}"
        } else {
            "Location not available"
        }

        Toast.makeText(this, locationMessage, Toast.LENGTH_LONG).show()
    }
}