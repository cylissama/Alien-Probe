import com.alien.enterpriseRFID.reader.AlienClass1Reader;
import com.alien.enterpriseRFID.reader.AlienReaderConnectionException;
import com.alien.enterpriseRFID.reader.AlienReaderNotValidException;
import com.alien.enterpriseRFID.reader.AlienReaderTimeoutException;

import java.util.*;
import java.util.stream.Collectors;

public class AlienGetTagList {

    public String readerIP;
    public String readerPort;
    public String readerUserName;
    public String readerPassword;
    public static AlienClass1Reader reader = new AlienClass1Reader();

    public static void main(String[] args) {
        reader.setConnection("161.6.218.87", 23); // Replace with your reader's IP address
        reader.setUsername("alien"); // Add your reader's username
        reader.setPassword("password"); // Add your reader's password
    }
    public void openReader(){
        try {
            reader.setConnection("161.6.218.87", 23); // Replace with your reader's IP address
            reader.setUsername("alien"); // Add your reader's username
            reader.setPassword("password");
            reader.open();
            System.out.println("Connection established with RFID reader.");
        } catch (AlienReaderNotValidException e) {
            throw new RuntimeException(e);
        } catch (AlienReaderTimeoutException e) {
            throw new RuntimeException(e);
        } catch (AlienReaderConnectionException e) {
            throw new RuntimeException(e);
        }
    }
    public void closeReader(){
        reader.close();
        System.out.println("Connection Closed.");

    }

    public static List<String> GetTagList(){
        try {
            String commandOutput = reader.doReaderCommand("t");
            List<String> outputLines = Arrays.stream(commandOutput.split("\\r?\\n"))
                    .collect(Collectors.toList());
            return outputLines;
        } catch (Exception e) {
            System.out.println(e);
            e.printStackTrace();
        }
        return null;
    }
}
