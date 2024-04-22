import com.alien.enterpriseRFID.reader.AlienClass1Reader;
import com.alien.enterpriseRFID.reader.AlienReaderConnectionException;
import com.alien.enterpriseRFID.reader.AlienReaderNotValidException;
import com.alien.enterpriseRFID.reader.AlienReaderTimeoutException;

import java.util.*;
import java.util.stream.Collectors;

public class AlienReader {

    public static String readerIP;
    public static int readerPort;
    public static String readerUserName;
    public static String readerPassword;
    public static AlienClass1Reader reader = new AlienClass1Reader();

    public static void main(String[] args) {

    }

    public static void openReader(){
        try {
            reader.setConnection(readerIP, readerPort); // Replace with your reader's IP address
            reader.setUsername(readerUserName); // Add your reader's username
            reader.setPassword(readerPassword);
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
    public static void closeReader(){
        try {
            reader.close();
            System.out.println("Connection Closed.");
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }

    public static List<String> GetTagList(){
        System.out.println(readerIP + " " + readerPort + " " + readerUserName + " " + readerPassword);
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

    public String getReaderIP() {
        return readerIP;
    }

    public void setReaderIP(String readerIP) {
        this.readerIP = readerIP;
    }

    public int getReaderPort() {
        return readerPort;
    }

    public void setReaderPort(int readerPort) {
        this.readerPort = readerPort;
    }

    public String getReaderUserName() {
        return readerUserName;
    }

    public void setReaderUserName(String readerUserName) {
        this.readerUserName = readerUserName;
    }

    public String getReaderPassword() {
        return readerPassword;
    }

    public void setReaderPassword(String readerPassword) {
        this.readerPassword = readerPassword;
    }
}
