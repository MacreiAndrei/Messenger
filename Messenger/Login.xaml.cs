using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
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
    public partial class Login : Window
    {
        private static HttpClient client;
        private const string TOKEN_FILE_NAME = "session_token.dat";
        private string tokenFilePath;

        public Login()
        {
            InitializeComponent();

            // Initialize token file path
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = System.IO.Path.Combine(appDataPath, "Messenger");
            Directory.CreateDirectory(appFolder);
            tokenFilePath = System.IO.Path.Combine(appFolder, TOKEN_FILE_NAME);

            // Initialize HttpClient with SSL bypass
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            client = new HttpClient(clientHandler);

            // Add event handlers
            NameTextBox.TextChanged += NameTextBox_TextChanged;
            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
            LoginButton.Click += LoginButton_Click;

            // Allow Enter key to trigger login
            this.KeyDown += Login_KeyDown;

            // Try auto-login with saved token
            TryAutoLogin();
        }

        private async void TryAutoLogin()
        {
            try
            {
                string savedToken = LoadSessionToken();
                if (!string.IsNullOrEmpty(savedToken))
                {
                    // Show loading state
                    LoginButton.IsEnabled = false;
                    LoginButton.Content = "Проверка сохраненной сессии...";

                    User user = await AuthenticateWithToken(savedToken);
                    if (user != null)
                    {
                        // Token is valid, proceed to main window
                        MainWindow mainWindow = new MainWindow(savedToken, user.username);
                        mainWindow.Show();
                        this.Close();
                        return;
                    }
                    else
                    {
                        // Token is invalid, clear it
                        ClearSessionToken();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при автоматическом входе: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginButton.Content = "Login";
            }
        }

        private async Task<User> AuthenticateWithToken(string sessionToken)
        {
            try
            {
                // Проверяем токен перед использованием
                if (string.IsNullOrWhiteSpace(sessionToken))
                {
                    System.Diagnostics.Debug.WriteLine("AuthenticateWithToken: Session token is null or empty");
                    return null;
                }

                var tokenAuth = new TokenAuth { SessionToken = sessionToken };
                string jsonRequest = JsonSerializer.Serialize(tokenAuth);
                string jsonResponse = await PostRequestAsync(ApiConfig.Endpoints.TokenValidation, jsonRequest);

                // Логируем ответ от сервера для отладки
                System.Diagnostics.Debug.WriteLine($"Server response: {jsonResponse}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                ResponseLogin response = JsonSerializer.Deserialize<ResponseLogin>(jsonResponse, options);

                if (response?.status == "success" && response.data?.user != null)
                {
                    var user = response.data.user;

                    // Проверяем, что сервер вернул корректный токен
                    if (string.IsNullOrWhiteSpace(user.sessionToken))
                    {
                        System.Diagnostics.Debug.WriteLine("Server returned empty session token");
                        return null;
                    }

                    // Store session data globally
                    App.CurrentSessionToken = user.sessionToken;
                    App.CurrentUser = user;

                    // Update saved token if it changed
                    if (user.sessionToken != sessionToken)
                    {
                        SaveSessionToken(user.sessionToken);
                    }

                    return user;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AuthenticateWithToken: {ex.Message}");
                return null;
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

        private string LoadSessionToken()
        {
            try
            {
                if (!File.Exists(tokenFilePath))
                    return null;

                byte[] encryptedToken = File.ReadAllBytes(tokenFilePath);
                byte[] decryptedToken = ProtectedData.Unprotect(
                    encryptedToken,
                    null,
                    DataProtectionScope.CurrentUser);

                string token = Encoding.UTF8.GetString(decryptedToken);
                System.Diagnostics.Debug.WriteLine($"Loaded session token. Length: {token?.Length ?? 0}");
                return token;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading session token: {ex.Message}");
                // If decryption fails, clear the invalid token file
                ClearSessionToken();
                return null;
            }
        }

        private void ClearSessionToken()
        {
            try
            {
                if (File.Exists(tokenFilePath))
                    File.Delete(tokenFilePath);
                System.Diagnostics.Debug.WriteLine("Session token cleared");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing session token: {ex.Message}");
            }
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

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            Register registerWindow = new Register();
            registerWindow.Show();
            this.Close();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string enteredName = NameTextBox.Text.Trim();
            string enteredPassword = PasswordBox.Password;

            // Validate input
            if (string.IsNullOrWhiteSpace(enteredName))
            {
                MessageBox.Show("Te rog să introduci numele.", "Câmp gol",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(enteredPassword))
            {
                MessageBox.Show("Te rog să introduci parola.", "Câmp gol",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            // Disable login button during request
            LoginButton.IsEnabled = false;
            LoginButton.Content = "Se conectează...";

            
                UserAuth user = new UserAuth
                {
                    Username = enteredName,
                    Password = enteredPassword
                };

                string jsonRequest = JsonSerializer.Serialize(user);
                string jsonResponse = await PostRequestAsync(ApiConfig.Endpoints.Login, jsonRequest);

                // Логируем ответ от сервера
                System.Diagnostics.Debug.WriteLine($"Login response: {jsonResponse}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = false
                };

                ResponseLogin response = JsonSerializer.Deserialize<ResponseLogin>(jsonResponse, options);

                if (response == null)
                {
                    MessageBox.Show("Eroare la procesarea răspunsului de la server.", "Eroare",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (response.status == "success" && response.data?.user != null)
                {
                    var userData = response.data.user;

                    // Проверяем, что сервер вернул токен
                    if (string.IsNullOrWhiteSpace(userData.sessionToken))
                    {
                        MessageBox.Show("Сервер не вернул токен сессии.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                Debug.WriteLine(userData.sessionToken);
                    // Store session token globally for other windows
                    App.CurrentSessionToken = userData.sessionToken;
                    App.CurrentUser = userData;

                    // Save session token to device
                    SaveSessionToken(userData.sessionToken);

                    MessageBox.Show($"Login reușit! Bun venit, {enteredName}!", "Succes",
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

        }

        private void Login_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && LoginButton.IsEnabled)
            {
                LoginButton_Click(sender, e);
            }
        }

        private static async Task<string> PostRequestAsync(string url, string json)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(url, content);
            return await response.Content.ReadAsStringAsync();
        }

        // Method to logout and clear saved token (call this from other parts of your app)
        public static void Logout()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appFolder = System.IO.Path.Combine(appDataPath, "Messenger");
                string tokenFilePath = System.IO.Path.Combine(appFolder, TOKEN_FILE_NAME);

                if (File.Exists(tokenFilePath))
                    File.Delete(tokenFilePath);

                App.CurrentSessionToken = null;
                App.CurrentUser = null;
                System.Diagnostics.Debug.WriteLine("User logged out successfully");

                // Închide toate ferestrele și deschide Login
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Închide toate ferestrele principale (MainWindow)
                    foreach (Window window in Application.Current.Windows.OfType<Window>().ToList())
                    {
                        if (window.GetType() != typeof(Login))
                        {
                            window.Close();
                        }
                    }

                    // Deschide fereastra de Login
                    Login loginWindow = new Login();
                    loginWindow.Show();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during logout: {ex.Message}");
            }
        }

        // Data classes based on your API structure
        public class UserAuth
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        public class TokenAuth
        {
            public string SessionToken { get; set; }
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