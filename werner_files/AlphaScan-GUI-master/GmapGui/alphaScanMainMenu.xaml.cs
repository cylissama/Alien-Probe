using System.Windows;

namespace GmapGui
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class alphaScanMainMenu : Window
    {
        private bool admin;
        public alphaScanMainMenu(bool adminMode)
        {
            InitializeComponent();

            admin = adminMode;
          if(admin == true)
            {
                mainMenu_MapManager.IsEnabled = true;
                mainMenu_Settings.IsEnabled = true;
            }
        }

        #region menu buttons
        private void mainMenu_DataAcquisition_Click(object sender, RoutedEventArgs e)
        {
            DataAquisition open = null;
            try
            {
                open = new DataAquisition(admin);
            }
            catch (System.Exception ex)
            { 
                System.Windows.Forms.MessageBox.Show("Exception occured: " + ex.Message.ToString());
            }
            open.Show();
            this.Close();
        }

        private void mainMenu_MapManager_Click(object sender, RoutedEventArgs e)
        {
            MainWindow open = null;
            try
            {
                 open = new MainWindow(admin);
            }
            catch (System.Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Exception occured: " + ex.Message.ToString());
            }
            try
            {
                open.Show();
                this.Close();
            }
            // did not open the map manager window, jsut goes back to main menu
            catch { }
        }

        private void mainMenu_Enforcement_Click(object sender, RoutedEventArgs e)
        {
            EnforcementWindow open = null;
            try
            {
                open = new EnforcementWindow(admin);
            }
            catch (System.Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Exception occured: " + ex.Message.ToString());
            }
            try
            {
                open.Show();
                this.Close();
            }
            // did not open the enforcement window, jsut goes back to main menu
            catch
            { }
            
        }

        private void mainMenu_Settings_Click(object sender, RoutedEventArgs e)
        {
            AlphaScanSettings open = null;
            try
            {
                open = new AlphaScanSettings(admin);
            }
            catch (System.Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Exception occured: " + ex.Message.ToString());
            }
            open.Show();
            this.Close();
        }

        private void mainMenu_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion
    }
}
