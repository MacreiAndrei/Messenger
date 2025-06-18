using Messenger.Dtos;
using Messenger.Dtos.ResponseDto;
using Messenger.Models;
using Messenger.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
        private HttpClient httpClient;
        public static SessionManager _sessionManager;

        public Register()
        {
            InitializeComponent();
            InitializeHttpClient();
            _sessionManager = new SessionManager();
            NameTextBox.TextChanged += NameTextBox_TextChanged;
            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
        }
        private void InitializeHttpClient()
        {
            var clientHandler = new HttpClientHandler();

            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
            {
                return true;
            };

            httpClient = new HttpClient(clientHandler);
            httpClient.BaseAddress = new Uri(ApiConfig.BASE_URL);
        }
        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            NamePlaceholder.Visibility = string.IsNullOrWhiteSpace(NameTextBox.Text)
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

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            Registration();
        }
        private async void Registration()
        {
            if (!string.IsNullOrWhiteSpace(NameTextBox.Text) && !string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                try
                {
                    var requestData = new
                    {
                        Username = NameTextBox.Text,
                        Password = PasswordBox.Password,
                    };

                    var json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var jsonResponse = await httpClient.PostAsync("Autentification/register", content);
                    var responseContent = await jsonResponse.Content.ReadAsStringAsync();

                    ApiResponse<UserResponseDto> response = JsonConvert.DeserializeObject<ApiResponse<UserResponseDto>>(responseContent);


                    if (response == null)
                    {
                        MessageBox.Show("Eroare la procesarea răspunsului de la server.", "Eroare",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (response.status == "success" && response.data?.user != null)
                    {
                        var userData = response.data.user;

                        if (string.IsNullOrWhiteSpace(userData.sessionToken))
                        {
                            MessageBox.Show("Сервер не вернул токен сессии.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        App.CurrentSessionToken = userData.sessionToken;
                        App.CurrentUser = userData;

                        SaveSessionToken(userData.sessionToken);

                        MessageBox.Show($"Login reușit! Bun venit, {NameTextBox.Text}!", "Succes",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        // Open MainWindow
                        MainWindow mainWindow = new MainWindow(userData.sessionToken, userData.username);
                        mainWindow.Show();
                        this.Close();
                    }
                    else if (response.status == "error")
                    {
                        MessageBox.Show($"Eroare la autentificare: {response.error}", "Eroare",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("Răspuns necunoscut de la server.", "Eroare",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Eroare neașteptată: {ex.Message}", "Eroare",
                   MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Debug.WriteLine($"Unexpected error in LoginButton_Click: {ex}");
                }
            }
            else
            {
                MessageBox.Show("Username and password must not be empty.");
            }
        }
        private async Task SaveSessionToken(string token)
        {
            _sessionManager.SaveSessionToken(token);
        }
    }
}