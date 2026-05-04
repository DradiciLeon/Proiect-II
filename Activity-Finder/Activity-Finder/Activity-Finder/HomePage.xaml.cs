using Activity_Finder.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Activity_Finder
{
    public partial class HomePage : Window
    {
        private User _currentUser;

        private SolidColorBrush _activeColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B6B"));
        private SolidColorBrush _defaultColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3436"));

        public HomePage(User user)
        {
            InitializeComponent();
            _currentUser = user;
            LoadHobbyFeed();
        }

        public void LoadHobbyFeed()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var listaPostari = context.Hobbies
                        .Include(h => h.User)
                        .Where(h => h.Date > DateTime.Now) // 🔴 FILTRU
                        .OrderByDescending(h => h.CreatedAt)
                        .ToList();

                    HobbyFeedControl.ItemsSource = listaPostari;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eroare la încărcarea feed-ului: {ex.Message}");
            }
        }

        private void HobbyMap_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser != null)
            {
                MainContentArea.Content = new HobbyMapControl(_currentUser);
            }
        }

        public void ShowFilteredHome(double centerLat, double centerLng, double radiusKm)
        {
            ShowHome_Click(null, null);

            try
            {
                using (var db = new AppDbContext())
                {
                    var all = db.Hobbies
                        .Include(h => h.User)
                        .Where(h => h.Date > DateTime.Now) // 🔴 FILTRU
                        .ToList();

                    var filtered = all.Where(h =>
                        h.Latitude != 0 &&
                        GetDistanceKm(centerLat, centerLng, h.Latitude, h.Longitude) <= radiusKm
                    ).OrderByDescending(h => h.CreatedAt).ToList();

                    HobbyFeedControl.ItemsSource = filtered;

                    if (filtered.Count == 0)
                        MessageBox.Show("Nu am găsit niciun hobby în această rază.");
                    else
                        MessageBox.Show($"Am găsit {filtered.Count} hobby-uri în zona selectată!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la filtrare: " + ex.Message);
            }
        }

        private double GetDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371d;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * (2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
        }

        private void BtnNavSupport_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new SupportControl(_currentUser);
            HeaderTitle.Text = "🎧 HELP & SUPPORT";
            SearchArea.Visibility = Visibility.Collapsed;
            ResetNavStyles();
            BtnNavSupport.Foreground = _activeColor;
            AnimateTransition();
        }

        private void AnimateTransition()
        {
            DoubleAnimation fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400)
            };
            MainContentArea.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void ResetNavStyles()
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3436"));
            BtnNavHome.Foreground = brush;
            BtnNavSettings.Foreground = brush;
            BtnNavProfile.Foreground = brush;
            BtnNavSupport.Foreground = brush;
        }

        public void ShowHome_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = DashboardView;
            HeaderTitle.Text = "👑 HOBBY APP";

            LoadHobbyFeed();

            SearchArea.Visibility = Visibility.Visible;
            ResetNavStyles();
            BtnNavHome.Foreground = _activeColor;
            AnimateTransition();
        }

        private void ShowSettings_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new SettingsControl(_currentUser);
            HeaderTitle.Text = "👑 HOBBY SETTINGS";
            SearchArea.Visibility = Visibility.Collapsed;
            ResetNavStyles();
            BtnNavSettings.Foreground = _activeColor;
            AnimateTransition();
        }

        private void BtnNavProfile_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new ProfilControl(_currentUser);
            HeaderTitle.Text = "👤 MY PROFILE";

            SearchArea.Visibility = Visibility.Collapsed;

            ResetNavStyles();
            BtnNavProfile.Foreground = _activeColor;
            AnimateTransition();
        }

        private void LoadHobbies()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var hobbies = context.Hobbies
                        .Where(h => h.Date > DateTime.Now) // 🔴 FILTRU
                        .OrderByDescending(h => h.Date)
                        .ToList();

                    HobbyFeedControl.ItemsSource = hobbies;
                }
            }
            catch (Exception ex)
            {
                Activity_Finder.Services.AppLogger.Log(ex);

                MessageBox.Show("Nu s-au putut încărca hobby-urile.",
                                "Eroare",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void PostHobby_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new PostHobby(_currentUser.Id);
            HeaderTitle.Text = "🚀 POST A HOBBY";
            SearchArea.Visibility = Visibility.Collapsed;
            ResetNavStyles();
            AnimateTransition();
        }

        private void Categories_Click(object sender, RoutedEventArgs e)
        {
            ExploreCateg explore = new ExploreCateg();
            explore.Show();
        }

        private void MainSearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                string searchText = MainSearchBox.Text?.Trim().ToLower() ?? string.Empty;

                using (var context = new AppDbContext())
                {
                    var query = context.Hobbies
                        .Include(h => h.User)
                        .Where(h => h.Date > DateTime.Now) // 🔴 FILTRU
                        .AsQueryable();

                    var hobbies = query.ToList();

                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        hobbies = hobbies.Where(h =>
                            ContainsSafe(h.Name, searchText) ||
                            ContainsSafe(h.Category, searchText) ||
                            ContainsSafe(h.City, searchText)
                        ).ToList();
                    }

                    HobbyFeedControl.ItemsSource = hobbies;
                }
            }
            catch (Exception ex)
            {
                Activity_Finder.Services.AppLogger.Log(ex);
                MessageBox.Show("Nu s-a putut efectua căutarea.");
            }
        }

        private bool ContainsSafe(string? value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.ToLower().Contains(search);
        }

        private int LevenshteinDistance(string a, string b)
        {
            int[,] dp = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++)
                dp[i, 0] = i;

            for (int j = 0; j <= b.Length; j++)
                dp[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;

                    dp[i, j] = Math.Min(
                        Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                        dp[i - 1, j - 1] + cost);
                }
            }

            return dp[a.Length, b.Length];
        }
        private void HobbyCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is Hobby hobby)
            {
                using (var context = new AppDbContext())
                {
                    var hobbyDb = context.Hobbies
                        .Include(h => h.User)
                        .Include(h => h.Users)
                        .FirstOrDefault(h => h.Id == hobby.Id);

                    if (hobbyDb == null)
                    {
                        MessageBox.Show("Activitatea nu a fost găsită.\n\nActivity not found.");
                        return;
                    }

                    string organizerName = hobbyDb.User != null
                        ? hobbyDb.User.Username
                        : "Unknown";

                    int locuriRamase = hobbyDb.MaxPeople - hobbyDb.Users.Count;

                    MessageBoxResult result = MessageBox.Show(
                        $"Activitate: {hobbyDb.Name}\n" +
                        $"Categorie: {hobbyDb.Category}\n" +
                        $"Locație: {hobbyDb.City}\n" +
                        $"Organizator: {organizerName}\n" +
                        $"Locuri rămase: {locuriRamase}\n\n" +
                        $"YES = Join\nVIEW_PROFILE = Vezi profil organizator\nCANCEL = Închide",
                        "Detalii activitate / Activity details",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Information
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        JoinSelectedHobby(hobbyDb.Id);
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        OpenOrganizerProfile(hobbyDb.UserId);
                    }
                }
            }
        }

        private void JoinSelectedHobby(int hobbyId)
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var hobby = context.Hobbies
                        .Include(h => h.Users)
                        .FirstOrDefault(h => h.Id == hobbyId);

                    var user = context.Users.FirstOrDefault(u => u.Id == _currentUser.Id);

                    if (hobby == null || user == null)
                    {
                        MessageBox.Show("Eroare la încărcarea datelor.\n\nError loading data.");
                        return;
                    }

                    if (hobby.UserId == _currentUser.Id)
                    {
                        MessageBox.Show(
                            "Nu poți da join la propria activitate.\n\nYou cannot join your own activity.",
                            "Join blocat / Join blocked",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    if (hobby.Date <= DateTime.Now)
                    {
                        MessageBox.Show(
                            "Activitatea s-a încheiat.\n\nThe activity has ended.",
                            "Activitate încheiată / Activity ended",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    if (hobby.Users.Any(u => u.Id == _currentUser.Id))
                    {
                        MessageBox.Show(
                            "Ești deja înscris la această activitate.\n\nYou already joined this activity.",
                            "Deja înscris / Already joined",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                        return;
                    }

                    if (hobby.Users.Count >= hobby.MaxPeople)
                    {
                        MessageBox.Show(
                            "Activitatea este plină.\n\nThe activity is full.",
                            "Activitate plină / Full activity",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    hobby.Users.Add(user);
                    context.SaveChanges();

                    MessageBox.Show(
                        "Te-ai înscris cu succes la activitate!\n\nYou joined the activity successfully.",
                        "Succes / Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    LoadHobbyFeed();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la join: " + ex.Message);
            }
        }


        private void OpenOrganizerProfile(int? organizerId)
        {
            if (organizerId == null)
            {
                MessageBox.Show("Organizatorul nu a fost găsit.\n\nOrganizer not found.");
                return;
            }

            using (var context = new AppDbContext())
            {
                var organizer = context.Users.FirstOrDefault(u => u.Id == organizerId.Value);

                if (organizer == null)
                {
                    MessageBox.Show("Profilul organizatorului nu a fost găsit.\n\nOrganizer profile not found.");
                    return;
                }

                Window profileWindow = new Window
                {
                    Title = "Profil organizator / Organizer profile",
                    Width = 420,
                    Height = 650,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = new ProfilControl(organizer)
                };

                profileWindow.Show();
            }
        }
    }
}