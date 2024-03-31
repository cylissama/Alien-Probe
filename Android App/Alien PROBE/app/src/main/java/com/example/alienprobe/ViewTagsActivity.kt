package com.example.alienprobe

import android.content.Intent
import android.os.Bundle
import android.util.Log
import android.widget.Button
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView

class ViewTagsActivity : AppCompatActivity() {

    private lateinit var tagsRecyclerView: RecyclerView
    private lateinit var adapter: TagsAdapter
    private lateinit var dataBaseHelper: DataBaseHelper

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.view_tags)

        tagsRecyclerView = findViewById(R.id.tagsRecyclerView)
        tagsRecyclerView.layoutManager = LinearLayoutManager(this)

        dataBaseHelper = DataBaseHelper(this)
        val allTags = dataBaseHelper.getAllTags() // Implement this method to fetch tags

/*        Log.d("ViewTagsActivity", "Number of tags fetched: ${allTags.size}")
        allTags.forEach { tag ->
            Log.d("ViewTagsActivity", "Tag: ${tag.toString()}")
        }*/

        adapter = TagsAdapter(this, allTags)
        tagsRecyclerView.adapter = adapter

        val backButton = findViewById<Button>(R.id.backButtonTagView)
        backButton.setOnClickListener {
            val intent = Intent(this, ScannerActivity::class.java)
            startActivity(intent)
        }
    }
}

