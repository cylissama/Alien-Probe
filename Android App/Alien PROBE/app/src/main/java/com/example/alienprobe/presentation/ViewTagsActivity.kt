package com.example.alienprobe.presentation

import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.widget.Button
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.example.alienprobe.database.DataBaseHelper
import com.example.alienprobe.R

class ViewTagsActivity : AppCompatActivity() {

    private lateinit var tagsRecyclerView: RecyclerView
    private lateinit var adapter: TagsAdapter
    private lateinit var dataBaseHelper: DataBaseHelper

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.view_tags)

        setupRecycler()

        setDataBaseHelper()

        setupAdapter()

        setupListeners()
    }
    private fun setupRecycler() {
        tagsRecyclerView = findViewById(R.id.tagsRecyclerView)
        tagsRecyclerView.layoutManager = LinearLayoutManager(this)

    }
    private fun setDataBaseHelper() {
        dataBaseHelper = DataBaseHelper(this)
    }
    private fun setupAdapter() {
        val allTags = dataBaseHelper.allTags
        adapter = TagsAdapter(
            this,
            allTags,
            dataBaseHelper
        )
        tagsRecyclerView.adapter = adapter
    }
    private fun setupListeners() {
        val backButton = findViewById<Button>(R.id.backButtonTagView)
        backButton.setOnClickListener {
            val intent = Intent(this, ScannerActivity::class.java)
            startActivity(intent)
        }
    }
    fun openGoogleMaps(latitude: Double, longitude: Double) {
        // Create a Uri from an intent string. Use the result to create an Intent.
        val gmmIntentUri = Uri.parse("geo:$latitude,$longitude?q=$latitude,$longitude")

        // Create an Intent from gmmIntentUri. Set the action to ACTION_VIEW
        val mapIntent = Intent(Intent.ACTION_VIEW, gmmIntentUri)

        // Make the Intent explicit by setting the Google Maps package
        mapIntent.setPackage("com.google.android.apps.maps")

        // Attempt to start an activity that can handle the Intent
        if (mapIntent.resolveActivity(packageManager) != null) {
            startActivity(mapIntent)
        } else {
            // If Google Maps is not installed, open the location in a web browser
            val browserIntent = Intent(Intent.ACTION_VIEW, Uri.parse("https://maps.google.com/?q=$latitude,$longitude"))
            startActivity(browserIntent)
        }
    }
}

