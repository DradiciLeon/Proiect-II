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
                    CustomMessageBox.Show("Te rog introdu username-ul și parola!", "Date lipsă");
                    return;
                }

                // 2. Protecție Brute Force (Păstrată)
                if (LoginAttemptLimiter.IsLocked(username, out TimeSpan remaining))
                {
                    CustomMessageBox.Show($"Prea multe încercări greșite. Încearcă din nou peste {Math.Ceiling(remaining.TotalMinutes)} minute.", "Cont blocat temporar");
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

                        CustomMessageBox.Show("Acces autorizat: Bun venit în panoul de administrare!", "Admin Login");

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

                        CustomMessageBox.Show($"Te-ai logat cu succes! Bine ai venit, {user.Username}!", "Succes");

                        HomePage home = new HomePage(user);
                        home.Show();
                        this.Close();
                        return;
                    }

                    // 4. Înregistrare eșec (Păstrată)
                    LoginAttemptLimiter.RegisterFailedAttempt(username);

                    CustomMessageBox.Show("Username sau parolă incorecte!", "Eroare login");
                }
            }
            catch (DbUpdateException ex)
            {
                AppLogger.Log(ex);
                CustomMessageBox.Show("A apărut o eroare la actualizarea datelor.", "Eroare bază de date");
            }
            catch (Exception ex)
            {
                AppLogger.Log(ex);
                CustomMessageBox.Show("A apărut o eroare la logare.", "Eroare");
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