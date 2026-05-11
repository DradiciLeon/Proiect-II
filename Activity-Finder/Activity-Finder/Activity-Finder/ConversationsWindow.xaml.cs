using Activity_Finder.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls;

namespace Activity_Finder
{
    public partial class ConversationsWindow : UserControl
    {
        private readonly User _currentUser;

        public ConversationsWindow(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            LoadConversations();
        }
        private void LoadConversations()
        {
            using var context = new AppDbContext();

            var conversations = context.Hobbies
                .Include(h => h.User)
                .Include(h => h.Users)
                .Where(h =>

                    // participi la activitate
                    h.Users.Any(u => u.Id == _currentUser.Id)

                    ||

                    // SAU e activitatea ta si exista mesaje de la altii
                    (
                        h.UserId == _currentUser.Id &&
                        context.ChatMessages.Any(m =>
                            m.HobbyId == h.Id &&
                            m.UserId != _currentUser.Id)
                    )
                )
                .Select(h => new ConversationViewModel
                {
                    HobbyId = h.Id,
                    HobbyName = h.Name,

                    OtherPerson = h.UserId == _currentUser.Id
                        ? "Participanții activității tale"
                        : "Organizator: " + h.User.Username,

                    LastMessage = context.ChatMessages
                        .Where(m => m.HobbyId == h.Id)
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => m.Message)
                        .FirstOrDefault() ?? "Nu există mesaje încă."
                })
                .ToList();

            ConversationsList.ItemsSource = conversations;
        }

        private void ConversationsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ConversationsList.SelectedItem is ConversationViewModel conversation)
            {
                using var context = new AppDbContext();

                var hobby = context.Hobbies
                    .FirstOrDefault(h => h.Id == conversation.HobbyId);

                if (hobby == null)
                    return;

                Window parentWindow = Window.GetWindow(this);

                if (parentWindow is HomePage home)
                {
                    home.MainContentArea.Content =
                        new ChatWindow( hobby,_currentUser);

                    home.HeaderTitle.Text = "💬 CHAT";
                }
            }
        }
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);

            if (parentWindow is HomePage home)
            {
                home.ShowHome_Click(null, null);
            }
        }
    }

    public class ConversationViewModel
    {
        public int HobbyId { get; set; }
        public string HobbyName { get; set; }
        public string OtherPerson { get; set; }
        public string LastMessage { get; set; }
    }
}