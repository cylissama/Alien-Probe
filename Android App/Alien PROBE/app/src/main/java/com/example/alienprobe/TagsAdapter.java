package com.example.alienprobe;

import android.content.Context;
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
    public TagsAdapter(Context context, List<TagModel> tagsList) {
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
        String text = "EPC: " + tag.getEPC() +  "\nLat: " + tag.getLatitude() +" Long: " + tag.getLongitude() + "\nTime: " + tag.getTime();
        holder.epcTextView.setText(text);
    }
    @Override
    public int getItemCount() {
        return tagsList.size();
    }
    public static class ViewHolder extends RecyclerView.ViewHolder {
        TextView epcTextView;

        ViewHolder(View itemView) {
            super(itemView);
            epcTextView = itemView.findViewById(R.id.epcTextView);
        }
    }
    public interface TagInteractionListener {
        void onDeleteTag(String tagId);
    }

}
