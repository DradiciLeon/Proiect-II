using Activity_Finder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Activity_Finder
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        private User _user;

        public SettingsControl(User user)
        {
            InitializeComponent();
            _user = user;
            LoadUserData();
        }

        private void LoadUserData()
        {
            // Punem numele (dacă DisplayName e gol, folosim Username-ul de login)
            DisplayNameInput.Text = _user.DisplayName ?? _user.Username;

            // Setăm RadioButton-urile pentru Notificări
            if (_user.PushNotifications) PushYes.IsChecked = true;
            else PushNo.IsChecked = true;

            // Setăm RadioButton-urile pentru Distanță
            if (_user.DistanceUnit == "KM") KmRadio.IsChecked = true;
            else MilesRadio.IsChecked = true;

            // Setăm ComboBox-ul
            VisibilityCombo.Text = _user.ProfileVisibility;

            // Setăm RadioButton-urile pentru Badge
            if (_user.ShowHobbyBadge) BadgeOn.IsChecked = true;
            else BadgeOff.IsChecked = true;
        }

        private void BtnApplyChanges_Click(object sender, RoutedEventArgs e)
        {
            using (var context = new AppDbContext())
            {
                // Găsim userul în baza de date după ID
                var dbUser = context.Users.FirstOrDefault(u => u.Id == _user.Id);

                if (dbUser != null)
                {
                    // Citim din interfață și salvăm în baza de date
                    dbUser.DisplayName = DisplayNameInput.Text;

                    // Verificăm care RadioButton e bifat
                    dbUser.PushNotifications = PushYes.IsChecked == true;
                    dbUser.DistanceUnit = KmRadio.IsChecked == true ? "KM" : "MILES";
                    dbUser.ShowHobbyBadge = BadgeOn.IsChecked == true;
                    dbUser.ProfileVisibility = (VisibilityCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

                    context.SaveChanges();

                    // Sincronizăm și obiectul local (cel din memorie) ca să nu apară diferențe
                    _user.DisplayName = dbUser.DisplayName;
                    _user.PushNotifications = dbUser.PushNotifications;
                    _user.DistanceUnit = dbUser.DistanceUnit;
                    _user.ProfileVisibility = dbUser.ProfileVisibility;
                    _user.ShowHobbyBadge = dbUser.ShowHobbyBadge;

                    MessageBox.Show("Setările au fost salvate cu succes în baza de date!");
                }
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            LogIn loginWin = new LogIn();
            loginWin.Show();
            Window.GetWindow(this).Close(); // Închide fereastra principală care găzduiește acest control
        }

        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Sigur vrei să ștergi contul? Acțiunea este ireversibilă!",
                                         "Atenție", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                using (var context = new AppDbContext())
                {
                    var dbUser = context.Users.FirstOrDefault(u => u.Id == _user.Id);
                    if (dbUser != null)
                    {
                        context.Users.Remove(dbUser);
                        context.SaveChanges();
                        Logout_Click(null, null);
                    }
                }
            }
        }
    }
}
