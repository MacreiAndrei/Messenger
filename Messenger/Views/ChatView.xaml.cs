using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static System.Net.Mime.MediaTypeNames;

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
        private string _chatId;

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

            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
            {
                return true;
            };

            httpClient = new HttpClient(clientHandler);
            httpClient.BaseAddress = new Uri(ApiConfig.BASE_URL);
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
                Tag = chat.ChatID + "o_O >_< T_T" + chat.ChatName,
                Background = Brushes.Transparent,
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8),
                CornerRadius = new CornerRadius(8),
                Cursor = Cursors.Hand
            };

            border.MouseLeftButtonUp += Contact_Click;

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

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

        private async void Contact_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var chatId = border?.Tag?.ToString().Split("o_O >_< T_T")[0];
            var chatName = border?.Tag?.ToString().Split("o_O >_< T_T")[1];
            _chatId = border?.Tag?.ToString().Split("o_O >_< T_T")[0];

            if (!string.IsNullOrEmpty(chatId) && !string.IsNullOrEmpty(chatName))
            {
                currentChatId = chatId;
                EmptyState.Visibility = Visibility.Collapsed;
                ChatArea.Visibility = Visibility.Visible;

                await UpdateChatHeader(chatName);

                await LoadMessagesForChat(chatId);

                messagePollingTimer.Start();

                MessagesScrollViewer.ScrollToBottom();
            }
        }

        private async Task UpdateChatHeader(string chatName)
        {
            ChatContactName.Text = chatName;
            ChatContactStatus.Text = "Online";
            ChatAvatarText.Text = chatName.Substring(0, 1);
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
            bool isMyMessage = message.SenderUsername == _username;
            if (!isMyMessage)
            {
                foreach (var child in MessagesPanel.Children)
                {
                    if (child is FrameworkElement element && element.Tag?.ToString() == message.MessageID)
                    {
                        return;
                    }
                }
            }

            var messageBorder = new Border
            {
                Style = (Style)FindResource(isMyMessage ? "MyMessageStyle" : "OtherMessageStyle"),
                Margin = new Thickness(0, 0, 0, 8),
                Tag = message.MessageID
            };

            var messagePanel = new StackPanel();

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
                SendMessage(true);
                MessageTextBox.Text = "";
            }
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            SendMessage(false);
        }

        private async void SendMessage(bool isPressedEnter)
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
                        if (!isPressedEnter)
                        {
                            MessageTextBox.Text = "Type your message here...";
                            MessageTextBox.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                            isMessagePlaceholder = true;
                        }
                        var tempMessage = new MessageInfo
                        {
                            Content = messageContent,
                            SenderUsername = _username,
                            Timestamp = DateTime.Now
                        };
                        AddMessageToUI(tempMessage);

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
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            CreateNewChat();
        }


        // Close dropdown when clicking outside

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(SearchTextBox.Text))
                SearchTextBox.Text = "Create new chat...";
        }

        private async void CreateNewChat()
        {
            try
            {
                var ChatName = SearchTextBox.Text;

                var requestData = new
                {
                    SessionToken = sessionToken,
                    ChatName = ChatName
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("Chat/new-chat", content);

                if (response.IsSuccessStatusCode)
                {
                    LoadUserChats();
                }
                else
                {
                    MessageBox.Show("Failed to create chat.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
            }
        }

        private void tbAddUserToChat(object sender, RoutedEventArgs e)
        {
            AddUserToChat();
        }
        public async void AddUserToChat()
        {
            try
            {
                var requestData = new
                {
                    SessionToken = sessionToken,
                    ChatID = _chatId,
                    Username = SearchTextBox.Text
                };
                Debug.Write(sessionToken);
                Debug.Write(_chatId);
                Debug.Write(SearchTextBox.Text);
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("Chat/add-user-in-chat", content);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"User {SearchTextBox.Text} added successfully");
                }
                else
                {
                    MessageBox.Show("Verify username.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
            }
        }

        private void dtDeleteChat_Click(object sender, RoutedEventArgs e)
        {
            DeleteChat();
        }
        private async void DeleteChat()
        {
            try
            {
                var ChatName = SearchTextBox.Text;

                var requestData = new
                {
                    SessionToken = sessionToken,
                    ChatID = _chatId
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri(httpClient.BaseAddress + "Chat/delete-chat"),
                    Content = content
                };

                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    EmptyState.Visibility = Visibility.Visible;
                    ChatArea.Visibility = Visibility.Collapsed;
                    LoadUserChats();
                }
                else
                {
                    MessageBox.Show("Failed to delete chat.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
            }
        }

        private void btUpdateChatName(object sender, RoutedEventArgs e)
        {
            UpdateChatName();
        }

        public async void UpdateChatName()
        {
            try
            {
                var requestData = new
                {
                    SessionToken = sessionToken,
                    ChatID = _chatId,
                    ChatName = SearchTextBox.Text
                };
                Debug.Write(sessionToken);
                Debug.Write(_chatId);
                Debug.Write(SearchTextBox.Text);
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri(httpClient.BaseAddress + "Chat/update-chat"),
                    Content = content
                };

                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    EmptyState.Visibility = Visibility.Visible;
                    ChatArea.Visibility = Visibility.Collapsed;
                    LoadUserChats();
                }
                else
                {
                    MessageBox.Show("Huston, we have a problem.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
            }
        }
    }

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