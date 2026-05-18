using Activity_Finder.Models;
using Activity_Finder.Security;
using Activity_Finder.Services;
using Microsoft.EntityFrameworkCore;
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
            return !string.IsNullOrWhiteSpace(email)
                   && Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        }

        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = UsernameBox.Text?.Trim() ?? "";
                string email = EmailBox.Text?.Trim() ?? "";
                string password = PasswordBox.Password;
                string confirmPassword = ConfirmPasswordBox.Password;

                if (string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(password) ||
                    string.IsNullOrWhiteSpace(confirmPassword))
                {
                    CustomMessageBox.Show("Te rog completează toate câmpurile!", "Lipsa informatii");
                    return;
                }

                if (username.Length < 3 || username.Length > 30)
                {
                    CustomMessageBox.Show("Username-ul trebuie să aibă între 3 și 30 de caractere.", "Eroare");
                                    
                    return;
                }

                if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_.-]+$"))
                {
                    MessageBox.Show("Username-ul poate conține doar litere, cifre, punct, minus sau underscore.",
                                    "Validare",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }

                if (!IsValidEmail(email))
                {
                    MessageBox.Show("Te rog introdu o adresă de email validă (ex: nume@gmail.com)!",
                                    "Email invalid",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }

                if (password.Length < 8 ||
                    !password.Any(char.IsUpper) ||
                    !password.Any(char.IsLower) ||
                    !password.Any(char.IsDigit))
                {
                    MessageBox.Show("Parola trebuie să aibă minim 8 caractere, o literă mare, o literă mică și o cifră.",
                                    "Parolă slabă",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }

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
                        Password = PasswordHasher.HashPassword(password),

                        DisplayName = username,
                        ProfileImagePath = "", // OBLIGATORIU: Inițializare cu string gol
                        PushNotifications = true,
                        DistanceUnit = "KM",
                        ProfileVisibility = "Everyone",
                        ShowHobbyBadge = true,

                        // Inițializăm listele pentru a evita erorile de relaționare la inserare
                        Hobbies = new System.Collections.Generic.List<Hobby>(),
                        UserInterests = new System.Collections.Generic.List<UserInterest>()
                    };

                    context.Users.Add(newUser);
                    context.SaveChanges();

                    CustomMessageBox.Show("Cont creat cu succes! Te poți loga acum.", "Succes");
                                    
                    LogIn loginWindow = new LogIn();
                    loginWindow.Show();
                    this.Close();
                }
            }
            catch (DbUpdateException ex)
            {
                AppLogger.Log(ex);

                MessageBox.Show(
                    ex.InnerException?.Message ?? ex.Message,
                    "Eroare bază de date",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (Exception ex)
            {
                AppLogger.Log(ex);
                MessageBox.Show("A apărut o eroare neașteptată la crearea contului.",
                                "Eroare",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            LogIn loginWindow = new LogIn();
            loginWindow.Show();
            this.Close();
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}