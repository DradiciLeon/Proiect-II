using Activity_Finder.Models;
using Activity_Finder.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Activity_Finder
{
    public partial class ProfilControl : UserControl
    {
        private User _currentUser;

        private string _googleApiKey = "AIzaSyDJQgSxw7taAsc23FuHBvuf-9Zle8y2jss";
        private DispatcherTimer _typingTimer;
        private bool _isSelectingFromList = false;
        private bool _locationSelectedFromSuggestions = false;
        private bool _isReadOnly = false;
        public ProfilControl(User user)
        {
            InitializeComponent();
            _currentUser = user;

            _typingTimer = new DispatcherTimer();
            _typingTimer.Interval = TimeSpan.FromMilliseconds(500);
            _typingTimer.Tick += TypingTimer_Tick;

            LoadUserData();
            LoadActivities();
        }
        public void SetReadOnlyMode()
        {
            _isReadOnly = true;

            DisplayNameInput.IsReadOnly = true;
            BioInput.IsReadOnly = true;
            LocationInput.IsReadOnly = true;

            UploadPhotoButton.IsEnabled = false;
            UploadPhotoButton.Cursor = null;

            SaveProfileButton.Visibility = Visibility.Collapsed;
            BtnAddHobby.Visibility = Visibility.Collapsed;
        }

        private void LoadActivities()
        {
            using (var context = new AppDbContext())
            {
                var myActivities = context.Hobbies
                    .Where(h => h.UserId == _currentUser.Id)
                    .OrderByDescending(h => h.Date)
                    .ToList();

                MyActivitiesList.ItemsSource = myActivities;

                var joinedActivities = context.Hobbies
                    .Where(h => h.Users.Any(u => u.Id == _currentUser.Id))
                    .OrderByDescending(h => h.Date)
                    .ToList();

                JoinedActivitiesList.ItemsSource = joinedActivities;
            }
        }

        private void LoadUserData()
        {
            using (var context = new AppDbContext())
            {
                var freshUser = context.Users.Find(_currentUser.Id);
                if (freshUser != null)
                {
                    _currentUser.DisplayName = freshUser.DisplayName;
                    _currentUser.Bio = freshUser.Bio;
                    _currentUser.Location = freshUser.Location;
                    _currentUser.ProfileImagePath = freshUser.ProfileImagePath;
                }

                // Calculăm Rating-ul Mediu
                var ratings = context.Ratings.Where(r => r.ToUserId == _currentUser.Id).ToList();
                if (ratings.Any())
                {
                    double average = ratings.Average(r => r.Stars);
                    LblRatingValue.Text = average.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                    LblRatingCount.Text = $" ({ratings.Count} feedback-uri)";
                }
                else
                {
                    LblRatingValue.Text = "N/A";
                    LblRatingCount.Text = " (fără feedback)";
                }
            }

            // Update UI Fields
            DisplayNameInput.Text = !string.IsNullOrEmpty(_currentUser.DisplayName) ? _currentUser.DisplayName : _currentUser.Username;
            BioInput.Text = _currentUser.Bio;
            LocationInput.Text = _currentUser.Location;

            if (!string.IsNullOrEmpty(_currentUser.ProfileImagePath) && System.IO.File.Exists(_currentUser.ProfileImagePath))
            {
                SetProfileImage(_currentUser.ProfileImagePath);
            }

            RefreshInterests();
        }

        private void LocationInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSelectingFromList) return;

            string input = LocationInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(input) || input == _currentUser.Location)
            {
                SuggestionsBorder.Visibility = Visibility.Collapsed;
                return;
            }

            _locationSelectedFromSuggestions = false;

            _typingTimer.Stop();
            _typingTimer.Start();
        }

        private async void TypingTimer_Tick(object sender, EventArgs e)
        {
            _typingTimer.Stop();

            string input = LocationInput.Text.Trim();

            if (input.Length < 3 || input == _currentUser.Location)
            {
                SuggestionsBorder.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"https://maps.googleapis.com/maps/api/place/autocomplete/json?input={Uri.EscapeDataString(input)}&key={_googleApiKey}";
                    string response = await client.GetStringAsync(url);

                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        var status = doc.RootElement.GetProperty("status").GetString();

                        if (status == "OK")
                        {
                            var predictions = doc.RootElement.GetProperty("predictions");
                            var suggestions = new List<string>();

                            foreach (var pred in predictions.EnumerateArray())
                            {
                                var description = pred.GetProperty("description").GetString();
                                if (!string.IsNullOrWhiteSpace(description))
                                    suggestions.Add(description);
                            }

                            if (suggestions.Count > 0)
                            {
                                LocationSuggestionsBox.ItemsSource = suggestions;
                                SuggestionsBorder.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                SuggestionsBorder.Visibility = Visibility.Collapsed;
                            }
                        }
                        else
                        {
                            SuggestionsBorder.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
            catch
            {
                SuggestionsBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void LocationSuggestionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LocationSuggestionsBox.SelectedItem is string selectedAddress)
            {
                _isSelectingFromList = true;

                LocationInput.Text = selectedAddress;
                SuggestionsBorder.Visibility = Visibility.Collapsed;

                _locationSelectedFromSuggestions = true;
                _currentUser.Location = selectedAddress;

                LocationSuggestionsBox.SelectedItem = null;

                _isSelectingFromList = false;
            }
        }

        private void LocationInput_GotFocus(object sender, RoutedEventArgs e)
        {
            SuggestionsBorder.Visibility = Visibility.Collapsed;
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string displayName = DisplayNameInput.Text.Trim();
                string bio = BioInput.Text.Trim();
                string location = LocationInput.Text.Trim();
                string errorMessage;

                if (!ContentFilter.IsSafeText(displayName, 30, out errorMessage))
                {
                    MessageBox.Show(errorMessage, "Display name invalid", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!ContentFilter.IsSafeText(bio, 250, out errorMessage))
                {
                    MessageBox.Show(errorMessage, "Bio invalid", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!ContentFilter.IsSafeText(location, 100, out errorMessage))
                {
                    MessageBox.Show(errorMessage, "Locație invalidă", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!_locationSelectedFromSuggestions)
                {
                    CustomMessageBox.Show(
                        "Te rog selectează locația din lista de sugestii.",
                        "Locație invalidă"
                    );
                    return;
                }

                using (var context = new AppDbContext())
                {
                    var userDb = context.Users.Find(_currentUser.Id);
                    if (userDb != null)
                    {
                        userDb.Username = displayName;
                        userDb.Bio = bio;
                        userDb.Location = location;

                        context.SaveChanges();
                        _currentUser = userDb;

                        SuggestionsBorder.Visibility = Visibility.Collapsed;

                        CustomMessageBox.Show("Profile updated successfully!", "Success");
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Error saving profile: " + ex.Message);
            }
        }

        private void UploadPhoto_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly)
            {
                CustomMessageBox.Show(
                    "Nu poți modifica poza altui utilizator.",
                    "Acces interzis"
                );
                return;
            }

            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Images|*.jpg;*.jpeg;*.png"
            };

            if (dlg.ShowDialog() == true)
            {
                string imagesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProfileImages");

                if (!Directory.Exists(imagesFolder))
                    Directory.CreateDirectory(imagesFolder);

                string destPath = Path.Combine(
                    imagesFolder,
                    $"profile_{_currentUser.Id}_{DateTime.Now.Ticks}{Path.GetExtension(dlg.FileName)}"
                );

                File.Copy(dlg.FileName, destPath, true);

                using (var context = new AppDbContext())
                {
                    var userDb = context.Users.Find(_currentUser.Id);

                    if (userDb != null)
                    {
                        userDb.ProfileImagePath = destPath;
                        context.SaveChanges();

                        _currentUser.ProfileImagePath = destPath;
                        SetProfileImage(destPath);

                        CustomMessageBox.Show(
                            "Poza de profil a fost actualizată.\n\nProfile picture updated.",
                            "Succes / Success"
                        );
                    }
                }
            }
        }

        private void SetProfileImage(string path)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                ProfilePictureBrush.ImageSource = bitmap;
                CameraIcon.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Eroare la încărcarea pozei: " + ex.Message);
            }
        }

        private void RefreshInterests()
        {
            using (var context = new AppDbContext())
            {
                InterestsList.ItemsSource = context.UserInterests
                    .Where(i => i.UserId == _currentUser.Id)
                    .ToList();
            }
        }

        private void BtnAddHobby_Click(object sender, RoutedEventArgs e)
        {
            HobbyPopup.IsOpen = true;
        }

        private void HobbySelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HobbySelector.SelectedItem is ListBoxItem selected)
            {
                string hobbyName = selected.Content.ToString();

                using (var context = new AppDbContext())
                {
                    if (!context.UserInterests.Any(i => i.UserId == _currentUser.Id && i.Name == hobbyName))
                    {
                        context.UserInterests.Add(new UserInterest
                        {
                            UserId = _currentUser.Id,
                            Name = hobbyName
                        });

                        context.SaveChanges();
                        RefreshInterests();
                    }
                }

                HobbyPopup.IsOpen = false;
            }
        }
        private void ActivityCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is Hobby hobby)
            {
                using (var context = new AppDbContext())
                {
                    var hobbyDb = context.Hobbies
                        .Where(h => h.Id == hobby.Id)
                        .Select(h => new
                        {
                            h.Name,
                            h.Category,
                            h.City,
                            h.Date,
                            h.MaxPeople,
                            OrganizerName = h.User.Username,
                            Participants = h.Users.Select(u => u.Username).ToList()
                        })
                        .FirstOrDefault();

                    if (hobbyDb == null)
                    {
                        CustomMessageBox.Show("Activitatea nu a fost găsită.");
                        return;
                    }

                    string participantsText = hobbyDb.Participants.Count == 0
                        ? "Niciun participant."
                        : "• " + string.Join("\n• ", hobbyDb.Participants);

                    Window detailsWindow = new Window
                    {
                        Title = "Detalii activitate",
                        Width = 440,
                        Height = 560,
                        ResizeMode = ResizeMode.NoResize,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        WindowStyle = WindowStyle.None,
                        AllowsTransparency = true,
                        Background = System.Windows.Media.Brushes.Transparent
                    };

                    Border outerBorder = new Border
                    {
                        CornerRadius = new CornerRadius(28),
                        Padding = new Thickness(10)
                    };

                    outerBorder.Background = new System.Windows.Media.LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops =
                {
                    new System.Windows.Media.GradientStop(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF6B6B"), 0),
                    new System.Windows.Media.GradientStop(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF9E5E"), 0.55),
                    new System.Windows.Media.GradientStop(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFD93D"), 1)
                }
                    };

                    Border card = new Border
                    {
                        Background = System.Windows.Media.Brushes.White,
                        CornerRadius = new CornerRadius(26),
                        Padding = new Thickness(26)
                    };

                    Grid grid = new Grid();

                    Button closeButton = new Button
                    {
                        Content = "✕",
                        Width = 38,
                        Height = 38,
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        Background = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF6B6B")),
                        Foreground = System.Windows.Media.Brushes.White,
                        BorderThickness = new Thickness(0),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

                    closeButton.Click += (s, ev) => detailsWindow.Close();

                    StackPanel panel = new StackPanel
                    {
                        Margin = new Thickness(0, 10, 0, 0)
                    };

                    TextBlock title = new TextBlock
                    {
                        Text = hobbyDb.Name,
                        FontSize = 28,
                        FontWeight = FontWeights.Black,
                        Foreground = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF6B6B")),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 5)
                    };

                    TextBlock subtitle = new TextBlock
                    {
                        Text = "Activity details",
                        FontSize = 13,
                        Foreground = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#636E72")),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 22)
                    };

                    Border detailsBox = new Border
                    {
                        Background = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFF7F4")),
                        CornerRadius = new CornerRadius(22),
                        Padding = new Thickness(18),
                        Margin = new Thickness(0, 0, 0, 18)
                    };

                    TextBlock details = new TextBlock
                    {
                        Text =
                            $"🎯 Categorie: {hobbyDb.Category}\n\n" +
                            $"👤 Organizator: {hobbyDb.OrganizerName}\n\n" +
                            $"📍 Locație: {hobbyDb.City}\n\n" +
                            $"📅 Data: {hobbyDb.Date:dd MMM yyyy HH:mm}\n\n" +
                            $"👥 Participanți ({hobbyDb.Participants.Count}/{hobbyDb.MaxPeople}):",
                        FontSize = 15,
                        Foreground = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D3436")),
                        TextWrapping = TextWrapping.Wrap
                    };

                    detailsBox.Child = details;

                    Border participantsBox = new Border
                    {
                        Background = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F5F6FA")),
                        CornerRadius = new CornerRadius(18),
                        Padding = new Thickness(16)
                    };

                    TextBlock participants = new TextBlock
                    {
                        Text = participantsText,
                        FontSize = 15,
                        Foreground = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D3436")),
                        TextWrapping = TextWrapping.Wrap
                    };

                    participantsBox.Child = participants;

                    panel.Children.Add(title);
                    panel.Children.Add(subtitle);
                    panel.Children.Add(detailsBox);
                    panel.Children.Add(participantsBox);

                    grid.Children.Add(panel);
                    grid.Children.Add(closeButton);

                    card.Child = grid;
                    outerBorder.Child = card;
                    detailsWindow.Content = outerBorder;

                    detailsWindow.ShowDialog();
                }
            }
        }
        private void DeleteInterest_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly)
                return;

            if ((sender as Button).CommandParameter is UserInterest interest)
            {
                using (var context = new AppDbContext())
                {
                    var toDelete = context.UserInterests.Find(interest.Id);

                    if (toDelete != null)
                    {
                        context.UserInterests.Remove(toDelete);
                        context.SaveChanges();
                        RefreshInterests();
                    }
                }
            }
        }
    }
}