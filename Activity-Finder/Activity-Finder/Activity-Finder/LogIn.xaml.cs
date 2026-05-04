using Activity_Finder.Models;
using Activity_Finder.Security;
using Activity_Finder.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;

namespace Activity_Finder
{
    public partial class LogIn : Window
    {
        public LogIn()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = UsernameBox.Text?.Trim() ?? "";
                string password = GetPassword();

                // 1. Validări de bază (Păstrate)
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Te rog introdu username-ul și parola!", "Date lipsă", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 2. Protecție Brute Force (Păstrată)
                if (LoginAttemptLimiter.IsLocked(username, out TimeSpan remaining))
                {
                    MessageBox.Show($"Prea multe încercări greșite. Încearcă din nou peste {Math.Ceiling(remaining.TotalMinutes)} minute.", "Cont blocat temporar", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var context = new AppDbContext())
                {
                    // --- LOGICĂ NOUĂ: VERIFICARE ADMIN ---
                    // Căutăm direct în tabelul de Admins (fără hash, plain text conform cerinței)
                    var admin = context.Admins
                        .FirstOrDefault(a => a.Username == username && a.Password == password);

                    if (admin != null)
                    {
                        // Resetăm încercările greșite pentru acest username, chiar dacă e admin
                        LoginAttemptLimiter.Reset(username);

                        MessageBox.Show("Acces autorizat: Bun venit în panoul de administrare!", "Admin Login", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Deschidem fereastra de Admin
                        AdminWindow adminWin = new AdminWindow();
                        adminWin.Show();

                        this.Close();
                        return;
                    }
                    // --- SFÂRȘIT LOGICĂ ADMIN ---


                    // 3. Logică existentă: Verificare User Normal (Păstrată intactă)
                    // Folosim AsEnumerable() pentru verificarea case-sensitive cerută de tine
                    var user = context.Users
                        .AsEnumerable()
                        .FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.Ordinal));

                    if (user != null && PasswordHasher.VerifyPassword(password, user.Password))
                    {
                        // Actualizare hash dacă e nevoie (Păstrată)
                        if (!PasswordHasher.IsHashedPassword(user.Password))
                        {
                            user.Password = PasswordHasher.HashPassword(password);
                            context.SaveChanges();
                        }

                        LoginAttemptLimiter.Reset(username);

                        MessageBox.Show($"Te-ai logat cu succes! Bine ai venit, {user.Username}!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                        HomePage home = new HomePage(user);
                        home.Show();
                        this.Close();
                        return;
                    }

                    // 4. Înregistrare eșec (Păstrată)
                    LoginAttemptLimiter.RegisterFailedAttempt(username);

                    MessageBox.Show("Username sau parolă incorecte!", "Eroare login", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (DbUpdateException ex)
            {
                AppLogger.Log(ex);
                MessageBox.Show("A apărut o eroare la actualizarea datelor.", "Eroare bază de date", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                AppLogger.Log(ex);
                MessageBox.Show("A apărut o eroare la logare.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetPassword()
        {
            if (PasswordBox.Visibility == Visibility.Visible)
                return PasswordBox.Password;

            return PasswordTextBox.Text;
        }

        private void CreateAccount_Click(object sender, RoutedEventArgs e)
        {
            SignUp signUpWindow = new SignUp();
            signUpWindow.Show();
            this.Close();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TogglePasswordVisibility(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Visibility == Visibility.Visible)
            {
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
            }
        }
    }
}