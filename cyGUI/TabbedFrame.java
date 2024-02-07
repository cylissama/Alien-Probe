import javax.swing.*;
import java.awt.*;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;

public class TabbedFrame extends JFrame {

    // Define the list model as an instance variable so it can be accessed by action listeners
    DefaultListModel<String> listModel = new DefaultListModel<>();

    public TabbedFrame() {
        setTitle("Alien U.F.O");
        setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
        setSize(400, 300); // Set the initial size

        JTabbedPane tabbedPane = new JTabbedPane();

        JPanel settingsPanel = new JPanel();
        JPanel operationPanel = new JPanel(new BorderLayout()); // Use BorderLayout for better layout management

        // Initialize the JList with the list model
        JList<String> readsList = new JList<>(listModel);
        JScrollPane scrollPane = new JScrollPane(readsList);
        scrollPane.setPreferredSize(new Dimension(200, 150));
        scrollPane.setBorder(BorderFactory.createTitledBorder("Reads"));

        // Buttons
        JButton startButton = new JButton("Start");
        JButton stopButton = new JButton("Stop");
        JButton clearButton = new JButton("Clear");

        // Button Panel
        JPanel buttonPanel = new JPanel();
        buttonPanel.add(startButton);
        buttonPanel.add(stopButton);
        buttonPanel.add(clearButton);

        // Adding components to the operation panel
        operationPanel.add(scrollPane, BorderLayout.CENTER); // Put the scrollPane in the center
        operationPanel.add(buttonPanel, BorderLayout.SOUTH); // Put the buttons at the bottom

        // Add the panels to the tabbed pane
        tabbedPane.addTab("Operation", operationPanel);
        tabbedPane.addTab("Settings", settingsPanel);

        // Add the tabbed pane to the JFrame
        add(tabbedPane);

        // Action listeners for buttons
        startButton.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                // Start updating the list (This is just a placeholder. Implement your data fetching logic here)
                listModel.addElement("New Read " + (listModel.size() + 1));
            }
        });

        stopButton.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                // Stop updating the list (Implement your stopping logic here)
                // This might involve stopping a thread or timer that updates the list
            }
        });

        clearButton.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                // Clear the list
                listModel.clear();
            }
        });

        setVisible(true);
    }

    public static void main(String[] args) {
        new TabbedFrame(); // Create and display the GUI
    }
}
