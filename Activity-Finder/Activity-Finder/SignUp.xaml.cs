using Activity_Finder;
using Activity_Finder.Models; // Asigură-te că acest namespace este corect pentru AppDbContext și clasa User
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Activity_Finder
{
    public partial class SignUp : Window
    {
        public SignUp()
        {
            InitializeComponent();
        }

        // Funcție care verifică dacă textul are format de email (ex: nume@domeniu.com)
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Șablonul standard pentru orice email valid
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

            // DACĂ VREI STRICT DOAR GMAIL ȘI YAHOO, folosește șablonul de mai jos în loc de cel de sus:
            // string pattern = @"^[a-zA-Z0-9._%+-]+@(gmail\.com|yahoo\.com|yahoo\.ro)$";

            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return regex.IsMatch(email);
        }


        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            // 1. Verificăm dacă sunt goale
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                MessageBox.Show("Te rog completează toate câmpurile!", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // --- NOU: 2. VERIFICĂM DACĂ EMAILUL ESTE VALID ---
            if (!IsValidEmail(email))
            {
                MessageBox.Show("Te rog introdu o adresă de email validă (ex: nume@gmail.com)!", "Email invalid", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Verificăm dacă parolele coincid
            if (password != confirmPassword)
            {
                MessageBox.Show("Parolele nu coincid! Încearcă din nou.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Interacțiunea cu Baza de Date
            using (var context = new AppDbContext())
            {
                // Verificăm dacă username-ul sau email-ul există deja
                bool userExists = context.Users.Any(u => u.Username == username || u.Email == email);

                if (userExists)
                {
                    MessageBox.Show("Acest Username sau Email este deja folosit!", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Dacă totul e ok, creăm noul User
                var newUser = new User
                {
                    Username = username,
                    Email = email,
                    Password = password // Pentru un proiect real, aici ar trebui pus un HASH, nu parola directă
                };

                // Adăugăm în baza de date și salvăm
                context.Users.Add(newUser);
                context.SaveChanges();

                MessageBox.Show("Cont creat cu succes! Te poți loga acum.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                // Ne întoarcem automat la fereastra de Login
                // Dacă fereastra ta de login se numește MainWindow sau LogIn, modifică mai jos:
                LogIn loginWindow = new LogIn();
                loginWindow.Show();
                this.Close(); // Închidem fereastra de Sign Up
            }
        }

        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            // Dacă user-ul se răzgândește și apasă pe "Back to Login"
            LogIn loginWindow = new LogIn();
            loginWindow.Show();
            this.Close();
        }

        // Asta e funcția care îți permite să tragi fereastra (drag)
        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}