using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
namespace GmapGui
{
   
    public partial class UserLoginScreen : Window
    {
        public UserLoginScreen()
        {

        }

        private void btn_Login_Click(object sender, RoutedEventArgs e)
        {
            login();
        }

        private void passwordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                login();
        }

        private void login()
        {
            string programDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan");
            string passCheck;
            string PasswordFile = "Password";
            PasswordFile = System.IO.Path.Combine(programDirectory, "Admin Info", PasswordFile);
            PasswordFile = PasswordFile + ".txt";
            if (!File.Exists(PasswordFile))
            {
                passCheck = "admin";
            }
            else {
                StreamReader streamReader = new StreamReader(PasswordFile);
                passCheck = streamReader.ReadLine();
                streamReader.Close();
            }

            if (passwordBox.Password == passCheck)
            {
                alphaScanMainMenu open = new alphaScanMainMenu(true);
                open.Show();
                this.Close();
            }
            else if (chb_NoAdmin.IsChecked == true)
            {
                alphaScanMainMenu open = new alphaScanMainMenu(false);
                open.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Incorrect Password, to log in without admin privileges please click 'Continue without Password'.");
            }
        }

        private void btn_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
