package com.example.alienprobe.presentation;

import android.app.Dialog;
import android.content.Context;
import android.content.Intent;
import android.graphics.Color;
import android.graphics.drawable.ColorDrawable;
import android.net.Uri;
import android.view.Gravity;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.cardview.widget.CardView;
import androidx.recyclerview.widget.RecyclerView;

import com.example.alienprobe.R;
import com.example.alienprobe.database.DataBaseHelper;
import com.example.alienprobe.database.TagModel;

import java.util.List;
import java.util.Objects;

public class TagsAdapter extends RecyclerView.Adapter<TagsAdapter.ViewHolder> {
    private final List<TagModel> tagsList;
    private final LayoutInflater inflater;
    private final Context context; // Added to use for launching an Intent
    private final DataBaseHelper dbHelper;

    public TagsAdapter(Context context, List<TagModel> tagsList, DataBaseHelper dbHelper) {
        this.context = context;
        this.inflater = LayoutInflater.from(context);
        this.tagsList = tagsList;
        this.dbHelper = dbHelper;
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
        String text = "EPC: " + tag.getEPC();

        holder.epcTextView.setText(text);
        holder.tagContainer.setOnClickListener(v -> showDialog(tag));
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
                    Intent browserIntent = new Intent(Intent.ACTION_VIEW, Uri.parse("https://maps.google.com/?q=" + tag.getLongitude() + "," + tag.getLatitude()));
                    context.startActivity(browserIntent);
                }
            }
        });
    }

    private void showDialog(TagModel tag) {

        final Dialog dialog = new Dialog(inflater.getContext());
        dialog.requestWindowFeature(Window.FEATURE_NO_TITLE);
        dialog.setContentView(R.layout.bottom_sheet_layout);


        Button delete = dialog.findViewById(R.id.deleteButton);

        TextView epc = dialog.findViewById(R.id.epcView);

        String textToSet = "EPC: " + tag.getEPC() + "\nLat: " + tag.getLatitude() + " Long: " + tag.getLongitude() + "\nTime: " + tag.getTime();
        epc.setText(textToSet);

        delete.setOnClickListener(v -> {
            // Delete the brew from the database
            if (dbHelper.deleteTag(String.valueOf(tag.getId()))) {
                // Remove brew from the list and notify adapter
                int position = tagsList.indexOf(tag);
                tagsList.remove(position);
                notifyItemRemoved(position);
                Toast.makeText(inflater.getContext(), "Tag deleted successfully", Toast.LENGTH_SHORT).show();
            } else {
                Toast.makeText(inflater.getContext(), "Failed to delete tag", Toast.LENGTH_SHORT).show();
            }
            dialog.dismiss();
        });
        dialog.show();
        Objects.requireNonNull(dialog.getWindow()).setLayout(ViewGroup.LayoutParams.MATCH_PARENT,ViewGroup.LayoutParams.WRAP_CONTENT);
        dialog.getWindow().setBackgroundDrawable(new ColorDrawable(Color.TRANSPARENT));
        dialog.getWindow().setGravity(Gravity.CENTER);
    }
    @Override
    public int getItemCount() {
        return tagsList.size();
    }
    public static class ViewHolder extends RecyclerView.ViewHolder {
        TextView epcTextView;
        Button mapButton;
        CardView tagContainer;
        ViewHolder(View itemView) {
            super(itemView);
            epcTextView = itemView.findViewById(R.id.tagView);
            mapButton = itemView.findViewById(R.id.mapButton);
            tagContainer = itemView.findViewById(R.id.tagContainer);
        }
    }
}
