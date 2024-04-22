import javax.swing.*;
import java.awt.*;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.io.BufferedWriter;
import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.sql.PreparedStatement;
import java.sql.SQLException;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.List;
import java.sql.DriverManager;
import java.sql.Connection;

public class AlienGUI extends JFrame {
    DefaultListModel<String> listModel = new DefaultListModel<>();
    private Timer timer; // Timer to manage periodic updates
    private JLabel statusLabel; // Label to show the current status

    public AlienGUI() {

        //tagGetter.openReader();

        setTitle("Alien P.R.O.B.E"); //Precision RFID Operational Bridge for Efficiency
        setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
        setSize(400, 300); // Set the initial size

        getContentPane().setBackground(Color.LIGHT_GRAY);

        JTabbedPane tabbedPane = new JTabbedPane();

        JPanel settingsPanel = new JPanel();
        JPanel operationPanel = new JPanel(new BorderLayout());

        settingsPanel.setLayout(new GridBagLayout());
        GridBagConstraints gbc = new GridBagConstraints();
        gbc.gridx = 0;
        gbc.gridy = 0;
        gbc.fill = GridBagConstraints.HORIZONTAL;
        gbc.insets = new Insets(4,4,4,4);

        //textfields for the settings menu
        JLabel usernameLabel = new JLabel("Username");
        JTextField usernameTextField = new JTextField(20);

        JLabel passwordLabel = new JLabel("Password");
        JTextField passwordTextField = new JTextField(20);

        JLabel IPLabel = new JLabel("IP Address");
        JTextField IPTextField = new JTextField(20);

        JLabel portLabel = new JLabel("Port");
        JTextField portTextField = new JTextField(20);

        JButton submitBtn = new JButton("Submit");


        settingsPanel.add(usernameLabel, gbc);
        gbc.gridy++; // Move to the next row
        settingsPanel.add(usernameTextField, gbc);
        gbc.gridy++; // Move to the next row

        settingsPanel.add(passwordLabel, gbc);
        gbc.gridy++;
        settingsPanel.add(passwordTextField, gbc);
        gbc.gridy++;

        settingsPanel.add(IPLabel, gbc);
        gbc.gridy++;
        settingsPanel.add(IPTextField, gbc);
        gbc.gridy++;

        settingsPanel.add(portLabel, gbc);
        gbc.gridy++;
        settingsPanel.add(portTextField, gbc);
        gbc.gridy++;
        settingsPanel.add(submitBtn, gbc);
        gbc.gridy++;

        //READS
        JList<String> readsList = new JList<>(listModel);
        JScrollPane scrollPane = new JScrollPane(readsList);
        scrollPane.setPreferredSize(new Dimension(200, 150));
        scrollPane.setBorder(BorderFactory.createTitledBorder("Reads"));

        JButton startButton = new JButton("Start");
        JButton stopButton = new JButton("Stop");
        JButton clearButton = new JButton("Clear");
        JButton saveButton = new JButton("Save");
        JPanel buttonPanel = new JPanel();

        //add buttons to panel
        buttonPanel.add(startButton);
        buttonPanel.add(stopButton);
        buttonPanel.add(clearButton);
        buttonPanel.add(saveButton);

        saveButton.addActionListener(e -> saveReadsToUniqueFile());

        // Initialize the status label and set its initial text
        statusLabel = new JLabel("Standby");
        statusLabel.setHorizontalAlignment(JLabel.RIGHT);

        // Layout for the bottom panel
        JPanel bottomPanel = new JPanel(new BorderLayout());
        bottomPanel.add(buttonPanel, BorderLayout.WEST);
        bottomPanel.add(statusLabel, BorderLayout.EAST);

        operationPanel.add(scrollPane, BorderLayout.CENTER);
        operationPanel.add(bottomPanel, BorderLayout.SOUTH);

        tabbedPane.addTab("Operation", operationPanel);
        tabbedPane.addTab("Settings", settingsPanel);

        add(tabbedPane);

        // listeners

        submitBtn.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                AlienReader.readerUserName = usernameTextField.getText();
                AlienReader.readerPassword = passwordTextField.getText();
                AlienReader.readerIP = IPTextField.getText();
                AlienReader.readerPort = Integer.parseInt(portTextField.getText());
            }
        });

        timer = new Timer(2000, new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                // Add a new item to the list model every second
                List<String> tagList = AlienReader.GetTagList();
                if (tagList == null) {
                    listModel.addElement("No Scans");
                }
                for(String tag : tagList) {
                    listModel.addElement(tag);
                }
            }
        });
        startButton.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                if (!timer.isRunning()) {
                    timer.start();
                    statusLabel.setText("Working");
                }
            }
        });
        stopButton.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                if (timer.isRunning()) {
                    timer.stop();
                    statusLabel.setText("Standby");
                }
            }
        });
        clearButton.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                listModel.clear();
            }
        });

        setVisible(true);
    }

    private void saveReadsToUniqueFile() {
        String directoryPath = "collected_data"; // Folder where you want to save the files
        File directory = new File(directoryPath);
        if (!directory.exists()) {
            directory.mkdirs(); // Make the directory (including any necessary but nonexistent parent directories)
        }

        // Format the current date and time to use as a filename
        String timeStamp = new SimpleDateFormat("yyyyMMddHHmmss").format(new Date());
        String fileName = "readsData_" + timeStamp + ".rtf";

        try (BufferedWriter writer = new BufferedWriter(new FileWriter(new File(directoryPath, fileName)))) {
            StringBuilder data = new StringBuilder();
            for (int i = 0; i < listModel.size(); i++) {
                data.append(listModel.get(i));
                if (i < listModel.size() - 1) { // This checks if it's not the last element
                    data.append(","); // Add a comma after each element except the last one
                }
            }
            writer.write(String.valueOf(data));
            System.out.println("Reads saved to file: " + fileName);
        } catch (IOException ex) {
            System.out.println("Error saving reads to file: " + ex.getMessage());
        }
    }

    public static void main(String[] args) {
        new AlienGUI(); // Create and display the GUI
    }
}

