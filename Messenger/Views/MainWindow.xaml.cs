
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Messenger.Models;
using Messenger.Views;

namespace Messenger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string _userToken;
        public string _username;
        public bool _isMaximized = false;
        public MainWindow(string userToken, string username)
        {
            InitializeComponent();
            _userToken = userToken;
            _username = username;
                tbUserName.Text = App.CurrentUser.username[0].ToString();
            SetActiveButton(ChatButton);
            ShowView("Chat");
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(HomeButton);
            ShowView("Home");
        }

        private void ChatButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(ChatButton);
            ShowView("Chat");
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(NotificationButton);
            ShowView("Notifications");
            NotificationDot.Visibility = Visibility.Collapsed;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(SettingsButton);
            ShowView("Settings");
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Întreabă utilizatorul dacă vrea să se deconecteze
            MessageBoxResult result = MessageBox.Show(
                "Ești sigur că vrei să te deconectezi?",
                "Confirmare deconectare",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SessionManager.ClearSession();
                Login login = new Login();
                login.Show();
                ChatView.messagePollingTimer.Stop();
                this.Close();
            }
        }

        private void ShowView(string viewName)
        {
            UserControl? view = viewName switch
            {
                "Home" => new Views.HomeView(),
                "Chat" => new Views.ChatView(),
                "Notifications" => new Views.NotificationsView(),
                "Settings" => new Views.SettingsView(),
                _ => null
            };

            if (view != null)
            {
                MainContent.Content = view;
            }
        }

        private void SetActiveButton(Button activeButton)
        {
            var normalStyle = FindResource("SidebarButtonStyle") as Style;
            var activeStyle = FindResource("ActiveSidebarButtonStyle") as Style;

            HomeButton.Style = normalStyle;
            ChatButton.Style = normalStyle;
            NotificationButton.Style = normalStyle;
            SettingsButton.Style = normalStyle;

            activeButton.Style = activeStyle;
        }

        public void ShowNotificationDot()
        {
            NotificationDot.Visibility = Visibility.Visible;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (!_isMaximized)
            {
                this.WindowState = WindowState.Maximized;
                _isMaximized = true;
            }
            else
            {
                this.WindowState = WindowState.Normal;
                _isMaximized = false;
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState= WindowState.Minimized;
        }

        private void Sidebar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}