using Activity_Finder.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
namespace Activity_Finder
{
    public class ChatMessageUI
    {
        public string Sender { get; set; }
        public string Text { get; set; }
        public string Time { get; set; }

        public string ProfileImagePath { get; set; }

        public Visibility ShowAvatar { get; set; }

        public HorizontalAlignment Alignment { get; set; }

        public Brush BubbleColor { get; set; }

        public Brush SenderColor { get; set; }

        public CornerRadius BubbleCornerRadius { get; set; }

        public string SeenByText { get; set; }

        public Visibility SeenVisibility { get; set; }
        public int UserId { get; set; }
    }
    public partial class ChatWindow : UserControl
    {
        private readonly User _currentUser;
        private readonly Hobby _hobby;

        public ChatWindow(Hobby hobby,User currentUser)
        {
            InitializeComponent();
            _hobby = hobby;
            _currentUser = currentUser;
            ;

            ChatTitleText.Text = hobby.Name;

            LoadMessages();
            MarkChatAsRead();
        }
        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            EmojiPopup.IsOpen = true;
        }

        private void Emoji_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content != null)
            {
                string emoji = btn.Content.ToString();

                int caretIndex = MessageTextBox.CaretIndex;

                MessageTextBox.Text =
                    MessageTextBox.Text.Insert(caretIndex, emoji);

                MessageTextBox.CaretIndex =
                    caretIndex + emoji.Length;

                MessageTextBox.Focus();

                EmojiPopup.IsOpen = false;
            }
        }
        private void MarkChatAsRead()
        {
            using (var context = new AppDbContext())
            {
                var status = context.ChatReadStatuses.FirstOrDefault(r =>
                    r.UserId == _currentUser.Id &&
                    r.HobbyId == _hobby.Id);

                if (status == null)
                {
                    status = new ChatReadStatus
                    {
                        UserId = _currentUser.Id,
                        HobbyId = _hobby.Id,
                        LastReadAt = DateTime.Now
                    };

                    context.ChatReadStatuses.Add(status);
                }
                else
                {
                    status.LastReadAt = DateTime.Now;
                }

                context.SaveChanges();
            }
        }
        private void LoadMessages()
        {
            using (var context = new AppDbContext())
            {
                var messages = context.ChatMessages
                    .Include(m => m.User)
                    .Where(m => m.HobbyId == _hobby.Id)
                    .OrderBy(m => m.SentAt)
                    .ToList(); 
                foreach (var message in messages)
                {
                    if (message.UserId == _currentUser.Id)
                        continue;

                    bool alreadySeen = context.ChatMessageSeens.Any(s =>
                        s.ChatMessageId == message.Id &&
                        s.UserId == _currentUser.Id);

                    if (!alreadySeen)
                    {
                        context.ChatMessageSeens.Add(new ChatMessageSeen
                        {
                            ChatMessageId = message.Id,
                            UserId = _currentUser.Id,
                            SeenAt = DateTime.Now
                        });
                    }
                }

                context.SaveChanges();

                var uiMessages = messages.Select(m => new ChatMessageUI
                {
                    Sender = m.User.Username,
                    Text = m.Message,
                    Time = m.SentAt.ToString("HH:mm"),
                    UserId = m.UserId,

                    ProfileImagePath = m.User.ProfileImagePath,

                    ShowAvatar = m.UserId == _currentUser.Id
          ? Visibility.Collapsed
          : Visibility.Visible,

                    Alignment = m.UserId == _currentUser.Id
          ? HorizontalAlignment.Right
          : HorizontalAlignment.Left,

                    BubbleColor = m.UserId == _currentUser.Id
          ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD6D6"))
          : Brushes.White,

                    SenderColor = m.UserId == _currentUser.Id
          ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B6B"))
          : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#636E72")),

                 BubbleCornerRadius = m.UserId == _currentUser.Id
    ? new CornerRadius(20, 20, 5, 20)
    : new CornerRadius(20, 20, 20, 5),

SeenByText = GetSeenByText(context, m.Id, m.UserId),

                    SeenVisibility = Visibility.Collapsed,

                }).ToList();
                MessagesList.ItemsSource = uiMessages;


                if (MessagesList.Items.Count > 0)
                {
                    MessagesList.ScrollIntoView(MessagesList.Items[MessagesList.Items.Count - 1]);
                }
            }
        }
        private string GetSeenByText(AppDbContext context, int messageId, int senderId)
        {
            var seenUsers = context.ChatMessageSeens
                .Where(s => s.ChatMessageId == messageId && s.UserId != senderId)
                .Select(s => s.User.Username)
                .ToList();

            if (seenUsers.Count == 0)
                return "";

            return "Seen by " + string.Join(", ", seenUsers);
        }
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
            }
        }

        private void SendMessage()
        {
            string text = MessageTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(text))
                return;

            using (var context = new AppDbContext())
            {
                ChatMessage message = new ChatMessage
                {
                    HobbyId = _hobby.Id,
                    UserId = _currentUser.Id,
                    Message = text,
                    SentAt = DateTime.Now
                };

                context.ChatMessages.Add(message);
                context.SaveChanges();
            }

            MessageTextBox.Clear();

            LoadMessages();
        }
        private void Avatar_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse ellipse && ellipse.Tag is int userId)
            {
                using (var context = new AppDbContext())
                {
                    var user = context.Users.FirstOrDefault(u => u.Id == userId);

                    if (user == null)
                    {
                        CustomMessageBox.Show("Utilizatorul nu a fost găsit.");
                        return;
                    }

                    var profileControl = new ProfilControl(user);
                    profileControl.SetReadOnlyMode();

                    Window profileWindow = new Window
                    {
                        Width = 1150,
                        Height = 820,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize,
                        WindowStyle = WindowStyle.None,
                        AllowsTransparency = true,
                        Background = Brushes.Transparent,
                        ShowInTaskbar = false
                    };

                    Border outerBorder = new Border
                    {
                        CornerRadius = new CornerRadius(35),
                        Padding = new Thickness(10),
                        Background = new LinearGradientBrush
                        {
                            StartPoint = new Point(0, 0),
                            EndPoint = new Point(1, 1),
                            GradientStops =
                    {
                        new GradientStop((Color)ColorConverter.ConvertFromString("#FF6B6B"), 0),
                        new GradientStop((Color)ColorConverter.ConvertFromString("#FF9E5E"), 0.55),
                        new GradientStop((Color)ColorConverter.ConvertFromString("#FFD93D"), 1)
                    }
                        }
                    };

                    Border innerCard = new Border
                    {
                        Background = Brushes.White,
                        CornerRadius = new CornerRadius(28),
                        ClipToBounds = true
                    };

                    Grid root = new Grid();

                    Button closeButton = new Button
                    {
                        Content = "✕",
                        Width = 45,
                        Height = 45,
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B6B")),
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(0),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 18, 18, 0),
                        Cursor = Cursors.Hand
                    };

                    closeButton.Click += (s, ev) => profileWindow.Close();

                    root.Children.Add(profileControl);
                    root.Children.Add(closeButton);

                    innerCard.Child = root;
                    outerBorder.Child = innerCard;
                    profileWindow.Content = outerBorder;

                    profileWindow.ShowDialog();
                }
            }
        }
        private void MessageBubble_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                ChatMessageUI message = border.DataContext as ChatMessageUI;

                if (message == null)
                    return;

                if (message.SeenVisibility == Visibility.Visible)
                {
                    message.SeenVisibility = Visibility.Collapsed;
                }
                else
                {
                    message.SeenVisibility = Visibility.Visible;
                }

                MessagesList.Items.Refresh();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            // Căutăm fereastra principală (HomePage)
            var homePage = Window.GetWindow(this) as HomePage;

            if (homePage != null)
            {
                // În loc să închidem fereastra, încărcăm înapoi lista de conversații
                homePage.MainContentArea.Content = new ConversationsWindow(_currentUser);

                // Actualizăm și titlul din header pentru a fi corect
                homePage.HeaderTitle.Text = "📩 CONVERSATIONS";
            }
        }
    }
}