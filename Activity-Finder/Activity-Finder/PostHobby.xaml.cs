using Activity_Finder.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Activity_Finder
{
    public partial class PostHobby : Window
    {
        private int _currentUserId;

        // Constructorul primește ID-ul userului care postează
        public PostHobby(int userId)
        {
            InitializeComponent();
            _currentUserId = userId;

            // Opțional: setăm data minimă ca fiind azi
            HobbyDate.DisplayDateStart = DateTime.Today;
            HobbyDate.SelectedDate = DateTime.Today;
        }

        private void BtnPostFinal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Validare date
                if (string.IsNullOrWhiteSpace(HobbyNameInput.Text))
                {
                    MessageBox.Show("Te rog introdu un nume pentru hobby!", "Validare", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CategoryCombo.SelectedItem == null)
                {
                    MessageBox.Show("Te rog selectează o categorie!", "Validare", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 2. Salvare în baza de date
                using (var context = new AppDbContext())
                {
                    // Căutăm userul în DB pentru a-l adăuga în lista de participanți
                    var currentUser = context.Users.FirstOrDefault(u => u.Id == _currentUserId);

                    if (currentUser == null)
                    {
                        MessageBox.Show("Eroare: Utilizatorul nu a fost găsit!");
                        return;
                    }

                    // Creăm obiectul Hobby conform modelului tău actualizat
                    var newHobby = new Hobby
                    {
                        Name = HobbyNameInput.Text.Trim(),
                        Category = (CategoryCombo.SelectedItem as ComboBoxItem)?.Content.ToString(),
                        Date = HobbyDate.SelectedDate,
                        MaxPeople = (int)PeopleSlider.Value,
                        Description = $"Activitate creată de {currentUser.Username}"
                    };

                    // Adăugăm creatorul în lista de participanți (Many-to-Many)
                    newHobby.Users.Add(currentUser);

                    context.Hobbies.Add(newHobby);
                    context.SaveChanges();

                    MessageBox.Show("Hobby-ul tău a fost postat cu succes!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Închidem fereastra după succes
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A apărut o eroare la salvarea hobby-ului:\n{ex.Message}", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}