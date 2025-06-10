using System;
using System.Collections.Generic;
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

namespace Messenger
{
    /// <summary>
    /// Interaction logic for Register.xaml
    /// </summary>
    public partial class Register : Window
    {
        public Register()
        {
            InitializeComponent();

            // Add event handlers
            NameTextBox.TextChanged += NameTextBox_TextChanged;
            EmailTextBox.TextChanged += EmailTextBox_TextChanged;
            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;

            // Add click handler for the Login button
            LoginButton.Click += LoginButton_Click;
        }

        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            NamePlaceholder.Visibility = string.IsNullOrWhiteSpace(NameTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            EmailPlaceholder.Visibility = string.IsNullOrWhiteSpace(EmailTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordPlaceholder.Visibility = string.IsNullOrWhiteSpace(PasswordBox.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Open the Login window
            Login loginWindow = new Login();
            loginWindow.Show();

            // Close the Register window
            this.Close();
        }
    }
}