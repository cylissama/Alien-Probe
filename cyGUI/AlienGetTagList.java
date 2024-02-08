import com.alien.enterpriseRFID.reader.AlienClass1Reader;
import java.util.*;
import java.util.stream.Collectors;

public class AlienGetTagList {

    public String readerIP;
    public String readerPort;
    public String readerUserName;
    public String readerPassword;

    public static void main(String[] args) {
        List<String> tagList = GetTagList();
        for (String line : tagList) {
            System.out.println(line); // Printing each line, or process as needed
        }
    }

    public static List<String> GetTagList(){
        try {
            AlienClass1Reader reader = new AlienClass1Reader();
            reader.setConnection("161.6.218.87", 23); // Replace with your reader's IP address
            reader.setUsername("alien"); // Add your reader's username
            reader.setPassword("password"); // Add your reader's password

            reader.open();
            System.out.println("Connection established with RFID reader.");

            // Perform operations, like sending the "t" command
            //System.out.println(reader.doReaderCommand("t"));

            String commandOutput = reader.doReaderCommand("t");
            // Splitting the command output into an array of strings, each representing a line
            List<String> outputLines = Arrays.stream(commandOutput.split("\\r?\\n"))
                    .collect(Collectors.toList());
            // Processing each line in the output
//            for (String line : outputLines) {
//                System.out.println(line); // Printing each line, or process as needed
//            }

            reader.close();
            System.out.println("Connection closed.");

            return outputLines;

        } catch (Exception e) {
            e.printStackTrace();
        }
        return null;
    }
}
