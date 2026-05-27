using Activity_Finder.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
                    UserId = r.User.Id,
                    Username = r.User.Username,
                    FullName = string.IsNullOrWhiteSpace(r.User.DisplayName)
                        ? r.User.Username
                        : r.User.DisplayName,
                    HobbyName = "Vrea să participe la: " + r.Hobby.Name,
                    RequestedAt = "Trimis la " + r.RequestedAt.ToString("dd MMM yyyy HH:mm"),
                    ProfileImage = r.User.ProfileImagePath
                })
                .ToList();

            RequestsList.ItemsSource = requests;
        }

        private void OpenProfile_Click(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is JoinRequestViewModel item)
            {
                OpenUserProfile(item.UserId);
            }
        }

        private void OpenUserProfile(int userId)
        {
            using var context = new AppDbContext();

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

            closeButton.Click += (s, e) => profileWindow.Close();

            root.Children.Add(profileControl);
            root.Children.Add(closeButton);

            innerCard.Child = root;
            outerBorder.Child = innerCard;
            profileWindow.Content = outerBorder;

            profileWindow.ShowDialog();
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

            if (request == null)
                return;

            if (request.Status != "Pending")
            {
                CustomMessageBox.Show("Această cerere nu mai este disponibilă.", "Cerere expirată");
                LoadRequests();
                return;
            }

            if (request.Hobby.Date <= DateTime.Now)
            {
                CustomMessageBox.Show("Nu mai poți accepta această cerere. Activitatea s-a încheiat.", "Activitate încheiată");
                request.Status = "Expired";
                context.SaveChanges();
                LoadRequests();
                return;
            }

            if (request.Hobby.Users.Count >= request.Hobby.MaxPeople)
            {
                CustomMessageBox.Show("Nu mai poți accepta această cerere. Activitatea este deja plină.", "Activitate plină");
                LoadRequests();
                return;
            }

            request.Status = "Accepted";

            if (!request.Hobby.Users.Any(u => u.Id == request.UserId))
                request.Hobby.Users.Add(request.User);

            context.ChatMessages.Add(new ChatMessage
            {
                HobbyId = request.HobbyId,
                UserId = request.UserId,
                Message = request.User.Username + " joined the chat!",
                SentAt = DateTime.Now
            });

            context.SaveChanges();

            CustomMessageBox.Show("Cererea a fost acceptată.", "Succes");
            LoadRequests();
        }

        private void Reject_Click(object sender, RoutedEventArgs e)
        {
            var item = (JoinRequestViewModel)((FrameworkElement)sender).DataContext;
            using var context = new AppDbContext();

            var request = context.JoinRequests
                .Include(r => r.Hobby)
                .FirstOrDefault(r => r.Id == item.Id);

            if (request == null)
                return;

            if (request.Status != "Pending")
            {
                CustomMessageBox.Show("Această cerere nu mai este disponibilă.", "Cerere expirată");
                LoadRequests();
                return;
            }

            if (request.Hobby.Date <= DateTime.Now)
            {
                CustomMessageBox.Show("Nu mai poți respinge această cerere. Activitatea s-a încheiat.", "Activitate încheiată");
                request.Status = "Expired";
                context.SaveChanges();
                LoadRequests();
                return;
            }

            request.Status = "Rejected";
            context.SaveChanges();

            CustomMessageBox.Show("Cererea a fost respinsă.", "Respins");
            LoadRequests();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();

            var home = Window.GetWindow(this) as HomePage;
            home?.ShowHome_Click(null, null);
        }

        public class JoinRequestViewModel
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public string Username { get; set; }
            public string FullName { get; set; }
            public string HobbyName { get; set; }
            public string RequestedAt { get; set; }
            public string ProfileImage { get; set; }
        }
    }
}