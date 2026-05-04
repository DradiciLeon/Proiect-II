using Activity_Finder.Models;
using Activity_Finder.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Activity_Finder
{
    public partial class PostHobby : UserControl
    {
        private int _currentUserId;

        // Variabile pentru Autocomplete Locație
        private string _googleApiKey = "AIzaSyDJQgSxw7taAsc23FuHBvuf-9Zle8y2jss";
        private DispatcherTimer _typingTimer;
        private double _selectedLat = 0;
        private double _selectedLng = 0;
        private bool _isSelectingFromList = false; // Ne ajută să nu căutăm din nou când dăm click pe o sugestie

        public PostHobby(int userId)
        {
            InitializeComponent();
            _currentUserId = userId;

            HobbyDate.DisplayDateStart = DateTime.Today;
            HobbyDate.SelectedDate = DateTime.Today;

            for (int h = 0; h < 24; h++) HourComboBox.Items.Add(h.ToString("00"));
            for (int m = 0; m < 60; m += 5) MinuteComboBox.Items.Add(m.ToString("00"));

            HourComboBox.SelectedItem = DateTime.Now.Hour.ToString("00");
            MinuteComboBox.SelectedItem = "00";

            // Setăm Timer-ul la jumătate de secundă. Va rula doar când te oprești din scris.
            _typingTimer = new DispatcherTimer();
            _typingTimer.Interval = TimeSpan.FromMilliseconds(500);
            _typingTimer.Tick += TypingTimer_Tick;
        }

        // Se declanșează la fiecare literă tastată
        private void CityInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSelectingFromList) return; // Ignorăm dacă doar a selectat cu click-ul din listă

            _typingTimer.Stop(); // Oprește timerul vechi
            _typingTimer.Start(); // Pornește altul nou
        }

        // Se execută doar după ce utilizatorul se oprește 500ms din scris
        private async void TypingTimer_Tick(object sender, EventArgs e)
        {
            _typingTimer.Stop();
            string input = CityInput.Text.Trim();

            if (input.Length < 3)
            {
                SuggestionsBorder.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Apelăm Google Places Autocomplete API
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
                    }
                }
            }
            catch { /* Ignorăm erorile de rețea ca să nu enervăm userul cu popup-uri de crash */ }
        }

        // Se declanșează când dai click pe o sugestie din ListBox
        private async void LocationSuggestionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LocationSuggestionsBox.SelectedItem is string selectedAddress)
            {
                _isSelectingFromList = true;
                CityInput.Text = selectedAddress; // Punem adresa oficială în casetă
                SuggestionsBorder.Visibility = Visibility.Collapsed; // Ascundem sugestiile
                _isSelectingFromList = false;

                // Acum că avem o adresă validă, cerem Coordonatele GPS (Lat și Lng) prin Geocoding API
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        string url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(selectedAddress)}&key={_googleApiKey}";
                        string response = await client.GetStringAsync(url);

                        using (JsonDocument doc = JsonDocument.Parse(response))
                        {
                            var status = doc.RootElement.GetProperty("status").GetString();
                            if (status == "OK")
                            {
                                var location = doc.RootElement.GetProperty("results")[0]
                                                          .GetProperty("geometry")
                                                          .GetProperty("location");

                                _selectedLat = location.GetProperty("lat").GetDouble();
                                _selectedLng = location.GetProperty("lng").GetDouble();
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private void BtnPostFinal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string hobbyName = HobbyNameInput.Text.Trim();
                string fullAddress = CityInput.Text.Trim();
                string hobbyNameError = string.Empty;
                string cityError = string.Empty;

                if (string.IsNullOrWhiteSpace(hobbyName) || string.IsNullOrWhiteSpace(fullAddress))
                {
                    MessageBox.Show("Te rog completează numele hobby-ului și locația!");
                    return;
                }

                if (!ContentFilter.IsSafeText(hobbyName, 60, out hobbyNameError))
                {
                    MessageBox.Show(hobbyNameError, "Conținut invalid");
                    return;
                }

                if (!ContentFilter.IsSafeText(fullAddress, 150, out cityError)) // Am mărit limita la 150 pentru adrese lungi
                {
                    MessageBox.Show(cityError, "Conținut invalid");
                    return;
                }

                if (CategorySelector.SelectedItem == null || HourComboBox.SelectedItem == null || MinuteComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Te rog selectează categoria și ora!");
                    return;
                }

                int hour = int.Parse(HourComboBox.SelectedItem.ToString());
                int minute = int.Parse(MinuteComboBox.SelectedItem.ToString());
                DateTime selectedDate = HobbyDate.SelectedDate ?? DateTime.Today;
                DateTime finalDateTime = selectedDate.Date.Add(new TimeSpan(hour, minute, 0));

                if (finalDateTime < DateTime.Now)
                {
                    MessageBox.Show("Nu poți posta în trecut!");
                    return;
                }

                // AVERTISMENT: Verificăm dacă user-ul chiar a dat click pe o sugestie ca să preluăm Lat/Lng
                if (_selectedLat == 0 && _selectedLng == 0)
                {
                    MessageBox.Show("Te rog selectează o locație validă din lista de sugestii!", "Locație nedetectată", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var context = new AppDbContext())
                {
                    // Verificăm ultima postare făcută de utilizatorul curent (nu participanții)
                    var lastPost = context.Hobbies
                        .Where(h => h.UserId == _currentUserId)
                        .OrderByDescending(h => h.CreatedAt)
                        .FirstOrDefault();

                    // Limitare: o postare pe oră per cont
                    if (lastPost != null && (DateTime.Now - lastPost.CreatedAt) < TimeSpan.FromHours(1))
                    {
                        int minutesLeft = (int)Math.Ceiling(
                            (TimeSpan.FromHours(1) - (DateTime.Now - lastPost.CreatedAt)).TotalMinutes
                        );

                        MessageBox.Show($"Mai poți posta peste {minutesLeft} minute.");
                        return;
                    }

                    var newHobby = new Hobby
                    {
                        Name = hobbyName,
                        Category = (CategorySelector.SelectedItem as ListBoxItem).Content.ToString(),
                        Date = finalDateTime,
                        CreatedAt = DateTime.Now,
                        MaxPeople = (int)PeopleSlider.Value,
                        City = fullAddress, // Aici se va salva toată adresa
                        Latitude = _selectedLat, // Coordonata X pe hartă
                        Longitude = _selectedLng, // Coordonata Y pe hartă
                        Description = "No description provided.",
                        UserId = _currentUserId
                    };

                    context.Hobbies.Add(newHobby);
                    context.SaveChanges();

                    MessageBox.Show("Postat cu succes!");
                    ReturnToDashboard();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eroare: {ex.Message}");
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e) => ReturnToDashboard();

        private void ReturnToDashboard()
        {
            var parentWindow = Window.GetWindow(this) as HomePage;
            parentWindow?.ShowHome_Click(null, null);
        }
    }
}