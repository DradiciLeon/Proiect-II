using Activity_Finder.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Activity_Finder
{
    public partial class HomePage : Window
    {
        private User _currentUser;

        private int _lastRequestsCount = 0;
        private DispatcherTimer _notificationTimer;
        private DispatcherTimer _chatTimer;
        private DateTime _lastChatCheck = DateTime.MinValue;

        private SolidColorBrush _activeColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B6B"));

        public HomePage(User user)
        {
            InitializeComponent();

            _currentUser = user;

            LoadHobbyFeed();

            StartJoinRequestNotifications();
            LoadRequestsBadge();

            LoadChatBadge();
            StartChatNotifications();

            ShowChatButton(true);
        }

        private void ShowChatButton(bool show)
        {
            BtnChat.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void StartChatNotifications()
        {
            _chatTimer = new DispatcherTimer();
            _chatTimer.Interval = TimeSpan.FromSeconds(3);
            _chatTimer.Tick += (s, e) => LoadChatBadge();
            _chatTimer.Start();
        }
        private void LoadChatBadge()
        {
            BtnChat.ApplyTemplate();

            var chatBadge = BtnChat.Template.FindName("ChatBadge", BtnChat) as Border;
            var chatBadgeText = BtnChat.Template.FindName("ChatBadgeText", BtnChat) as TextBlock;

            if (chatBadge == null || chatBadgeText == null)
                return;

            using (var context = new AppDbContext())
            {
                var myHobbyIds = context.Hobbies
                    .Where(h =>
                        h.UserId == _currentUser.Id ||
                        h.Users.Any(u => u.Id == _currentUser.Id))
                    .Select(h => h.Id)
                    .ToList();

                int unread = context.ChatMessages.Count(m =>
                    myHobbyIds.Contains(m.HobbyId) &&
                    m.UserId != _currentUser.Id &&
                    m.SentAt > _lastChatCheck);

                if (unread > 0)
                {
                    chatBadge.Visibility = Visibility.Visible;
                    chatBadgeText.Text = unread.ToString();
                }
                else
                {
                    chatBadge.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void LoadRequestsBadge()
        {
            using (var context = new AppDbContext())
            {
                int count = context.JoinRequests.Count(r =>
                    r.Hobby.UserId == _currentUser.Id &&
                    r.Status == "Pending");

                if (count > 0)
                {
                    RequestsBadge.Visibility = Visibility.Visible;
                    RequestsBadgeText.Text = count.ToString();
                }
                else
                {
                    RequestsBadge.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void StartJoinRequestNotifications()
        {
            using (var context = new AppDbContext())
            {
                _lastRequestsCount = context.JoinRequests.Count(r =>
                    r.Hobby.UserId == _currentUser.Id &&
                    r.Status == "Pending");
            }

            _notificationTimer = new DispatcherTimer();
            _notificationTimer.Interval = TimeSpan.FromSeconds(4);
            _notificationTimer.Tick += (s, e) => CheckForNewJoinRequests();
            _notificationTimer.Start();
        }

        private void CheckForNewJoinRequests()
        {
            using (var context = new AppDbContext())
            {
                int currentCount = context.JoinRequests.Count(r =>
                    r.Hobby.UserId == _currentUser.Id &&
                    r.Status == "Pending");

                if (currentCount > _lastRequestsCount)
                {
                    CustomMessageBox.Show(
                        "Ai primit o nouă cerere de join!",
                        "🔔 Join Request"
                    );
                }

                _lastRequestsCount = currentCount;
            }

            LoadRequestsBadge();
        }

        public void LoadHobbyFeed()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var listaPostari = context.Hobbies
                        .Include(h => h.User)
                        .Where(h => h.Date > DateTime.Now)
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
                ShowChatButton(true);
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
                        .Where(h => h.Date > DateTime.Now)
                        .ToList();

                    var filtered = all.Where(h =>
                        h.Latitude != 0 &&
                        GetDistanceKm(centerLat, centerLng, h.Latitude, h.Longitude) <= radiusKm
                    ).OrderByDescending(h => h.CreatedAt).ToList();

                    HobbyFeedControl.ItemsSource = filtered;

                    if (filtered.Count == 0)
                        CustomMessageBox.Show("Nu am găsit niciun hobby în această rază.");
                    else
                        CustomMessageBox.Show($"Am găsit {filtered.Count} hobby-uri în zona selectată!");
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Eroare la filtrare: " + ex.Message);
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
            BtnNavRequests.Foreground = brush;
        }

        public void ShowHome_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = DashboardView;
            HeaderTitle.Text = "👑 HOBBY APP";

            LoadHobbyFeed();

            SearchArea.Visibility = Visibility.Visible;
            ShowChatButton(true);

            ResetNavStyles();
            BtnNavHome.Foreground = _activeColor;

            AnimateTransition();
        }

        private void ShowSettings_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new SettingsControl(_currentUser);
            HeaderTitle.Text = "👑 HOBBY SETTINGS";

            SearchArea.Visibility = Visibility.Collapsed;
            ShowChatButton(true);

            ResetNavStyles();
            BtnNavSettings.Foreground = _activeColor;

            AnimateTransition();
        }

        private void BtnNavProfile_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new ProfilControl(_currentUser);
            HeaderTitle.Text = "👤 MY PROFILE";

            SearchArea.Visibility = Visibility.Collapsed;
            ShowChatButton(true);

            ResetNavStyles();
            BtnNavProfile.Foreground = _activeColor;

            AnimateTransition();
        }

        private void BtnNavSupport_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new SupportControl(_currentUser);
            HeaderTitle.Text = "🎧 HELP & SUPPORT";

            SearchArea.Visibility = Visibility.Collapsed;
            ShowChatButton(true);

            ResetNavStyles();
            BtnNavSupport.Foreground = _activeColor;

            AnimateTransition();
        }

        private void PostHobby_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new PostHobby(_currentUser.Id);
            HeaderTitle.Text = "🚀 POST A HOBBY";

            SearchArea.Visibility = Visibility.Collapsed;
            ShowChatButton(true);

            ResetNavStyles();

            AnimateTransition();
        }

        private void Categories_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new ExploreCateg(_currentUser);
            HeaderTitle.Text = "EXPLORE CATEGORIES";
            SearchArea.Visibility = Visibility.Collapsed;
            AnimateTransition();
        }

        private void MainSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string searchText = MainSearchBox.Text?.Trim().ToLower() ?? string.Empty;

                using (var context = new AppDbContext())
                {
                    var hobbies = context.Hobbies
                        .Include(h => h.User)
                        .Where(h => h.Date > DateTime.Now)
                        .ToList();

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
            catch
            {
                CustomMessageBox.Show("Nu s-a putut efectua căutarea.");
            }
        }

        private bool ContainsSafe(string? value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.ToLower().Contains(search);
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
                        CustomMessageBox.Show("Activitatea nu a fost găsită.");
                        return;
                    }

                    string organizerName = hobbyDb.User != null ? hobbyDb.User.Username : "Unknown";
                    int locuriRamase = hobbyDb.MaxPeople - hobbyDb.Users.Count;

                    Window detailsWindow = new Window
                    {
                        Width = 460,
                        Height = 540,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize,
                        WindowStyle = WindowStyle.None,
                        AllowsTransparency = true,
                        Background = Brushes.Transparent
                    };

                    Border outerBorder = new Border
                    {
                        CornerRadius = new CornerRadius(30),
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

                    Border card = new Border
                    {
                        Background = Brushes.White,
                        CornerRadius = new CornerRadius(26),
                        Padding = new Thickness(26)
                    };

                    Grid grid = new Grid();

                    Button closeButton = new Button
                    {
                        Content = "✕",
                        Width = 42,
                        Height = 42,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B6B")),
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(0),
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

                    closeButton.Click += (s, ev) => detailsWindow.Close();

                    StackPanel panel = new StackPanel();

                    TextBlock title = new TextBlock
                    {
                        Text = hobbyDb.Name,
                        FontSize = 30,
                        FontWeight = FontWeights.Black,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B6B")),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 20, 0, 4)
                    };

                    TextBlock subtitle = new TextBlock
                    {
                        Text = "Activity details",
                        FontSize = 14,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#636E72")),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 24)
                    };

                    Border infoBox = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF7F4")),
                        CornerRadius = new CornerRadius(22),
                        Padding = new Thickness(20),
                        Margin = new Thickness(0, 0, 0, 18)
                    };

                    TextBlock info = new TextBlock
                    {
                        Text =
                            $"🎯  Categorie: {hobbyDb.Category}\n\n" +
                            $"👤  Organizator: {organizerName}\n\n" +
                            $"📍  Locație: {hobbyDb.City}\n\n" +
                            $"📅  Data: {hobbyDb.Date:dd MMM yyyy HH:mm}\n\n" +
                            $"👥  Participanți: {hobbyDb.Users.Count}/{hobbyDb.MaxPeople}\n\n" +
                            $"✅  Locuri rămase: {locuriRamase}",
                        FontSize = 15,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3436")),
                        TextWrapping = TextWrapping.Wrap
                    };

                    infoBox.Child = info;

                    StackPanel buttons = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 12, 0, 0)
                    };

                    Button joinButton = new Button
                    {
                        Content = "Join",
                        Width = 110,
                        Height = 42,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B6B")),
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(0),
                        FontWeight = FontWeights.Bold,
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Margin = new Thickness(0, 0, 10, 0)
                    };

                    Button profileButton = new Button
                    {
                        Content = "View Profile",
                        Width = 130,
                        Height = 42,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F2F6")),
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3436")),
                        BorderThickness = new Thickness(0),
                        FontWeight = FontWeights.Bold,
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

                    joinButton.Click += (s, ev) =>
                    {
                        detailsWindow.Close();
                        JoinSelectedHobby(hobbyDb.Id);
                    };

                    profileButton.Click += (s, ev) =>
                    {
                        detailsWindow.Close();
                        OpenOrganizerProfile(hobbyDb.UserId);
                    };

                    buttons.Children.Add(joinButton);
                    buttons.Children.Add(profileButton);

                    panel.Children.Add(title);
                    panel.Children.Add(subtitle);
                    panel.Children.Add(infoBox);
                    panel.Children.Add(buttons);

                    grid.Children.Add(panel);
                    grid.Children.Add(closeButton);

                    card.Child = grid;
                    outerBorder.Child = card;
                    detailsWindow.Content = outerBorder;

                    detailsWindow.ShowDialog();
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
                        CustomMessageBox.Show("Eroare la încărcarea datelor.");
                        return;
                    }

                    if (hobby.UserId == _currentUser.Id)
                    {
                        CustomMessageBox.Show("Nu poți trimite cerere la propria activitate.", "Cerere blocată");
                        return;
                    }

                    if (hobby.Date <= DateTime.Now)
                    {
                        CustomMessageBox.Show("Activitatea s-a încheiat.", "Activitate încheiată");
                        return;
                    }

                    if (hobby.Users.Any(u => u.Id == _currentUser.Id))
                    {
                        CustomMessageBox.Show("Ești deja acceptat la această activitate.", "Deja acceptat");
                        return;
                    }

                    if (hobby.Users.Count >= hobby.MaxPeople)
                    {
                        CustomMessageBox.Show("Activitatea este plină.", "Activitate plină");
                        return;
                    }

                    bool alreadyPending = context.JoinRequests.Any(r =>
                        r.HobbyId == hobby.Id &&
                        r.UserId == _currentUser.Id &&
                        r.Status == "Pending");

                    if (alreadyPending)
                    {
                        CustomMessageBox.Show("Ai trimis deja o cerere pentru această activitate.", "Cerere existentă");
                        return;
                    }

                    var rejectedRequest = context.JoinRequests.FirstOrDefault(r =>
                        r.HobbyId == hobby.Id &&
                        r.UserId == _currentUser.Id &&
                        r.Status == "Rejected");

                    if (rejectedRequest != null)
                    {
                        rejectedRequest.Status = "Pending";
                        rejectedRequest.RequestedAt = DateTime.Now;
                    }
                    else
                    {
                        context.JoinRequests.Add(new JoinRequest
                        {
                            HobbyId = hobby.Id,
                            UserId = _currentUser.Id,
                            Status = "Pending",
                            RequestedAt = DateTime.Now
                        });
                    }

                    context.SaveChanges();

                    CustomMessageBox.Show(
                        "Cererea ta a fost trimisă organizatorului.",
                        "Cerere trimisă"
                    );

                    LoadHobbyFeed();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Eroare la trimiterea cererii: " + ex.Message);
            }
        }

        private void OpenOrganizerProfile(int? organizerId)
        {
            if (organizerId == null)
            {
                CustomMessageBox.Show("Organizatorul nu a fost găsit.");
                return;
            }

            using (var context = new AppDbContext())
            {
                var organizer = context.Users.FirstOrDefault(u => u.Id == organizerId.Value);

                if (organizer == null)
                {
                    CustomMessageBox.Show("Profilul organizatorului nu a fost găsit.");
                    return;
                }

                var profileControl = new ProfilControl(organizer);
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
                    Owner = this,
                    ShowInTaskbar = false,
                    Opacity = 0
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
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                closeButton.Click += (s, ev) => profileWindow.Close();

                root.Children.Add(profileControl);
                root.Children.Add(closeButton);

                innerCard.Child = root;
                outerBorder.Child = innerCard;
                profileWindow.Content = outerBorder;

                DoubleAnimation fade = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(250)
                };

                profileWindow.BeginAnimation(Window.OpacityProperty, fade);
                profileWindow.ShowDialog();
            }
        }

        private void Requests_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new JoinRequestsWindow(_currentUser);
            HeaderTitle.Text = "🔔 JOIN REQUESTS";

            SearchArea.Visibility = Visibility.Collapsed;
            ShowChatButton(true);

            ResetNavStyles();
            BtnNavRequests.Foreground = _activeColor;

            LoadRequestsBadge();

            AnimateTransition();
        }

        private void OpenConversations_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new ConversationsWindow(_currentUser);
            HeaderTitle.Text = "💬 CHATS";

            SearchArea.Visibility = Visibility.Collapsed;
            ShowChatButton(false);

            _lastChatCheck = DateTime.Now;
            LoadChatBadge();

            AnimateTransition();
        }

        private void OpenChat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var acceptedHobbies = context.Hobbies
                        .Include(h => h.User)
                        .Include(h => h.Users)
                        .Where(h =>
                            h.UserId == _currentUser.Id ||
                            h.Users.Any(u => u.Id == _currentUser.Id))
                        .ToList();

                    if (acceptedHobbies.Count == 0)
                    {
                        CustomMessageBox.Show(
                            "Nu ai niciun chat disponibil.\n\nTrebuie să fii acceptat la o activitate ca să poți vorbi în chat.",
                            "Chat indisponibil"
                        );
                        return;
                    }

                    if (acceptedHobbies.Count == 1)
                    {
                        MainContentArea.Content = new ChatWindow(acceptedHobbies[0], _currentUser);
                        HeaderTitle.Text = "💬 CHAT";

                        SearchArea.Visibility = Visibility.Collapsed;
                        ShowChatButton(false);

                        _lastChatCheck = DateTime.Now;
                        LoadChatBadge();

                        AnimateTransition();
                        return;
                    }

                    CustomMessageBox.Show(
                        "Ai mai multe activități acceptate. Deschide chat-ul din lista de conversații.",
                        "Alege activitatea"
                    );
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Eroare la deschiderea chat-ului: " + ex.Message);
            }
        }

      
    }
}