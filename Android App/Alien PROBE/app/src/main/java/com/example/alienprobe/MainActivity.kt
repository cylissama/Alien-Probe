package com.example.alienprobe

import android.os.Bundle
import androidx.activity.ComponentActivity
import android.content.Intent
import android.widget.Button

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main) // Inflating activity_main.xml layout

        val buttonClick = findViewById<Button>(R.id.btnViewScanner)
        buttonClick.setOnClickListener {
            val intent = Intent(this, ScannerActivity::class.java)
            startActivity(intent)
        }
    }


}
