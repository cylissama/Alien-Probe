import javax.swing.*;
import java.awt.*;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;

public class AlienGUI extends JFrame {

    DefaultListModel<String> listModel = new DefaultListModel<>();
    private Timer timer; // Timer to manage periodic updates
    private JLabel statusLabel; // Label to show the current status

    public AlienGUI() {
        setTitle("Alien U.F.O");
        setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
        setSize(400, 300); // Set the initial size

//        ImageIcon logoIcon = new ImageIcon("/Users/cylis/Documents/IdeaProjects/Alien/src/aline png.png");
//        Image image = logoIcon.getImage(); // Convert ImageIcon to Image
//        Image newimg = image.getScaledInstance(150, 50,  Image.SCALE_SMOOTH); // Scale it to the new size
//        ImageIcon scaledIcon = new ImageIcon(newimg);
//
//        JLabel logoLabel = new JLabel(scaledIcon);
//        JPanel topPanel = new JPanel(new FlowLayout(FlowLayout.LEFT));
//        topPanel.add(logoLabel);

        getContentPane().setBackground(Color.LIGHT_GRAY);

        JTabbedPane tabbedPane = new JTabbedPane();

        JPanel settingsPanel = new JPanel();
        JPanel operationPanel = new JPanel(new BorderLayout());

        //READS
        JList<String> readsList = new JList<>(listModel);
        JScrollPane scrollPane = new JScrollPane(readsList);
        scrollPane.setPreferredSize(new Dimension(200, 150));
        scrollPane.setBorder(BorderFactory.createTitledBorder("Reads"));

        JButton startButton = new JButton("Start");
        JButton stopButton = new JButton("Stop");
        JButton clearButton = new JButton("Clear");

        JPanel buttonPanel = new JPanel();
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

//        add(topPanel, BorderLayout.NORTH);
        add(tabbedPane);

        // Define the timer but don't start it yet
        timer = new Timer(1000, new ActionListener() { // Timer set to fire every 1000 milliseconds (1 second)
            @Override
            public void actionPerformed(ActionEvent e) {
                // Add a new item to the list model every second
                listModel.addElement("New Read " + (listModel.size() + 1));
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

    public void updateReadsList(String[] tagList) {
        SwingUtilities.invokeLater(() -> {
            listModel.clear(); // Optional: Clear existing entries
            for(String tag : tagList) {
                listModel.addElement(tag);
            }
        });
    }


    public static void main(String[] args) {
        new AlienGUI(); // Create and display the GUI
    }
}
