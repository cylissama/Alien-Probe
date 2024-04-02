package com.example.alienprobe;

import static androidx.core.content.ContextCompat.startActivity;

import android.content.Context;
import android.content.Intent;
import android.net.Uri;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.TextView;
import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import java.util.List;

public class TagsAdapter extends RecyclerView.Adapter<TagsAdapter.ViewHolder> {
    private List<TagModel> tagsList;
    private LayoutInflater inflater;
    private Context context; // Added to use for launching an Intent

    public TagsAdapter(Context context, List<TagModel> tagsList) {
        this.context = context; // Initialize context
        this.inflater = LayoutInflater.from(context);
        this.tagsList = tagsList;
    }

    @NonNull
    @Override
    public ViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View view = inflater.inflate(R.layout.tag_item, parent, false);
        return new ViewHolder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull ViewHolder holder, int position) {
        TagModel tag = tagsList.get(position);
        String text = "EPC: " + tag.getEPC() + "\nLat: " + tag.getLatitude() + " Long: " + tag.getLongitude() + "\nTime: " + tag.getTime();
        holder.epcTextView.setText(text);

        // Set the click listener for the mapButton instead of epcTextView
        holder.mapButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                // Open Google Maps with the specified coordinates
                Uri gmmIntentUri = Uri.parse("geo:" + tag.getLatitude() + "," + tag.getLongitude() + "?z=10");
                Intent mapIntent = new Intent(Intent.ACTION_VIEW, gmmIntentUri);
                mapIntent.setPackage("com.google.android.apps.maps");
                if (mapIntent.resolveActivity(context.getPackageManager()) != null) {
                    context.startActivity(mapIntent);
                } else {
                    // If Google Maps is not installed, open the location in a web browser
                    Intent browserIntent = new Intent(Intent.ACTION_VIEW, Uri.parse("https://maps.google.com/?q=" + tag.getLatitude() + "," + tag.getLongitude()));
                    context.startActivity(browserIntent);
                }
            }
        });
    }

    @Override
    public int getItemCount() {
        return tagsList.size();
    }
    public static class ViewHolder extends RecyclerView.ViewHolder {
        TextView epcTextView;
        Button mapButton; // Reference to the map button
        ViewHolder(View itemView) {
            super(itemView);
            epcTextView = itemView.findViewById(R.id.epcTextView);
            mapButton = itemView.findViewById(R.id.mapButton); // Initialize the button
        }
    }
    public interface TagInteractionListener {
        void onDeleteTag(String tagId);
    }
}
