import java.util.*;

public class Main {
    public static void main(String[] args) {
        AlienGUI tabbedFrame = new AlienGUI(); // Instantiate the tabbed frame
        List<String> tagList = AlienGetTagList.GetTagList(); // Assume this now returns String[]
        // Convert array to list, assuming GetTagList() is modified to return List<String>
//        List<String> tags = Arrays.asList(tagList);
        tabbedFrame.updateReadsList(tagList); // Update the reads list in the UI
    }

}
