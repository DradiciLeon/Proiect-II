using Activity_Finder.Models;
using Activity_Finder.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Activity_Finder
{
    public partial class SupportControl : UserControl
    {
        private User _currentUser;

        public SupportControl(User user)
        {
            InitializeComponent();
            _currentUser = user;

            LoadMyRequests();
        }

        private void LoadMyRequests()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    // Pasul 1: Preluăm datele brute din baza de date în memoria RAM (.ToList())
                    var messagesFromDb = context.SupportMessages
                        .Where(m => m.UserId == _currentUser.Id)
                        .OrderByDescending(m => m.SentAt)
                        .ToList();

                    // Pasul 2: Mapăm proprietățile de design în memorie, unde C#-ul poate rula ToString și Visibility
                    var myRequests = messagesFromDb.Select(m => new {
                        m.Message,
                        Date = m.SentAt.ToString("dd MMM yyyy HH:mm"),
                        StatusText = m.IsSolved ? "✅ SOLVED" : "⏳ PENDING",
                        StatusColor = m.IsSolved ? "#2ecc71" : "#f39c12",
                        m.AdminReply,
                        ReplyVisibility = !string.IsNullOrWhiteSpace(m.AdminReply) ? Visibility.Visible : Visibility.Collapsed
                    }).ToList();

                    MyRequestsList.ItemsSource = myRequests;
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Eroare la încărcarea cererilor: " + ex.Message);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this) as HomePage;
            parentWindow?.ShowHome_Click(null, null);
        }

        private void BtnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            string messageText = TxtSupportMessage.Text.Trim();

            if (string.IsNullOrWhiteSpace(messageText))
            {
                MessageBox.Show("Te rog scrie un mesaj înainte de trimitere.", "Mesaj gol", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string errorMessage;
            if (!ContentFilter.IsSafeText(messageText, 500, out errorMessage))
            {
                MessageBox.Show($"{errorMessage}", "Conținut invalid", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new AppDbContext())
                {
                    DateTime now = DateTime.Now;
                    DateTime today = DateTime.Today;

                    int messagesToday = context.SupportMessages.Count(m => m.UserId == _currentUser.Id && m.SentAt >= today);

                    if (messagesToday >= 5)
                    {
                        MessageBox.Show("Ai atins limita de 5 mesaje pe zi.", "Limită atinsă", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var lastMessage = context.SupportMessages
                        .Where(m => m.UserId == _currentUser.Id)
                        .OrderByDescending(m => m.SentAt)
                        .FirstOrDefault();

                    if (lastMessage != null)
                    {
                        var timeSinceLast = now - lastMessage.SentAt;
                        if (timeSinceLast.TotalSeconds < 30)
                        {
                            int secondsLeft = 30 - (int)timeSinceLast.TotalSeconds;
                            MessageBox.Show($"Mai așteaptă {secondsLeft} secunde.", "Prea rapid", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    var newMsg = new SupportMessage
                    {
                        UserId = _currentUser.Id,
                        Message = messageText,
                        SentAt = now,
                        IsSolved = false,
                        AdminReply = ""
                    };

                    context.SupportMessages.Add(newMsg);
                    context.SaveChanges();

                    int remaining = 5 - (messagesToday + 1);
                    MessageBox.Show($"Mesaj trimis! Mai poți trimite {remaining} mesaje azi.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                    TxtSupportMessage.Clear();
                    LoadMyRequests();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eroare la trimiterea mesajului: {ex.Message}", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}