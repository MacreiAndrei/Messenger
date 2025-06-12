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
        private const string TOKEN_FILE_NAME = "session_token.dat";
        private string tokenFilePath;

        public Register()
        {
            InitializeComponent();
            InitializeHttpClient();
            NameTextBox.TextChanged += NameTextBox_TextChanged;
            EmailTextBox.TextChanged += EmailTextBox_TextChanged;
            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;

            // Add click handler for the Login button
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = System.IO.Path.Combine(appDataPath, "Messenger");
            Directory.CreateDirectory(appFolder);
            tokenFilePath = System.IO.Path.Combine(appFolder, TOKEN_FILE_NAME);
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

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            Registration();
        }
        private async void Registration()
        {
            if (!string.IsNullOrWhiteSpace(NameTextBox.Text) && !string.IsNullOrWhiteSpace(EmailTextBox.Text) && !string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                try
                {
                    var requestData = new
                    {
                        Username = NameTextBox.Text,
                        Password = PasswordBox.Password,
                        Email = EmailTextBox.Text
                    };

                    var json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var jsonResponse = await httpClient.PostAsync("Autentification/register", content);
                    var responseContent = await jsonResponse.Content.ReadAsStringAsync();

                    ResponseLogin response = JsonConvert.DeserializeObject<ResponseLogin>(responseContent);


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
        }
        private void SaveSessionToken(string token)
        {
            try
            {
                // Проверяем токен перед сохранением
                if (string.IsNullOrWhiteSpace(token))
                {
                    System.Diagnostics.Debug.WriteLine("SaveSessionToken: Attempted to save null or empty token");
                    throw new ArgumentException("Токен сессии не может быть пустым", nameof(token));
                }

                // Encrypt token before saving (optional but recommended)
                byte[] encryptedToken = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(token),
                    null,
                    DataProtectionScope.CurrentUser);

                File.WriteAllBytes(tokenFilePath, encryptedToken);
                System.Diagnostics.Debug.WriteLine($"Session token saved successfully. Length: {token.Length}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving session token: {ex.Message}");
                MessageBox.Show($"Не удалось сохранить токен сессии: {ex.Message}", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        public class ResponseLogin
        {
            public string status { get; set; }
            public ResponseData data { get; set; }
            public string error { get; set; }
        }

        // New wrapper class for the data
        public class ResponseData
        {
            public User user { get; set; }
        }

        // Your existing User class remains the same
        public class User
        {
            public int userID { get; set; }
            public string username { get; set; }
            public string password { get; set; }
            public string email { get; set; }
            public bool isOnline { get; set; }
            public bool isAccountDeleted { get; set; }
            public DateTime lastTimeOnline { get; set; }
            public string sessionToken { get; set; }
            public DateTime sessionTokenExpirationDate { get; set; }
            public string userProfilePicturePath { get; set; }
        }
    }
}