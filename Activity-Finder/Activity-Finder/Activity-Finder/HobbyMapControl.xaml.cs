using Activity_Finder.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Web.WebView2.Core;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;

namespace Activity_Finder
{
    public partial class HobbyMapControl : UserControl
    {
        private User _currentUser;
        private string _googleApiKey = "AIzaSyDJQgSxw7taAsc23FuHBvuf-9Zle8y2jss"; // Recomandat sa nu o tii hardcodata in prod
        private double _currentPinLat = 45.9432;
        private double _currentPinLng = 24.9668;

        // VARIabilă nouă pentru a ține minte pe ce activitate am dat click pe hartă
        private int _selectedHobbyId = 0;

        public HobbyMapControl(User user)
        {
            InitializeComponent();
            _currentUser = user;

            if (LblRangeText != null)
            {
                string unit = _currentUser?.DistanceUnit ?? "KM";
                LblRangeText.Text = $"{RangeSlider.Value} {unit}";
            }

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                await MapWebView.EnsureCoreWebView2Async(null);
                MapWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                MapWebView.NavigationCompleted += (s, e) => LoadHobbiesOnMap();
                LoadGoogleMap();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare WebView2: " + ex.Message);
            }
        }

        private void LoadGoogleMap()
        {
            // ... (Codul tău de încărcare HTML/JS rămâne identic, nu l-am modificat) ...
            string html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>html, body {{ height: 100%; margin: 0; padding: 0; }} #map {{ height: 100%; width: 100%; }}</style>
                <script src='https://maps.googleapis.com/maps/api/js?key={_googleApiKey}&libraries=places'></script>
                <script>
                    var map, userMarker, userCircle, geocoder;
                    function initMap() {{
                        geocoder = new google.maps.Geocoder();
                        var startLoc = {{ lat: 45.9432, lng: 24.9668 }};
                        map = new google.maps.Map(document.getElementById('map'), {{ center: startLoc, zoom: 7, mapTypeControl: false, streetViewControl: false }});
                        userMarker = new google.maps.Marker({{ position: startLoc, map: map, draggable: true }});
                        userCircle = new google.maps.Circle({{ strokeColor: '#FF6B6B', fillColor: '#FF6B6B', fillOpacity: 0.15, map: map, center: startLoc, radius: 10000 }});
                        
                        google.maps.event.addListener(userMarker, 'drag', function() {{ userCircle.setCenter(userMarker.getPosition()); }});
                        google.maps.event.addListener(userMarker, 'dragend', function() {{ sendPos(); }});
                        map.addListener('click', function(e) {{ userMarker.setPosition(e.latLng); userCircle.setCenter(e.latLng); sendPos(); }});
                    }}
                    function sendPos() {{
                        var p = userMarker.getPosition();
                        window.chrome.webview.postMessage('PIN_MOVED:' + p.lat() + ',' + p.lng());
                    }}
                    function updateRadius(m) {{ if (userCircle) userCircle.setRadius(m); }}
                    function centerOnUserMarker() {{ if (userMarker) {{ map.setCenter(userMarker.getPosition()); map.setZoom(12); }} }}
                    function searchAndMovePin(address) {{
                        geocoder.geocode({{ 'address': address }}, function(r, s) {{
                            if (s == 'OK') {{ var p = r[0].geometry.location; map.setCenter(p); userMarker.setPosition(p); userCircle.setCenter(p); sendPos(); }}
                        }});
                    }}
                    function createSvgIcon(text, color) {{
                        var width = text.length * 8 + 30;
                        var svg = `<svg xmlns='http://www.w3.org/2000/svg' width='${{width}}' height='40'><rect x='0' y='0' width='${{width}}' height='28' rx='12' fill='${{color}}' /><text x='${{width/2}}' y='18' font-family='Arial' font-size='12' font-weight='bold' fill='white' text-anchor='middle'>${{text}}</text><path d='M ${{width/2 - 6}} 28 L ${{width/2 + 6}} 28 L ${{width/2}} 36 Z' fill='${{color}}' /></svg>`;
                        return 'data:image/svg+xml;charset=UTF-8,' + encodeURIComponent(svg);
                    }}
                    function addHobbyPin(id, lat, lng, title, cat) {{
                        var col = cat === 'Sport' ? '#FF6B6B' : (cat === 'Music' ? '#FFD93D' : '#2D3436');
                        var m = new google.maps.Marker({{ position: {{ lat: lat, lng: lng }}, map: map, icon: createSvgIcon(title, col) }});
                        m.addListener('click', function() {{ window.chrome.webview.postMessage('HOBBY_CLICK:' + id); }});
                    }}
                </script>
            </head>
            <body onload='initMap()'><div id='map'></div></body>
            </html>";
            MapWebView.NavigateToString(html);
        }

        private async void LoadHobbiesOnMap()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var hobbies = db.Hobbies
                        .Where(h => h.Latitude != 0 && h.Date > DateTime.Now)
                        .ToList();

                    foreach (var h in hobbies)
                    {
                        string script = $"addHobbyPin({h.Id}, {h.Latitude.ToString(CultureInfo.InvariantCulture)}, {h.Longitude.ToString(CultureInfo.InvariantCulture)}, '{h.Name.Replace("'", "\\'")}', '{h.Category}');";
                        await MapWebView.ExecuteScriptAsync(script);
                    }
                }
            }
            catch { }
        }

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string msg = e.TryGetWebMessageAsString();
                if (msg.StartsWith("PIN_MOVED:"))
                {
                    var coords = msg.Split(':')[1].Split(',');
                    _currentPinLat = double.Parse(coords[0], CultureInfo.InvariantCulture);
                    _currentPinLng = double.Parse(coords[1], CultureInfo.InvariantCulture);
                }
                else if (msg.StartsWith("HOBBY_CLICK:"))
                {
                    int id = int.Parse(msg.Split(':')[1]);

                    // Salvăm ID-ul ca să știm la ce dăm Join
                    _selectedHobbyId = id;

                    using (var db = new AppDbContext())
                    {
                        var h = db.Hobbies.FirstOrDefault(x => x.Id == id);
                        if (h != null)
                        {
                            LblDetTitle.Text = h.Name;
                            LblDetDesc.Text = h.Description;
                            LblDetAddress.Text = h.City;
                            LblDetPeople.Text = $"Caută {h.MaxPeople} participanți";
                            DetailsPopup.IsOpen = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Eroare primire mesaj: " + ex.Message);
            }
        }

        private void ScanArea_Click(object sender, RoutedEventArgs e)
        {
            double rKm = RangeSlider.Value;
            if (_currentUser?.DistanceUnit == "MILES") rKm *= 1.60934;

            (Window.GetWindow(this) as HomePage)?.ShowFilteredHome(_currentPinLat, _currentPinLng, rKm);
        }

        private void SearchLocation_Click(object sender, RoutedEventArgs e) => MapWebView.ExecuteScriptAsync($"searchAndMovePin('{TxtSearchLocation.Text.Replace("'", "\\'")}');");
        private void MyPin_Click(object sender, RoutedEventArgs e) => MapWebView.ExecuteScriptAsync("centerOnUserMarker();");

        private void RangeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LblRangeText != null) LblRangeText.Text = $"{Math.Round(e.NewValue)} {_currentUser?.DistanceUnit ?? "KM"}";
            double val = e.NewValue;
            double m = (_currentUser?.DistanceUnit == "MILES" ? val * 1609.34 : val * 1000);

            if (MapWebView != null && MapWebView.CoreWebView2 != null)
                MapWebView.ExecuteScriptAsync($"updateRadius({m.ToString(CultureInfo.InvariantCulture)});");
        }

        private void CloseDetails_Click(object sender, RoutedEventArgs e) => DetailsPopup.IsOpen = false;

        // --- AICI ESTE LOGICA DE JOIN ADAPTATĂ DUPĂ CEA DIN HOMEPAGE ---
        private void JoinHobby_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedHobbyId == 0) return;

            try
            {
                using (var context = new AppDbContext())
                {
                    var hobby = context.Hobbies
                        .Include(h => h.Users)
                        .FirstOrDefault(h => h.Id == _selectedHobbyId);

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

                    DetailsPopup.IsOpen = false; // Închidem pop-up-ul după succes
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Eroare la trimiterea cererii: " + ex.Message);
            }
        }
    }
}