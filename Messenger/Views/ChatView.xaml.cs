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
using System.Net.Http;
using Newtonsoft.Json;
using System.Windows.Threading;
using System.Diagnostics;

namespace Messenger.Views
{
    public partial class ChatView : UserControl
    {
        private bool isMessagePlaceholder = true;
        private string currentChatId;
        private string sessionToken;
        private HttpClient httpClient;
        private DispatcherTimer messagePollingTimer;
        private string _userToken;
        private string _username;

        public ChatView(string sessionToken, string username)
        {
            InitializeComponent();
            InitializeHttpClient();
            InitializeMessagePolling();
            _userToken = sessionToken;
            _username = username;
            SetSessionToken(_userToken);
        }

        private void InitializeHttpClient()
        {
            var clientHandler = new HttpClientHandler();

            // For development with localhost HTTPS - bypass SSL certificate validation
            // WARNING: Only use this for development/testing environments!
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
            {
                return true; // Accept all certificates for localhost development
            };

            httpClient = new HttpClient(clientHandler);
            httpClient.BaseAddress = new Uri("https://172.20.10.8:5172");
        }

        private void InitializeMessagePolling()
        {
            messagePollingTimer = new DispatcherTimer();
            messagePollingTimer.Interval = TimeSpan.FromSeconds(0.5);
            messagePollingTimer.Tick += async (s, e) => await PollForNewMessages();
        }


        public void SetSessionToken(string token)
        {
            sessionToken = token;
            LoadUserChats();
        }

        private async void LoadUserChats()
        {
            try
            {
                var requestData = new
                {
                    SessionToken = sessionToken
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("Chat/all-users-chats", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var chats = JsonConvert.DeserializeObject<ChatInfoResponse>(responseContent);
                    foreach(var chat in chats.data)
                    {
                        Debug.WriteLine(chat.ChatName);
                    }
                    PopulateContactsList(chats.data);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading chats: {ex.Message}");
            }
        }

        private void PopulateContactsList(List<ChatInfo> chats)
        {
            ContactsList.Children.Clear();

            foreach (var chat in chats)
            {
                var border = CreateContactBorder(chat);
                ContactsList.Children.Add(border);
            }
        }

        private Border CreateContactBorder(ChatInfo chat)
        {
            var border = new Border
            {
                Tag = chat.ChatID,
                Background = Brushes.Transparent,
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8),
                CornerRadius = new CornerRadius(8),
                Cursor = Cursors.Hand
            };

            border.MouseLeftButtonUp += Contact_Click;

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            // Avatar
            var avatarBorder = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                Background = new SolidColorBrush(Color.FromRgb(139, 92, 246)),
                Margin = new Thickness(0, 0, 12, 0)
            };

            var avatarText = new TextBlock
            {
                Text = chat.ChatName.Substring(0, 1).ToUpper(),
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            avatarBorder.Child = avatarText;

            // Text content
            var textPanel = new StackPanel();

            var nameText = new TextBlock
            {
                Text = chat.ChatName,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39))
            };

            var statusText = new TextBlock
            {
                Text = "Click to open chat",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };

            textPanel.Children.Add(nameText);
            textPanel.Children.Add(statusText);

            stackPanel.Children.Add(avatarBorder);
            stackPanel.Children.Add(textPanel);

            border.Child = stackPanel;

            return border;
        }

        private void GroupsTab_Click(object sender, RoutedEventArgs e)
        {
            GroupsTab.BorderBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246));
            GroupsTab.Foreground = new SolidColorBrush(Color.FromRgb(139, 92, 246));

            PeopleTab.BorderBrush = Brushes.Transparent;
            PeopleTab.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));

            FilterContactsList("groups");
        }

        private void PeopleTab_Click(object sender, RoutedEventArgs e)
        {
            PeopleTab.BorderBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246));
            PeopleTab.Foreground = new SolidColorBrush(Color.FromRgb(139, 92, 246));

            GroupsTab.BorderBrush = Brushes.Transparent;
            GroupsTab.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));

            FilterContactsList("people");
        }

        private void FilterContactsList(string filter)
        {
            // For now, show all contacts since we don't have group/person distinction from API
            foreach (Border contact in ContactsList.Children)
            {
                contact.Visibility = Visibility.Visible;
            }
        }

        private async void Contact_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var chatId = border?.Tag?.ToString();

            if (!string.IsNullOrEmpty(chatId))
            {
                currentChatId = chatId;

                // Show chat area
                EmptyState.Visibility = Visibility.Collapsed;
                ChatArea.Visibility = Visibility.Visible;

                // Update chat header
                await UpdateChatHeader(chatId);

                // Load messages
                await LoadMessagesForChat(chatId);

                // Start polling for new messages
                messagePollingTimer.Start();

                // Scroll to bottom
                MessagesScrollViewer.ScrollToBottom();
            }
        }

        private async Task UpdateChatHeader(string chatId)
        {
            // For now, use a simple approach
            ChatContactName.Text = $"Chat {chatId}";
            ChatContactStatus.Text = "Online";
            ChatAvatarText.Text = chatId.Substring(0, 1);
            ChatAvatar.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246));
            ChatOnlineStatus.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            ChatOnlineStatus.Visibility = Visibility.Visible;
        }

        private async Task LoadMessagesForChat(string chatId)
        {
            try
            {
                var requestData = new MessageType2
                {
                    SessionToken = sessionToken,
                    ChatID = chatId
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("Message/get-chat-messages", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<ServerMessageResponse>(responseContent);

                    if (responseData.status == "success")
                    {
                        var messages = responseData.data.messages.Select(m => new MessageInfo
                        {
                            MessageID = m.MessageID.ToString(),
                            Content = m.Content,
                            SenderUsername = m.Sender.Username,
                            Timestamp = m.TimeStamp,
                            ChatID = m.ChatID.ToString()
                        }).ToList();

                        DisplayMessages(messages);
                    }
                    else
                    {
                        MessageBox.Show($"Error: {responseData.error}");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Error loading messages: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading messages: {ex.Message}");
            }
        }

        private async Task PollForNewMessages()
        {
            if (string.IsNullOrEmpty(currentChatId) || string.IsNullOrEmpty(sessionToken))
                return;

            try
            {
                var requestData = new MessageType2
                {
                    SessionToken = sessionToken,
                    ChatID = currentChatId
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("Message/get-new-chat-messages", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<ServerMessageResponse>(responseContent);

                    if (responseData.status == "success" && responseData.data.messages?.Count > 0)
                    {
                        // Получаем ID последнего сообщения (если есть)
                        var lastMessageId = MessagesPanel.Children.Count > 0 ?
                            (MessagesPanel.Children[MessagesPanel.Children.Count - 1] as FrameworkElement)?.Tag?.ToString() : null;

                        var newMessages = responseData.data.messages
                            .Where(m => lastMessageId == null || m.MessageID.ToString() != lastMessageId)
                            .Select(m => new MessageInfo
                            {
                                MessageID = m.MessageID.ToString(),
                                Content = m.Content,
                                SenderUsername = m.Sender.Username,
                                Timestamp = m.TimeStamp,
                                ChatID = m.ChatID.ToString()
                            }).ToList();

                        foreach (var message in newMessages)
                        {
                            AddMessageToUI(message);
                        }

                        if (newMessages.Count > 0)
                        {
                            MessagesScrollViewer.ScrollToBottom();
                        }
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Error polling new messages: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Polling error: {ex.Message}");
            }
        }



        private void DisplayMessages(List<MessageInfo> messages)
        {
            MessagesPanel.Children.Clear();

            if (messages != null)
            {
                foreach (var message in messages)
                {
                    AddMessageToUI(message);
                }
            }
        }

        private void AddMessageToUI(MessageInfo message)
        {
            // Проверяем, не было ли уже добавлено это сообщение
            foreach (var child in MessagesPanel.Children)
            {
                if (child is FrameworkElement element && element.Tag?.ToString() == message.MessageID)
                {
                    return; // Сообщение уже есть в чате
                }
            }

            // Determine if message is from current user (this would need proper implementation)
            bool isMyMessage = message.SenderUsername == _username; // This should be dynamic

            var messageBorder = new Border
            {
                Style = (Style)FindResource(isMyMessage ? "MyMessageStyle" : "OtherMessageStyle"),
                Margin = new Thickness(0, 0, 0, 8),
                Tag = message.MessageID // Сохраняем ID сообщения для проверки дубликатов
            };

            var messagePanel = new StackPanel();

            // Add sender name for other messages
            if (!isMyMessage)
            {
                var senderText = new TextBlock
                {
                    Text = message.SenderUsername,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(139, 92, 246)),
                    Margin = new Thickness(0, 0, 0, 2)
                };
                messagePanel.Children.Add(senderText);
            }

            var contentText = new TextBlock
            {
                Text = message.Content,
                FontSize = 14,
                Foreground = isMyMessage ? Brushes.White : new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                TextWrapping = TextWrapping.Wrap
            };

            var timeText = new TextBlock
            {
                Text = message.Timestamp.ToString("HH:mm"),
                FontSize = 10,
                Foreground = isMyMessage ?
                    new SolidColorBrush(Color.FromRgb(200, 200, 200)) :
                    new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 2, 0, 0)
            };

            messagePanel.Children.Add(contentText);
            messagePanel.Children.Add(timeText);

            messageBorder.Child = messagePanel;
            MessagesPanel.Children.Add(messageBorder);
        }
        private void AddMessage(string messageText, bool isMyMessage)
        {
            var messageBorder = new Border
            {
                Style = (Style)FindResource(isMyMessage ? "MyMessageStyle" : "OtherMessageStyle")
            };

            var textBlock = new TextBlock
            {
                Text = messageText,
                FontSize = 14,
                Foreground = isMyMessage ? Brushes.White : new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                TextWrapping = TextWrapping.Wrap
            };

            messageBorder.Child = textBlock;
            MessagesPanel.Children.Add(messageBorder);
        }

        private void MessageTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (isMessagePlaceholder)
            {
                MessageTextBox.Text = "";
                MessageTextBox.Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81));
                isMessagePlaceholder = false;
            }
        }

        private void MessageTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MessageTextBox.Text))
            {
                MessageTextBox.Text = "Type your message here...";
                MessageTextBox.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                isMessagePlaceholder = true;
            }
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
            }
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private async void SendMessage()
        {
            if (!isMessagePlaceholder && !string.IsNullOrWhiteSpace(MessageTextBox.Text) && !string.IsNullOrEmpty(currentChatId))
            {
                try
                {
                    var messageContent = MessageTextBox.Text;

                    var requestData = new
                    {
                        SessionToken = sessionToken,
                        Content = messageContent,
                        ChatID = currentChatId
                    };

                    var json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync("Message/Save", content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Clear textbox
                        MessageTextBox.Text = "Type your message here...";
                        MessageTextBox.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                        isMessagePlaceholder = true;

                        // The message will appear via polling, but we can add it immediately for better UX
                        var tempMessage = new MessageInfo
                        {
                            Content = messageContent,
                            SenderUsername = "alice", // This should be the current user
                            Timestamp = DateTime.Now
                        };
                        AddMessageToUI(tempMessage);

                        // Scroll to bottom
                        MessagesScrollViewer.UpdateLayout();
                        MessagesScrollViewer.ScrollToBottom();
                    }
                    else
                    {
                        MessageBox.Show("Failed to send message.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error sending message: {ex.Message}");
                }
            }
        }

        public void Cleanup()
        {
            messagePollingTimer?.Stop();
            httpClient?.Dispose();
        }
    }

    // Data models

    public class MessageType2
    {
        public string SessionToken { get; set; }
        public string ChatID { get; set; }
    }

    public class ServerMessageResponse
    {
        public string status { get; set; }
        public string error { get; set; }
        public MessageData data { get; set; }
    }

    public class MessageData
    {
        public List<ServerMessage> messages { get; set; }
    }

    public class ServerMessage
    {
        public int MessageID { get; set; }
        public int UserID { get; set; }
        public string Content { get; set; }
        public DateTime TimeStamp { get; set; }
        public int ChatID { get; set; }
        public bool IsSeen { get; set; }
        public bool IsFile { get; set; }
        public MessageSender Sender { get; set; }
    }

    public class MessageSender
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string UserProfilePicturePath { get; set; }
    }

    public class ChatInfoResponse
    {
        public string status { get; set; }
        public List<ChatInfo> data { get; set; }
    }

    public class ChatInfo
    {
        public string ChatID { get; set; }
        public string ChatName { get; set; }
    }

    public class MessageInfo
    {
        public string MessageID { get; set; }
        public string Content { get; set; }
        public string SenderUsername { get; set; }
        public DateTime Timestamp { get; set; }
        public string ChatID { get; set; }
    }
}