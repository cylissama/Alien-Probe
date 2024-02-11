import javax.swing.*;
import java.awt.*;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.List;

public class AlienGUI extends JFrame {

    AlienGetTagList tagGetter = new AlienGetTagList();
    DefaultListModel<String> listModel = new DefaultListModel<>();
    private Timer timer; // Timer to manage periodic updates
    private JLabel statusLabel; // Label to show the current status

    public AlienGUI() {
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
//        settingsPanel.add(usernameLabel);
//        settingsPanel.add(usernameTextField);
//        settingsPanel.add(passwordLabel);
//        settingsPanel.add(passwordTextField);
//        settingsPanel.add(IPLabel);
//        settingsPanel.add(IPTextField);
//        settingsPanel.add(portLabel);
//        settingsPanel.add(portTextField);


//        textField.addActionListener(new ActionListener() {
//            @Override
//            public void actionPerformed(ActionEvent e) {
//                myStringVar = textField.getText(); // Update string variable on text input
//                System.out.println("Updated string variable to: " + myStringVar);
//            }
//        });
        //READS
        JList<String> readsList = new JList<>(listModel);
        JScrollPane scrollPane = new JScrollPane(readsList);
        scrollPane.setPreferredSize(new Dimension(200, 150));
        scrollPane.setBorder(BorderFactory.createTitledBorder("Reads"));

        JButton startButton = new JButton("Start");
        JButton stopButton = new JButton("Stop");
        JButton clearButton = new JButton("Clear");
        JPanel buttonPanel = new JPanel();

        //add buttons to panel
        buttonPanel.add(startButton);
        buttonPanel.add(stopButton);
        buttonPanel.add(clearButton);

        // Initialize the status label and set its initial text
        statusLabel = new JLabel("Standby");
        statusLabel.setHorizontalAlignment(JLabel.RIGHT); // Align the text to the right

        // Layout for the bottom panel
        JPanel bottomPanel = new JPanel(new BorderLayout());
        bottomPanel.add(buttonPanel, BorderLayout.WEST); // Add button panel to the left
        bottomPanel.add(statusLabel, BorderLayout.EAST); // Add status label to the right

        operationPanel.add(scrollPane, BorderLayout.CENTER);
        operationPanel.add(bottomPanel, BorderLayout.SOUTH); // Use the bottom panel here

        tabbedPane.addTab("Operation", operationPanel);
        tabbedPane.addTab("Settings", settingsPanel);

//      //add(topPanel, BorderLayout.NORTH);
        add(tabbedPane);

        // Define the timer but don't start it yet
        timer = new Timer(5000, new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                // Add a new item to the list model every second
                List<String> tagList = tagGetter.GetTagList();
                listModel.addElement("\n--------------------------------\n");
                for(String tag : tagList) {
                    System.out.println(tag);
                    listModel.addElement(tag);
                }
            }
        });

        // Start button starts the timer and updates the status
        startButton.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                if (!timer.isRunning()) {
                    timer.start();
                    statusLabel.setText("In Operation");
                }
            }
        });

        // Stop button stops the timer and updates the status
        stopButton.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                if (timer.isRunning()) {
                    timer.stop();
                    statusLabel.setText("Standby");
                }
            }
        });

        // Clear button clears the list
        clearButton.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                listModel.clear();
            }
        });

        setVisible(true);
    }

    public void updateReadsList(List<String> tagList) {
        SwingUtilities.invokeLater(() -> {
            listModel.clear(); // Optional: Clear existing entries
            for(String tag : tagList) {
                System.out.println(tag);
                listModel.addElement(tag);
            }
        });
    }

    public static void main(String[] args) {
        new AlienGUI(); // Create and display the GUI
    }
}
