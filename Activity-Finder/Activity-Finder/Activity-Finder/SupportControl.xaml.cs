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
                MessageBox.Show(
                    "Te rog scrie un mesaj înainte de trimitere.\n\nPlease type a message before sending.",
                    "Mesaj gol / Empty Message",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            string errorMessage;
            if (!ContentFilter.IsSafeText(messageText, 500, out errorMessage))
            {
                MessageBox.Show(
                    $"{errorMessage}\n\nThe message contains inappropriate or unsafe content.",
                    "Conținut invalid / Invalid content",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            try
            {
                using (var context = new AppDbContext())
                {
                    DateTime now = DateTime.Now;
                    DateTime today = DateTime.Today;

                    int messagesToday = context.SupportMessages
                        .Count(m => m.UserId == _currentUser.Id &&
                                    m.SentAt >= today);

                    if (messagesToday >= 5)
                    {
                        MessageBox.Show(
                            "Ai atins limita de 5 mesaje pe zi.\n\nYou have reached the limit of 5 messages per day.",
                            "Limită mesaje / Message limit",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
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

                            MessageBox.Show(
                                $"Mai așteaptă {secondsLeft} secunde.\n\nPlease wait {secondsLeft} seconds.",
                                "Prea rapid / Too fast",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning
                            );
                            return;
                        }
                    }

                    var newMsg = new SupportMessage
                    {
                        UserId = _currentUser.Id,
                        Message = messageText,
                        SentAt = now
                    };

                    context.SupportMessages.Add(newMsg);
                    context.SaveChanges();

                    int remaining = 5 - (messagesToday + 1);

                    MessageBox.Show(
                        $"Mesaj trimis! Mai poți trimite {remaining} mesaje azi.\n\nMessage sent! You can send {remaining} more today.",
                        "Succes / Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    TxtSupportMessage.Clear();
                    BtnBack_Click(null, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Eroare la trimiterea mesajului: {ex.Message}\n\nError sending message: {ex.Message}",
                    "Eroare / Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}