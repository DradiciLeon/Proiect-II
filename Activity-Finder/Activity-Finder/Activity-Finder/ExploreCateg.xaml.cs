using Activity_Finder.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Activity_Finder
{
    public partial class ExploreCateg : UserControl
    {
        private readonly User _currentUser;

        public ExploreCateg(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
        }

        private void Category_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string category)
            {
                SelectedCategoryTitle.Text = category.ToUpper();

                using (var context = new AppDbContext())
                {
                    var activities = context.Hobbies
                        .Include(h => h.User)
                        .Include(h => h.Users)
                        .Where(h =>
                            h.Category.ToLower().Contains(category.ToLower()) &&
                            h.Date > DateTime.Now)
                        .OrderBy(h => h.Date)
                        .ToList();

                    ActivitiesList.ItemsSource = activities;

                    NoActivitiesText.Visibility = activities.Count == 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }

                CategoriesView.Visibility = Visibility.Collapsed;
                ActivitiesView.Visibility = Visibility.Visible;
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            ActivitiesView.Visibility = Visibility.Collapsed;
            CategoriesView.Visibility = Visibility.Visible;
        }

        private void ActivityCard_Click(object sender, MouseButtonEventArgs e)
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
                        Cursor = Cursors.Hand
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
                        Cursor = Cursors.Hand,
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
                        Cursor = Cursors.Hand
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

                CustomMessageBox.Show("Cererea ta a fost trimisă organizatorului.", "Cerere trimisă");
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
                    Cursor = Cursors.Hand
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
    }
}