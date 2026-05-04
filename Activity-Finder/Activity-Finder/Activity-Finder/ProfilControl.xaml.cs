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

        // Variabile pentru Autocomplete Locație
        private string _googleApiKey = "AIzaSyDJQgSxw7taAsc23FuHBvuf-9Zle8y2jss";
        private DispatcherTimer _typingTimer;
        private bool _isSelectingFromList = false;
        private bool _locationSelectedFromSuggestions = false;

        public ProfilControl(User user)
        {
            InitializeComponent();
            _currentUser = user;

            _typingTimer = new DispatcherTimer();
            _typingTimer.Interval = TimeSpan.FromMilliseconds(500);
            _typingTimer.Tick += TypingTimer_Tick;

            LoadUserData();
        }

        // 1. Încărcare date la deschidere
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
            }

            DisplayNameInput.Text = !string.IsNullOrEmpty(_currentUser.DisplayName)
                                    ? _currentUser.DisplayName
                                    : _currentUser.Username;

            BioInput.Text = _currentUser.Bio;
            LocationInput.Text = _currentUser.Location;

            if (!string.IsNullOrWhiteSpace(_currentUser.Location))
                _locationSelectedFromSuggestions = true;

            if (!string.IsNullOrEmpty(_currentUser.ProfileImagePath) && File.Exists(_currentUser.ProfileImagePath))
            {
                SetProfileImage(_currentUser.ProfileImagePath);
            }

            RefreshInterests();
        }

        // Se declanșează la fiecare literă tastată în locație
        private void LocationInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSelectingFromList) return;

            _locationSelectedFromSuggestions = false;

            _typingTimer.Stop();
            _typingTimer.Start();
        }

        // Se execută după ce utilizatorul se oprește 500ms din scris
        private async void TypingTimer_Tick(object sender, EventArgs e)
        {
            _typingTimer.Stop();

            string input = LocationInput.Text.Trim();

            if (input.Length < 3)
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
                                suggestions.Add(pred.GetProperty("description").GetString());
                            }

                            LocationSuggestionsBox.ItemsSource = suggestions;
                            SuggestionsBorder.Visibility = Visibility.Visible;
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

        // Se declanșează când utilizatorul selectează o locație din sugestii
        private void LocationSuggestionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LocationSuggestionsBox.SelectedItem is string selectedAddress)
            {
                _isSelectingFromList = true;
                LocationInput.Text = selectedAddress;
                SuggestionsBorder.Visibility = Visibility.Collapsed;
                _isSelectingFromList = false;

                _locationSelectedFromSuggestions = true;
            }
        }

        // 2. Salvare modificări profil (Nume, Bio, Locație)
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

                // Locația trebuie aleasă din sugestiile Google, nu scrisă manual
                if (!_locationSelectedFromSuggestions)
                {
                    MessageBox.Show(
                        "Te rog selectează locația din lista de sugestii.",
                        "Locație invalidă",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
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

                        MessageBox.Show("Profile updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving profile: " + ex.Message);
            }
        }

        // 3. Upload Poza Profil
        // 3. Upload Poza Profil
private void UploadPhoto_Click(object sender, RoutedEventArgs e)
{
    OpenFileDialog dlg = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png" };

    if (dlg.ShowDialog() == true)
    {
        string imagesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProfileImages");

        if (!Directory.Exists(imagesFolder))
            Directory.CreateDirectory(imagesFolder);

        // MODIFICAT:
        // Am adăugat DateTime.Now.Ticks în numele fișierului.
        // Motiv: dacă poza are mereu același nume, WPF poate afișa imaginea veche din cache.
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

                // MODIFICAT:
                // Actualizăm imediat obiectul local, ca UI-ul să știe noua poză fără restart.
                _currentUser.ProfileImagePath = destPath;

                // MODIFICAT:
                // Reîncărcăm imaginea imediat după upload.
                SetProfileImage(destPath);

                MessageBox.Show(
                    "Poza de profil a fost actualizată.\n\nProfile picture updated.",
                    "Succes / Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }
    }
}

private void SetProfileImage(string path)
{
    try
    {
        // MODIFICAT:
        // BitmapCacheOption.OnLoad încarcă imaginea complet în memorie.
        // BitmapCreateOptions.IgnoreImageCache forțează WPF să nu folosească imaginea veche.
        BitmapImage bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();

        // MODIFICAT:
        // Freeze ajută imaginea să fie folosită stabil în UI și eliberează fișierul.
        bitmap.Freeze();

        ProfilePictureBrush.ImageSource = bitmap;
        CameraIcon.Visibility = Visibility.Collapsed;
    }
    catch (Exception ex)
    {
        MessageBox.Show("Eroare la încărcarea pozei: " + ex.Message);
    }
}

        // 4. Logica Hobby-uri
        private void RefreshInterests()
        {
            using (var context = new AppDbContext())
            {
                InterestsList.ItemsSource = context.UserInterests.Where(i => i.UserId == _currentUser.Id).ToList();
            }
        }

        private void BtnAddHobby_Click(object sender, RoutedEventArgs e) => HobbyPopup.IsOpen = true;

        private void HobbySelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HobbySelector.SelectedItem is ListBoxItem selected)
            {
                string hobbyName = selected.Content.ToString();
                using (var context = new AppDbContext())
                {
                    if (!context.UserInterests.Any(i => i.UserId == _currentUser.Id && i.Name == hobbyName))
                    {
                        context.UserInterests.Add(new UserInterest { UserId = _currentUser.Id, Name = hobbyName });
                        context.SaveChanges();
                        RefreshInterests();
                    }
                }
                HobbyPopup.IsOpen = false;
            }
        }

        private void DeleteInterest_Click(object sender, RoutedEventArgs e)
        {
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