using Messenger.Dtos.ResponseDto;
using Messenger.Models;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Messenger.Views
{
    public partial class ChatView : UserControl
    {
        private bool isMessagePlaceholder = true;
        private string currentChatId;
        private static HttpClient httpClient;
        public static DispatcherTimer messagePollingTimer;
        private string _chatId;

        private bool _isChatCreate = true;
        private bool _isAddUser = false;
        private bool _isRenameChat = false;

        private bool _isEditMessage = false;
        private Message _editMessage;

        public ChatView()
        {
            InitializeComponent();
            InitializeHttpClient();
            InitializeMessagePolling();
            LoadUserChats();
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
            messagePollingTimer.Interval = TimeSpan.FromSeconds(2);
            messagePollingTimer.Tick += async (s, e) => await PollForNewMessages();
            messagePollingTimer.Tick += async (s, e) => await PollForNewDeletedMessages();
            messagePollingTimer.Tick += async (s, e) => await PollForNewEditedMessages();
        }

        private async void LoadUserChats()
        {
            try
            {
                var requestData = new
                {
                    SessionToken = App.CurrentSessionToken
                };

                var json = JsonSerializer.Serialize(requestData);
                var jsonResponse = await PostRequestAsync("Chat/all-users-chats", json);
                var response = await ConvertApiResponse<AllUsersChatsResponseDto>(jsonResponse);
                if (response == null)
                {
                    MessageBox.Show("Eroare la procesarea răspunsului de la server.", "Eroare",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                if (response.status == "success" && response.data.chats != null)
                {
                    PopulateContactsList(response.data.chats);
                }
                else if(response.status == "error")
                {
                    MessageBox.Show($"Error loading chats: {response.error}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading chats: {ex.Message}");
            }
        }
        private void PopulateContactsList(List<Chat> chats)
        {
            ContactsList.Children.Clear();

            foreach (var chat in chats)
            {
                var border = CreateContactBorder(chat);
                ContactsList.Children.Add(border);
            }
        }

        private Border CreateContactBorder(Chat chat)
        {
            var border = new Border
            {
                Tag = chat.chatID + "o_O >_< T_T" + chat.chatName,
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
                Text = chat.chatName.Substring(0, 1).ToUpper(),
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
                Text = chat.chatName,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39))
            };
            textPanel.Children.Add(nameText);

            if(chat.lastMessage != null && chat.lastMessage.content != null)
            {
                var statusText = new TextBlock
                {
                    Text = chat.lastMessage.content,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
                };
                textPanel.Children.Add(statusText);
            }
          


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
                    SessionToken = App.CurrentSessionToken,
                    ChatID = chatId
                };

                var json = JsonSerializer.Serialize(requestData);
                var jsonResponse = await PostRequestAsync("Message/get-chat-messages", json);
                var response = await ConvertApiResponse<MessagesResponseDto>(jsonResponse);

                if (response.status == "success" && response.data.messages != null)
                {
                    DisplayMessages(response.data.messages);
                }
                else if (response.status == "error")
                {
                    MessageBox.Show($"Error loading messages: {response.error}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading messages: {ex.Message}");
            }
        }

        private async Task PollForNewMessages()
        {
            try
            {
                var requestData = new MessageType2
                {
                    SessionToken = App.CurrentSessionToken,
                    ChatID = currentChatId
                };

                var json = JsonSerializer.Serialize(requestData);
                var jsonResponse = await PostRequestAsync("Message/get-new-chat-messages", json);
                var response = await ConvertApiResponse<MessagesResponseDto>(jsonResponse);

                if (response.status == "success" && response.data.messages != null)
                {
                    var lastMessageId = MessagesPanel.Children.Count > 0 ?
                        (MessagesPanel.Children[MessagesPanel.Children.Count - 1] as FrameworkElement)?.Tag?.ToString() : null;

                    var newMessages = response.data.messages
                        .Where(m => lastMessageId == null || m.messageID.ToString() != lastMessageId).ToList();

                    foreach (var message in newMessages)
                    {
                        AddMessageToUI(message);
                    }

                    if (newMessages.Count > 0)
                    {
                        MessagesScrollViewer.ScrollToBottom();
                    }
                }
                else if (response.status == "error")
                {
                    MessageBox.Show($"Error polling new messages: {response.error}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Polling error: {ex.Message}");
            }
        }
        private async Task PollForNewEditedMessages()
        {
            try
            {
                var requestData = new MessageType2
                {
                    SessionToken = App.CurrentSessionToken,
                    ChatID = currentChatId
                };
                var json = JsonSerializer.Serialize(requestData);
                var jsonResponse = await PostRequestAsync("Message/get-updated-messages", json);
                var response = await ConvertApiResponse<MessagesResponseDto>(jsonResponse);

                if (response.status == "success" && response.data.messages != null)
                {
                    foreach (var message in response.data.messages)
                    {
                        foreach (var child in MessagesPanel.Children)
                        {
                            if (child is Border border && border.Tag?.ToString() == message.messageID.ToString())
                            {
                                if (border.Child is StackPanel panel)
                                {
                                    foreach (var item in panel.Children)
                                    {
                                        if (item is TextBlock textBlock &&
                                            textBlock.FontSize == 14)
                                        {
                                            textBlock.Text = message.content;
                                            break;
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
                else if (response.status == "error")
                {
                    MessageBox.Show($"Error polling edited messages: {response.error}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Polling error: {ex.Message}");
            }
        }

        private async Task PollForNewDeletedMessages()
        {
            try
            {
                var requestData = new MessageType2
                {
                    SessionToken = App.CurrentSessionToken,
                    ChatID = currentChatId
                };

                var json = JsonSerializer.Serialize(requestData);
                var jsonResponse = await PostRequestAsync("Message/get-deleted-messages", json);
                var response = await ConvertApiResponse<DeletedMessadesResponseDto>(jsonResponse);

                if (response.status == "success" && response.data.messages != null)
                {
                    foreach (var message in response.data.messages)
                    {
                        foreach (var child in MessagesPanel.Children)
                        {
                            if (child is FrameworkElement element && element.Tag?.ToString() == message.ToString())
                            {
                                MessagesPanel.Children.Remove(element);
                                break;
                            }
                        }
                    }
                }
                else if (response.status == "error")
                {
                    MessageBox.Show($"Error polling edited messages: {response.error}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Polling error: {ex.Message}");
            }
        }



        private void DisplayMessages(List<Message> messages)
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

        private void AddMessageToUI(Message message)
        {
            bool isMyMessage = message.sender.username == App.CurrentUser.username;
            if (!isMyMessage)
            {
                foreach (var child in MessagesPanel.Children)
                {
                    if (child is FrameworkElement element && element.Tag?.ToString() == message.messageID.ToString())
                    {
                        return;
                    }
                }
            }

            var messageBorder = new Border
            {
                Style = (Style)FindResource(isMyMessage ? "MyMessageStyle" : "OtherMessageStyle"),
                Margin = new Thickness(0, 0, 0, 8),
                Tag = message.messageID
            };

            var messagePanel = new StackPanel();

            if (!isMyMessage)
            {
                var senderText = new TextBlock
                {
                    Text = message.sender.username,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(139, 92, 246)),
                    Margin = new Thickness(0, 0, 0, 2)
                };
                messagePanel.Children.Add(senderText);
            }

            var contentText = new TextBlock
            {
                Text = message.content,
                FontSize = 14,
                Foreground = isMyMessage ? Brushes.White : new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                TextWrapping = TextWrapping.Wrap
            };

            var timeText = new TextBlock
            {
                Text = message.timeStamp.ToString("HH:mm"),
                FontSize = 10,
                Foreground = isMyMessage ?
                    new SolidColorBrush(Color.FromRgb(200, 200, 200)) :
                    new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 2, 0, 0)
            };


            messagePanel.Children.Add(contentText);
            messagePanel.Children.Add(timeText);

            if (isMyMessage)
            {
                var contextMenu = new ContextMenu();

                var editMenuItem = new MenuItem { Header = "Редактировать" };
                editMenuItem.Click += async (s, e) => EditMessage(message);

                var deleteMenuItem = new MenuItem { Header = "Удалить" };
                deleteMenuItem.Click += (s, e) => DeleteMessage(message);

                contextMenu.Items.Add(editMenuItem);
                contextMenu.Items.Add(deleteMenuItem);

                messageBorder.ContextMenu = contextMenu;
            }

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
                if (!_isEditMessage)
                {
                    try
                    {
                        var messageContent = MessageTextBox.Text;

                        var requestData = new
                        {
                            SessionToken = App.CurrentSessionToken,
                            Content = messageContent,
                            ChatID = currentChatId
                        };
                        var json = JsonSerializer.Serialize(requestData);
                        var jsonResponse = await PostRequestAsync("Message/save", json);
                        var response = await ConvertApiResponse<MessageSendResponseDto>(jsonResponse);
                        if (response.status == "success")
                        {
                            if (!isPressedEnter)
                            {
                                MessageTextBox.Text = "Type your message here...";
                                MessageTextBox.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                                isMessagePlaceholder = true;
                            }
                            AddMessageToUI(response.data.message);

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
                else
                {
                    try
                    {
                        var requestData = new
                        {
                            SessionToken = App.CurrentSessionToken,
                            MessageID = _editMessage.messageID,
                            Content = MessageTextBox.Text,
                        };

                        var json = JsonSerializer.Serialize(requestData);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var request = new HttpRequestMessage
                        {
                            Method = HttpMethod.Put,
                            RequestUri = new Uri(httpClient.BaseAddress + "Message/message-update"),
                            Content = content
                        };
                        var response = await httpClient.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            _editMessage.content = MessageTextBox.Text;
                            MessageTextBox.Text = "Type your message here...";
                            MessageTextBox.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                            isMessagePlaceholder = true;
                            _isEditMessage = false;
                        }
                        else
                        {
                            MessageBox.Show("Failed to update message.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error sending message: {ex.Message}");
                    }
                }
            }
        }
    
        public async void EditMessage(Message message)
        {
            _isEditMessage = true;
            _editMessage = message;
            MessageTextBox.Text = message.content;
            isMessagePlaceholder = false;
        }

        public async void DeleteMessage(Message message)
        {
            try
            {

                var requestData = new
                {
                    SessionToken = App.CurrentSessionToken,
                    MessageID = message.messageID
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri(httpClient.BaseAddress + "Message/message-delete"),
                    Content = content
                };

                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    foreach (var child in MessagesPanel.Children)
                    {
                        if (child is FrameworkElement element && element.Tag?.ToString() == message.messageID.ToString())
                        {
                            MessagesPanel.Children.Remove(element);
                            break;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Failed to delete message.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
            }
        }

        public void Cleanup()
        {
            messagePollingTimer?.Stop();
            httpClient?.Dispose();
        }
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isChatCreate) { CreateNewChat(); }
            if (_isAddUser) { AddUserToChat(); }
            if (_isRenameChat) { UpdateChatName(); }
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
                SearchTextBox.Text = "Create new chat...";
        }

        private async void CreateNewChat()
        {
            try
            {
                var requestData = new
                {
                    SessionToken = App.CurrentSessionToken,
                    ChatName = SearchTextBox.Text
                };

                var json = JsonSerializer.Serialize(requestData);
                var jsonResponse = await PostRequestAsync("Chat/new-chat", json);
                var response = await ConvertApiResponse<Chat>(jsonResponse);

                if (response.status == "success")
                {
                    LoadUserChats();
                }
                else
                {
                    MessageBox.Show($"Error loading chats: {response.error}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
            }
        }

        private void tbAddUserToChat(object sender, RoutedEventArgs e)
        {
            _isChatCreate = false;
            _isAddUser = true;
            _isRenameChat = false;
            SearchTextBox.Text = "Add user to chat chat...";
        }
        public async void AddUserToChat()
        {
            try
            {
                var requestData = new
                {
                    SessionToken = App.CurrentSessionToken,
                    ChatID = _chatId,
                    Username = SearchTextBox.Text
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("Chat/add-user-in-chat", content);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"User {SearchTextBox.Text} added successfully");
                    _isChatCreate = true;
                    _isAddUser = false;
                    _isRenameChat = false;
                    SearchTextBox.Text = "Create new chat...";
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
                    SessionToken = App.CurrentSessionToken,
                    ChatID = _chatId
                };

                var json = JsonSerializer.Serialize(requestData);
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
                    _isChatCreate = true;
                    _isAddUser = false;
                    _isRenameChat = false;
                        SearchTextBox.Text = "Create new chat...";
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
            _isChatCreate = false;
            _isAddUser = false;
            _isRenameChat = true;
            SearchTextBox.Text = "New chat name...";
        }

        public async void UpdateChatName()
        {
            try
            {
                var requestData = new
                {
                    SessionToken = App.CurrentSessionToken,
                    ChatID = _chatId,
                    ChatName = SearchTextBox.Text
                };
                Debug.Write(App.CurrentSessionToken);
                Debug.Write(_chatId);
                Debug.Write(SearchTextBox.Text);
                var json = JsonSerializer.Serialize(requestData);
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
        private async Task<string> PostRequestAsync(string url, string json)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await httpClient.PostAsync(url, content);
            return await response.Content.ReadAsStringAsync();
        }
        private async  Task<ApiResponse<T>> ConvertApiResponse<T>(string response)
        {
            return JsonSerializer.Deserialize<ApiResponse<T>>(response);
        }
    }

    public class MessageType2
    {
        public string SessionToken { get; set; }
        public string ChatID { get; set; }
    }
}