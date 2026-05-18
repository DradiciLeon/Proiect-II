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

        private string _googleApiKey = "AIzaSyDJQgSxw7taAsc23FuHBvuf-9Zle8y2jss";
        private DispatcherTimer _typingTimer;
        private double _selectedLat = 0;
        private double _selectedLng = 0;
        private bool _isSelectingFromList = false;

        public PostHobby(int userId)
        {
            InitializeComponent();
            _currentUserId = userId;

            HobbyDate.DisplayDateStart = DateTime.Today;
            HobbyDate.SelectedDate = DateTime.Today;

            for (int h = 0; h < 24; h++)
                HourComboBox.Items.Add(h.ToString("00"));

            for (int m = 0; m < 60; m += 5)
                MinuteComboBox.Items.Add(m.ToString("00"));

            HourComboBox.SelectedItem = DateTime.Now.Hour.ToString("00");
            MinuteComboBox.SelectedItem = "00";

            _typingTimer = new DispatcherTimer();
            _typingTimer.Interval = TimeSpan.FromMilliseconds(500);
            _typingTimer.Tick += TypingTimer_Tick;
        }

        private void CityInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSelectingFromList) return;

            _selectedLat = 0;
            _selectedLng = 0;

            _typingTimer.Stop();
            _typingTimer.Start();
        }

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

                            LocationSuggestionsBox.ItemsSource = suggestions;
                            SuggestionsBorder.Visibility = suggestions.Count > 0
                                ? Visibility.Visible
                                : Visibility.Collapsed;
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

        private async void LocationSuggestionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LocationSuggestionsBox.SelectedItem is string selectedAddress)
            {
                _isSelectingFromList = true;

                CityInput.Text = selectedAddress;
                SuggestionsBorder.Visibility = Visibility.Collapsed;
                LocationSuggestionsBox.SelectedItem = null;

                _isSelectingFromList = false;

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
                catch
                {
                    _selectedLat = 0;
                    _selectedLng = 0;
                }
            }
        }

        private void BtnPostFinal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string hobbyName = HobbyNameInput.Text.Trim();
                string fullAddress = CityInput.Text.Trim();

                string description = string.IsNullOrWhiteSpace(HobbyDescriptionInput.Text) ? "Nicio descriere furnizată." : HobbyDescriptionInput.Text.Trim();

                if (string.IsNullOrWhiteSpace(hobbyName) || string.IsNullOrWhiteSpace(fullAddress))
                {
                    CustomMessageBox.Show("Te rog completează numele hobby-ului și locația!");
                    return;
                }

                if (!ContentFilter.IsSafeText(hobbyName, 60, out string hobbyNameError))
                {
                    CustomMessageBox.Show(hobbyNameError, "Conținut invalid");
                    return;
                }

                if (!ContentFilter.IsSafeText(fullAddress, 150, out string cityError))
                {
                    CustomMessageBox.Show(cityError, "Conținut invalid");
                    return;
                }

                if (CategorySelector.SelectedItem == null ||
                    HourComboBox.SelectedItem == null ||
                    MinuteComboBox.SelectedItem == null)
                {
                    CustomMessageBox.Show("Te rog selectează categoria și ora!");
                    return;
                }

                int hour = int.Parse(HourComboBox.SelectedItem.ToString());
                int minute = int.Parse(MinuteComboBox.SelectedItem.ToString());

                DateTime selectedDate = HobbyDate.SelectedDate ?? DateTime.Today;
                DateTime finalDateTime = selectedDate.Date.Add(new TimeSpan(hour, minute, 0));

                if (finalDateTime < DateTime.Now)
                {
                    CustomMessageBox.Show("Nu poți posta în trecut!");
                    return;
                }

                if (_selectedLat == 0 && _selectedLng == 0)
                {
                    CustomMessageBox.Show(
                        "Te rog selectează o locație validă din lista de sugestii!",
                        "Locație nedetectată"
                    );
                    return;
                }

                using (var context = new AppDbContext())
                {
                    bool userHasActivityAtSameTime = context.Hobbies.Any(h =>
                        h.UserId == _currentUserId &&
                        h.Date == finalDateTime);

                    if (userHasActivityAtSameTime)
                    {
                        CustomMessageBox.Show(
                            "Ai deja o activitate postată la această oră.\n\nAlege altă oră.",
                            "Conflict de timp"
                        );
                        return;
                    }

                    bool samePlaceSameTime = context.Hobbies.Any(h =>
                        h.City.ToLower() == fullAddress.ToLower() &&
                        h.Date == finalDateTime);

                    if (samePlaceSameTime)
                    {
                        CustomMessageBox.Show(
                            "Există deja o activitate în această locație la această oră.\n\nAlege altă locație sau altă oră.",
                            "Conflict de locație"
                        );
                        return;
                    }

                    var newHobby = new Hobby
                    {
                        Name = hobbyName,
                        Category = (CategorySelector.SelectedItem as ListBoxItem).Content.ToString(),
                        Date = finalDateTime,
                        CreatedAt = DateTime.Now,
                        MaxPeople = (int)PeopleSlider.Value,
                        City = fullAddress,
                        Latitude = _selectedLat,
                        Longitude = _selectedLng,
                        Description = description,
                        UserId = _currentUserId
                    };

                    context.Hobbies.Add(newHobby);
                    context.SaveChanges();

                    CustomMessageBox.Show("Postat cu succes!");
                    ReturnToDashboard();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Eroare: {ex.Message}");
            }
        }

        private void CategorySelector_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new System.Windows.Input.MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = System.Windows.UIElement.MouseWheelEvent,
                    Source = sender
                };
                var parent = ((System.Windows.FrameworkElement)sender).Parent as System.Windows.UIElement;
                parent?.RaiseEvent(eventArg);
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