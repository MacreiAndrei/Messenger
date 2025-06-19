using Messenger.Dtos;
using Messenger.Dtos.ResponseDto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

namespace Messenger.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        private static HttpClient httpClient;
        public SettingsView()
        {
            InitializeComponent();
            InitializeHttpClient();
            tbChangeUsername.Text = App.CurrentUser.username;
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



        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "ATENȚIE: Vrei să ștergi definitiv contul?\n\n" +
                "Această acțiune va șterge:\n" +
                "• Toate conversațiile tale\n" +
                "• Toate datele personale\n" +
                "• Accesul la aplicație\n\n" +
                "Această acțiune NU poate fi anulată!",
                "Confirmare ștergere cont",
                MessageBoxButton.YesNo,
                MessageBoxImage.Stop
            );

            if (result == MessageBoxResult.Yes)
            {
                // Confirmare suplimentară
                var finalConfirm = MessageBox.Show(
                    "Ultima confirmare: Ștergi definitiv contul?",
                    "Confirmare finală",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Stop
                );

                if (finalConfirm == MessageBoxResult.Yes)
                {

                    // Aici ar fi logica pentru ștergerea contului
                    DeleteUser();
                }
            }
        }
        public async void DeleteUser()
        {
            try
            {
                var requestData = new
                {
                    SessionToken = App.CurrentSessionToken
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri(httpClient.BaseAddress + "User/delete-user"),
                    Content = content
                };

                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<ApiResponse<UserResponseDto>>(responseContent);

                    if (responseData.status == "success")
                    {
                        Window window = Window.GetWindow(this);
                        if (window != null)
                        {
                            window.Close();
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Error: {responseData.error}");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Error: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void btChangeUsername_Click(object sender, RoutedEventArgs e)
        {
            ChangeUsername();
        }
        private async void ChangeUsername()
        {
            if (!string.IsNullOrWhiteSpace(tbChangeUsername.Text))
            {
                try
                {
                    var requestData = new
                    {
                        SessionToken = App.CurrentSessionToken,
                        NewUserName = tbChangeUsername.Text,
                    };

                    var json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Put,
                        RequestUri = new Uri(httpClient.BaseAddress + "User/change-user-name"),
                        Content = content
                    };

                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var responseData = JsonConvert.DeserializeObject<ApiResponse<UserResponseDto>>(responseContent);

                        if (responseData.status == "success")
                        {
                            tbChangeUsername.Text = responseData.data.user.username;
                            MessageBox.Show("email changed successfully");
                        }
                        else
                        {
                            MessageBox.Show($"Error: {responseData.error}");
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Error: {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("empty username");
            }
        }

        private void btChangePassword_Click(object sender, RoutedEventArgs e)
        {
            ChangePassword();
        }
        private async void ChangePassword()
        {
            if (!string.IsNullOrWhiteSpace(pbPasswdord1.Password) && !string.IsNullOrWhiteSpace(pbPasswdord2.Password) && pbPasswdord1.Password == pbPasswdord2.Password)
            {
                try
                {
                    var requestData = new
                    {
                        SessionToken = App.CurrentSessionToken,
                        NewPassword = pbPasswdord1.Password,
                    };

                    var json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Put,
                        RequestUri = new Uri(httpClient.BaseAddress + "User/change-user-password"),
                        Content = content
                    };

                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var responseData = JsonConvert.DeserializeObject<ApiResponse<UserResponseDto>>(responseContent);

                        if (responseData.status == "success")
                        {
                            MessageBox.Show("password changed successfully");
                        }
                        else
                        {
                            MessageBox.Show($"Error: {responseData.error}");
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Error: {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }

            }
            else
            {
                MessageBox.Show("passwords dont match");
            }
        }
    }
}