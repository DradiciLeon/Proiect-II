using Activity_Finder.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Activity_Finder
{
    public partial class JoinRequestsWindow : UserControl
    {
        private readonly User _currentUser;
        private readonly DispatcherTimer _timer;

        public JoinRequestsWindow(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            LoadRequests();

            // Timer pentru a verifica cereri noi la fiecare 3 secunde
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(3);
            _timer.Tick += (s, e) => LoadRequests();
            _timer.Start();
        }

        private void LoadRequests()
        {
            using var context = new AppDbContext();

            var requests = context.JoinRequests
                .Include(r => r.User)
                .Include(r => r.Hobby)
                .Where(r =>
                    r.Hobby.UserId == _currentUser.Id &&
                    r.Status == "Pending")
                .Select(r => new JoinRequestViewModel
                {
                    Id = r.Id,
                    Username = r.User.Username,
                    FullName = string.IsNullOrWhiteSpace(r.User.DisplayName)
                        ? r.User.Username
                        : r.User.DisplayName,
                    HobbyName = "Vrea să participe la: " + r.Hobby.Name,
                    RequestedAt = "Trimis la " + r.RequestedAt.ToString("dd MMM yyyy HH:mm"),
                    // Trimitem calea brută, converter-ul se ocupă de restul
                    ProfileImage = r.User.ProfileImagePath
                })
                .ToList();

            RequestsList.ItemsSource = requests;
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            var item = (JoinRequestViewModel)((FrameworkElement)sender).DataContext;
            using var context = new AppDbContext();

            var request = context.JoinRequests
                .Include(r => r.User)
                .Include(r => r.Hobby)
                .ThenInclude(h => h.Users)
                .FirstOrDefault(r => r.Id == item.Id);

            if (request == null) return;

            request.Status = "Accepted";

            if (!request.Hobby.Users.Any(u => u.Id == request.UserId))
            {
                request.Hobby.Users.Add(request.User);
            }

            // Notificare automată în chat
            context.ChatMessages.Add(new ChatMessage
            {
                HobbyId = request.HobbyId,
                UserId = request.UserId,
                Message = request.User.Username + " joined the chat!",
                SentAt = DateTime.Now
            });

            context.SaveChanges();
            MessageBox.Show("Cererea a fost acceptată.", "Succes");
            LoadRequests();
        }

        private void Reject_Click(object sender, RoutedEventArgs e)
        {
            var item = (JoinRequestViewModel)((FrameworkElement)sender).DataContext;
            using var context = new AppDbContext();

            var request = context.JoinRequests.FirstOrDefault(r => r.Id == item.Id);
            if (request == null) return;

            request.Status = "Rejected";
            context.SaveChanges();

            MessageBox.Show("Cererea a fost respinsă.", "Respins");
            LoadRequests();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop(); // Oprim timer-ul când părăsim pagina
            var home = Window.GetWindow(this) as HomePage;
            home?.ShowHome_Click(null, null);
        }

        public class JoinRequestViewModel
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public string FullName { get; set; }
            public string HobbyName { get; set; }
            public string RequestedAt { get; set; }
            public string ProfileImage { get; set; }
        }
    }
}