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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Messenger.Views
{
    /// <summary>
    /// Interaction logic for NotificationsView.xaml
    /// </summary>
    public partial class NotificationsView : UserControl
    {
        public NotificationsView()
        {
            InitializeComponent();
            LoadNotifications();
        }

        private void LoadNotifications()
        {
            // Aici poți încărca notificările din baza de date sau din alt serviciu
            // Pentru moment, notificările sunt hardcodate în XAML
        }

        private void MarkAllAsRead_Click(object sender, RoutedEventArgs e)
        {
            // Găsește toate notificările necitite și le marchează ca citite
            var unreadNotifications = GetUnreadNotifications();

            foreach (var notification in unreadNotifications)
            {
                // Schimbă stilul de la UnreadNotificationStyle la NotificationItemStyle
                notification.Style = FindResource("NotificationItemStyle") as Style;

                // Elimină punctul de notificare (ellipse-ul violet)
                var grid = notification.Child as Grid;
                if (grid != null)
                {
                    var stackPanel = grid.Children[1] as StackPanel;
                    if (stackPanel != null)
                    {
                        var titleGrid = stackPanel.Children[0] as Grid;
                        if (titleGrid != null && titleGrid.Children.Count > 1)
                        {
                            var ellipse = titleGrid.Children[1] as Ellipse;
                            if (ellipse != null)
                            {
                                ellipse.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                }
            }

            // Afișează un mesaj de confirmare
            MessageBox.Show("Toate notificările au fost marcate ca citite.", "Confirmare",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AllFilter_Click(object sender, RoutedEventArgs e)
        {
            SetActiveFilter(AllFilter);
            ShowAllNotifications();
        }

        private void UnreadFilter_Click(object sender, RoutedEventArgs e)
        {
            SetActiveFilter(UnreadFilter);
            ShowUnreadNotifications();
        }

        private void MessagesFilter_Click(object sender, RoutedEventArgs e)
        {
            SetActiveFilter(MessagesFilter);
            ShowMessageNotifications();
        }

        private void SetActiveFilter(Button activeButton)
        {
            // Resetează toate butoanele de filtru
            var normalStyle = FindResource("FilterButtonStyle") as Style;

            AllFilter.Background = Brushes.Transparent;
            AllFilter.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));
            AllFilter.BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219));
            AllFilter.BorderThickness = new Thickness(1);

            UnreadFilter.Background = Brushes.Transparent;
            UnreadFilter.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));
            UnreadFilter.BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219));
            UnreadFilter.BorderThickness = new Thickness(1);

            MessagesFilter.Background = Brushes.Transparent;
            MessagesFilter.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));
            MessagesFilter.BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219));
            MessagesFilter.BorderThickness = new Thickness(1);

            // Setează butonul activ
            activeButton.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246));
            activeButton.Foreground = Brushes.White;
            activeButton.BorderThickness = new Thickness(0);
        }

        private void ShowAllNotifications()
        {
            var notifications = GetAllNotifications();
            foreach (var notification in notifications)
            {
                notification.Visibility = Visibility.Visible;
            }

            // Ascunde mesajul de notificări goale dacă există notificări
            EmptyNotifications.Visibility = notifications.Any() ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ShowUnreadNotifications()
        {
            var allNotifications = GetAllNotifications();
            var unreadNotifications = GetUnreadNotifications();

            foreach (var notification in allNotifications)
            {
                notification.Visibility = unreadNotifications.Contains(notification) ?
                    Visibility.Visible : Visibility.Collapsed;
            }

            // Afișează mesajul de notificări goale dacă nu sunt notificări necitite
            EmptyNotifications.Visibility = unreadNotifications.Any() ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ShowMessageNotifications()
        {
            var allNotifications = GetAllNotifications();
            var messageNotifications = GetMessageNotifications();

            foreach (var notification in allNotifications)
            {
                notification.Visibility = messageNotifications.Contains(notification) ?
                    Visibility.Visible : Visibility.Collapsed;
            }

            // Afișează mesajul de notificări goale dacă nu sunt notificări de mesaje
            EmptyNotifications.Visibility = messageNotifications.Any() ? Visibility.Collapsed : Visibility.Visible;
        }

        private List<Border> GetAllNotifications()
        {
            var notifications = new List<Border>();

            foreach (var child in NotificationsContainer.Children)
            {
                if (child is Border border && border != EmptyNotifications)
                {
                    notifications.Add(border);
                }
            }

            return notifications;
        }

        private List<Border> GetUnreadNotifications()
        {
            var unreadNotifications = new List<Border>();

            foreach (var child in NotificationsContainer.Children)
            {
                if (child is Border border && border != EmptyNotifications)
                {
                    // Verifică dacă notificarea folosește stilul pentru notificări necitite
                    if (border.Style == FindResource("UnreadNotificationStyle") as Style)
                    {
                        unreadNotifications.Add(border);
                    }
                }
            }

            return unreadNotifications;
        }


        private List<Border> GetMessageNotifications()
        {
            var messageNotifications = new List<Border>();

            foreach (var child in NotificationsContainer.Children)
            {
                if (child is Border border && border != EmptyNotifications)
                {
                    // Verifică dacă notificarea conține un mesaj
                    var grid = border.Child as Grid;
                    if (grid != null)
                    {
                        var stackPanel = grid.Children[1] as StackPanel;
                        if (stackPanel != null)
                        {
                            // Prima copil al StackPanel-ului este un Grid care conține titlul
                            var titleGrid = stackPanel.Children[0] as Grid;
                            if (titleGrid != null)
                            {
                                // Primul copil al Grid-ului de titlu este TextBlock-ul cu titlul
                                var titleTextBlock = titleGrid.Children[0] as TextBlock;
                                if (titleTextBlock != null && titleTextBlock.Text.ToLower().Contains("mesaj"))
                                {
                                    messageNotifications.Add(border);
                                }
                            }
                            else
                            {
                                // Fallback: dacă primul copil nu este un Grid, încearcă direct ca TextBlock
                                var titleTextBlock = stackPanel.Children[0] as TextBlock;
                                if (titleTextBlock != null && titleTextBlock.Text.ToLower().Contains("mesaj"))
                                {
                                    messageNotifications.Add(border);
                                }
                            }
                        }
                    }
                }
            }

            return messageNotifications;
        }

        // Metodă publică pentru a adăuga notificări noi din exterior
        public void AddNotification(string title, string message, string time, bool isUnread = true, string iconType = "message")
        {
            var notification = CreateNotificationElement(title, message, time, isUnread, iconType);
            NotificationsContainer.Children.Insert(0, notification); // Adaugă la început
        }

        private Border CreateNotificationElement(string title, string message, string time, bool isUnread, string iconType)
        {
            var border = new Border();
            border.Style = FindResource(isUnread ? "UnreadNotificationStyle" : "NotificationItemStyle") as Style;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon
            var iconBorder = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                Background = new SolidColorBrush(Color.FromRgb(139, 92, 246))
            };
            Grid.SetColumn(iconBorder, 0);

            // Content
            var stackPanel = new StackPanel { Margin = new Thickness(15, 0, 0, 0) };

            var titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleTextBlock = new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55))
            };
            Grid.SetColumn(titleTextBlock, 0);

            if (isUnread)
            {
                var ellipse = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(Color.FromRgb(139, 92, 246)),
                    Margin = new Thickness(10, 0, 0, 0)
                };
                Grid.SetColumn(ellipse, 1);
                titleGrid.Children.Add(ellipse);
            }

            titleGrid.Children.Add(titleTextBlock);
            stackPanel.Children.Add(titleGrid);

            var messageTextBlock = new TextBlock
            {
                Text = message,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 5, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(messageTextBlock);

            Grid.SetColumn(stackPanel, 1);

            // Time
            var timeTextBlock = new TextBlock
            {
                Text = time,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(timeTextBlock, 2);

            grid.Children.Add(iconBorder);
            grid.Children.Add(stackPanel);
            grid.Children.Add(timeTextBlock);

            border.Child = grid;
            return border;
        }
    }
}