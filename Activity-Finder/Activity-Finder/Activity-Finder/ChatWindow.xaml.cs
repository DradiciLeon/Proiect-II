using Activity_Finder.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
namespace Activity_Finder
{
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

                var uiMessages = messages.Select(m => new
                {
                    Sender = m.User.Username,
                    Text = m.Message,
                    Time = m.SentAt.ToString("HH:mm"),

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
                        : new CornerRadius(20, 20, 20, 5)
                }).ToList();

                MessagesList.ItemsSource = uiMessages;

                if (MessagesList.Items.Count > 0)
                {
                    MessagesList.ScrollIntoView(MessagesList.Items[MessagesList.Items.Count - 1]);
                }
            }
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