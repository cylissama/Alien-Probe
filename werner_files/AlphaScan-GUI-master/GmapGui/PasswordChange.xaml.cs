using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace GmapGui
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class PasswordChange : Window
    {
        public PasswordChange()
        {
            InitializeComponent();
        }

        //This is just the functionality for changing the password which is saved in the Password file
        private void newPassBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (newPassBox.Password.Equals(ConfirmPass.Password))
                {
                Confirm.IsEnabled = true;
            }

            else
            {
                Confirm.IsEnabled = false;
            }
        }

        private void ConfirmPass_KeyDown(object sender, KeyEventArgs e)
        {
            if (ConfirmPass.Password.Equals(newPassBox.Password))
            {
                Confirm.IsEnabled = true;

            }
            else
            {
                Confirm.IsEnabled = false;
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            string programDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan");

            string PasswordFile = "Password";

            if (!Directory.Exists(System.IO.Path.Combine(programDirectory, "Admin Info")))
            {
                Directory.CreateDirectory(System.IO.Path.Combine(programDirectory, "Admin Info"));
            }
            PasswordFile = System.IO.Path.Combine(programDirectory, "Admin Info", PasswordFile);
            PasswordFile = PasswordFile + ".txt";

            StreamWriter streamWriter = new StreamWriter(PasswordFile);
            streamWriter.WriteLine(ConfirmPass.Password);
            streamWriter.Close();

            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


    }
}
