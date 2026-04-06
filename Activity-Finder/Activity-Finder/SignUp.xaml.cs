using Activity_Finder.Models;
using Microsoft.EntityFrameworkCore; // ADĂUGAT: pentru excepții EF Core
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace Activity_Finder
{
    public partial class SignUp : Window
    {
        public SignUp()
        {
            InitializeComponent();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

                return regex.IsMatch(email);
            }
            catch (ArgumentException)
            {
                // ADĂUGAT:
                // Dacă pattern-ul regex ar fi invalid din greșeală.
                MessageBox.Show("Modelul de validare pentru email este invalid.",
                                "Eroare validare",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                // ADĂUGAT:
                // Orice altă eroare neașteptată la validarea emailului.
                MessageBox.Show($"A apărut o eroare la verificarea emailului:\n{ex.Message}",
                                "Eroare",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return false;
            }
        }

        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = UsernameBox.Text?.Trim();
                string email = EmailBox.Text?.Trim();
                string password = PasswordBox.Password;
                string confirmPassword = ConfirmPasswordBox.Password;

                // 1. Verificăm dacă sunt goale
                if (string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(password) ||
                    string.IsNullOrWhiteSpace(confirmPassword))
                {
                    MessageBox.Show("Te rog completează toate câmpurile!",
                                    "Eroare",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }

                // ADĂUGAT:
                // Verificare lungime minimă username
                if (username.Length < 3)
                {
                    MessageBox.Show("Username-ul trebuie să aibă cel puțin 3 caractere.",
                                    "Validare",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }

                // ADĂUGAT:
                // Verificare lungime minimă parolă
                if (password.Length < 6)
                {
                    MessageBox.Show("Parola trebuie să aibă cel puțin 6 caractere.",
                                    "Validare",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }

                // 2. Verificăm dacă emailul este valid
                if (!IsValidEmail(email))
                {
                    MessageBox.Show("Te rog introdu o adresă de email validă (ex: nume@gmail.com)!",
                                    "Email invalid",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }

                // 3. Verificăm dacă parolele coincid
                if (password != confirmPassword)
                {
                    MessageBox.Show("Parolele nu coincid! Încearcă din nou.",
                                    "Eroare",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }

                using (var context = new AppDbContext())
                {
                    // ADĂUGAT:
                    // Verificăm dacă DB răspunde înainte de operațiile principale.
                    context.Users.Any();

                    bool userExists = context.Users
                                             .Any(u => u.Username == username || u.Email == email);

                    if (userExists)
                    {
                        MessageBox.Show("Acest Username sau Email este deja folosit!",
                                        "Eroare",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                        return;
                    }

                    var newUser = new User
                    {
                        Username = username,
                        Email = email,
                        Password = password
                    };

                    context.Users.Add(newUser);
                    context.SaveChanges();

                    MessageBox.Show("Cont creat cu succes! Te poți loga acum.",
                                    "Succes",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);

                    LogIn loginWindow = new LogIn();
                    loginWindow.Show();
                    this.Close();
                }
            }
            catch (DbUpdateException ex)
            {
                // ADĂUGAT:
                // Foarte important la SignUp.
                // Apare la inserare/salvare dacă DB are probleme, constraint-uri, duplicate etc.
                MessageBox.Show($"Eroare la salvarea datelor în baza de date:\n{ex.Message}",
                                "Eroare bază de date",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            catch (InvalidOperationException ex)
            {
                // ADĂUGAT:
                // Poate apărea dacă există o problemă cu contextul EF sau logica interogării.
                MessageBox.Show($"Operația nu a putut fi finalizată:\n{ex.Message}",
                                "Eroare operație",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            catch (ArgumentNullException ex)
            {
                // ADĂUGAT:
                // Pentru situații în care o valoare ajunge null neașteptat.
                MessageBox.Show($"O valoare necesară lipsește:\n{ex.Message}",
                                "Eroare date",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // ADĂUGAT:
                // Catch general pentru orice altă eroare neașteptată.
                MessageBox.Show($"A apărut o eroare neașteptată la crearea contului:\n{ex.Message}",
                                "Eroare neașteptată",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogIn loginWindow = new LogIn();
                loginWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                // ADĂUGAT:
                // Pentru eventuale probleme la deschiderea ferestrei de login.
                MessageBox.Show($"Nu s-a putut reveni la fereastra de login:\n{ex.Message}",
                                "Eroare",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    DragMove();
                }
            }
            catch (InvalidOperationException ex)
            {
                // ADĂUGAT:
                // DragMove poate arunca excepție dacă este apelat într-un context nepotrivit.
                MessageBox.Show($"Fereastra nu poate fi mutată în acest moment:\n{ex.Message}",
                                "Eroare interfață",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A apărut o eroare la mutarea ferestrei:\n{ex.Message}",
                                "Eroare",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }
    }
}