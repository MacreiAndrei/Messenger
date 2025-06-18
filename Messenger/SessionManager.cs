using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using static Messenger.Login;
using System.Windows;

namespace Messenger.Models
{
    public class SessionManager
    {
        private const string TOKEN_FILE_NAME = "session_token.dat";
        private const string USER_DATA_FILE_NAME = "user_data.dat";
        private static readonly string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static readonly string appFolder = Path.Combine(appDataPath, "Messenger");
        private static readonly string tokenFilePath = Path.Combine(appFolder, TOKEN_FILE_NAME);
        private static readonly string userDataFilePath = Path.Combine(appFolder, USER_DATA_FILE_NAME);

        static SessionManager()
        {
            Directory.CreateDirectory(appFolder);
        }

        /// <summary>
        /// Сохраняет сессию пользователя на устройство
        /// </summary>
        /// <param name="user">Данные пользователя</param>
        public void SaveSession(User user)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException(nameof(user), "Данные пользователя не могут быть null");
                }

                if (string.IsNullOrWhiteSpace(user.sessionToken))
                {
                    throw new ArgumentException("Токен сессии не может быть пустым", nameof(user.sessionToken));
                }

                SaveSessionToken(user.sessionToken);

                string userJson = JsonSerializer.Serialize(user);
                byte[] encryptedUserData = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(userJson),
                    null,
                    DataProtectionScope.CurrentUser);

                File.WriteAllBytes(userDataFilePath, encryptedUserData);
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось сохранить сессию: {ex.Message}");
            }
        }

        /// <summary>
        /// Загружает сохраненную сессию
        /// </summary>
        /// <returns>Данные пользователя или null, если сессия не найдена</returns>
        public static User LoadSession()
        {
            try
            {
                if (!File.Exists(userDataFilePath) || !File.Exists(tokenFilePath))
                    return null;

                byte[] encryptedUserData = File.ReadAllBytes(userDataFilePath);
                byte[] decryptedUserData = ProtectedData.Unprotect(
                    encryptedUserData,
                    null,
                    DataProtectionScope.CurrentUser);

                string userJson = Encoding.UTF8.GetString(decryptedUserData);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<User>(userJson, options);
            }
            catch
            {
                // Если расшифровка не удалась, очищаем неверные данные
                ClearSession();
                return null;
            }
        }

        public void SaveSessionToken(string token)
        {
            try
            {
                // Проверяем, что токен не null и не пустой
                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new ArgumentException("Токен сессии не может быть пустым или null", nameof(token));
                }

                byte[] encryptedToken = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(token),
                    null,
                    DataProtectionScope.CurrentUser);

                File.WriteAllBytes(tokenFilePath, encryptedToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось сохранить токен: {ex.Message}");
            }
        }

        /// <summary>
        /// Загружает токен сессии
        /// </summary>
        /// <returns>Токен сессии или null</returns>
        public static string LoadSessionToken()
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

                return Encoding.UTF8.GetString(decryptedToken);
            }
            catch
            {
                ClearSession();
                return null;
            }
        }

        /// <summary>
        /// Проверяет валидность токена сессии на сервере
        /// </summary>
        /// <param name="token">Токен для проверки</param>
        /// <returns>Данные пользователя, если токен валиден</returns>
        public static async Task<User> ValidateSessionTokenAsync(string token)
        {
            try
            {
                // Проверяем токен перед отправкой
                if (string.IsNullOrWhiteSpace(token))
                {
                    return null;
                }

                using var client = CreateHttpClient();

                var tokenAuth = new { SessionToken = token };
                string jsonRequest = JsonSerializer.Serialize(tokenAuth);

                using var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(ApiConfig.Endpoints.TokenValidation, content);

                if (!response.IsSuccessStatusCode)
                    return null;

                string jsonResponse = await response.Content.ReadAsStringAsync();

                // Добавляем логирование для отладки
                System.Diagnostics.Debug.WriteLine($"Server response: {jsonResponse}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var responseObj = JsonSerializer.Deserialize<ResponseLogin>(jsonResponse, options);

                if (responseObj?.status == "success" && responseObj.data?.user != null)
                {
                    var user = responseObj.data.user;

                    // Проверяем, что токен в ответе не пустой
                    if (string.IsNullOrWhiteSpace(user.sessionToken))
                    {
                        System.Diagnostics.Debug.WriteLine("Warning: Server returned empty session token");
                        return null;
                    }

                    return user;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating token: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Очищает сохраненную сессию
        /// </summary>
        public static void ClearSession()
        {
            try
            {
                if (File.Exists(tokenFilePath))
                    File.Delete(tokenFilePath);

                if (File.Exists(userDataFilePath))
                    File.Delete(userDataFilePath);

                App.CurrentSessionToken = null;
                App.CurrentUser = null;
            }
            catch
            {
                // Игнорируем ошибки при очистке
            }
        }
        /// <summary>
        /// Deconectează utilizatorul și îl redirecționează la Login
        /// </summary>
        public static void LogoutAndRedirectToLogin()
        {
            try
            {
                // Curăță sesiunea
                ClearSession();

                // Redirecționează la Login pe thread-ul UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Închide toate ferestrele except Login
                    var windowsToClose = Application.Current.Windows
                        .OfType<Window>()
                        .Where(w => w.GetType() != typeof(Login))
                        .ToList();

                    foreach (Window window in windowsToClose)
                    {
                        window.Close();
                    }

                    // Verifică dacă există deja o fereastră Login deschisă
                    var existingLogin = Application.Current.Windows
                        .OfType<Login>()
                        .FirstOrDefault();

                    if (existingLogin == null)
                    {
                        // Deschide o nouă fereastră Login
                        Login loginWindow = new Login();
                        loginWindow.Show();
                    }
                    else
                    {
                        // Aduce fereastra Login existentă în față
                        existingLogin.WindowState = WindowState.Normal;
                        existingLogin.Activate();
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during logout: {ex.Message}");
                MessageBox.Show($"Eroare la deconectare: {ex.Message}", "Eroare",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public void UpdateSession(User user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.sessionToken))
            {
                throw new ArgumentException("Невозможно обновить сессию с пустыми данными пользователя или токеном");
            }

            SaveSession(user);
            App.CurrentUser = user;
            App.CurrentSessionToken = user.sessionToken;
        }

        private static HttpClient CreateHttpClient()
        {
            var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            return new HttpClient(clientHandler);
        }
    }
}