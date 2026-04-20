using Activity_Finder.Models;
using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore; // ADĂUGAT: pentru excepții EF Core

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
                // Preluăm datele introduse
                string username = UsernameBox.Text?.Trim();
                string password = PasswordBox.Visibility == Visibility.Visible
                    ? PasswordBox.Password
                    : PasswordTextBox.Text;

                // Verificăm dacă sunt goale
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Te rog introdu username-ul și parola!",
                                    "Atenție",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }

                // Conectare la baza de date
                using (var context = new AppDbContext())
                {
                    // ADĂUGAT:
                    // Any() forțează o interogare simplă și ne ajută să vedem rapid dacă baza de date răspunde.
                    // Este util ca verificare de conexiune înainte de login.
                    context.Users.Any();

                    var user = context.Users
                                      .FirstOrDefault(u => u.Username == username && u.Password == password);

                    if (user != null)
                    {
                        MessageBox.Show($"Te-ai logat cu succes! Bine ai venit, {user.Username}!",
                        "Succes",
                         MessageBoxButton.OK,
                        MessageBoxImage.Information);

                        // ACESTA ESTE CODUL DE REDIRECȚIONARE:
                        // Creăm HomePage și îi pasăm obiectul 'user' găsit în baza de date
                        HomePage home = new HomePage(user);
                        home.Show();

                        this.Close(); // Închidem fereastra de Login
                    }
                    else
                    {
                        MessageBox.Show("Username sau parolă incorecte!",
                                        "Eroare",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                // ADĂUGAT:
                // Apare uneori când interogarea LINQ/EF returnează ceva neașteptat
                // sau contextul este într-o stare invalidă.
                MessageBox.Show($"Eroare de operare în timpul autentificării:\n{ex.Message}",
                                "Eroare logică",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            catch (DbUpdateException ex)
            {
                // ADĂUGAT:
                // Deși la login nu faci SaveChanges(), poate apărea în anumite scenarii EF.
                MessageBox.Show($"Eroare la accesarea bazei de date:\n{ex.Message}",
                                "Eroare bază de date",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // ADĂUGAT:
                // Prinde orice altă eroare neașteptată (ex: conexiune căzută, config greșit, null etc.)
                MessageBox.Show($"A apărut o eroare neașteptată la logare:\n{ex.Message}",
                                "Eroare neașteptată",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void CreateAccount_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ADĂUGAT:
                // Try-catch și aici, în caz că fereastra SignUp nu poate fi creată/deschisă.
                SignUp signUpWindow = new SignUp();
                signUpWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nu s-a putut deschide fereastra de înregistrare:\n{ex.Message}",
                                "Eroare",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                // ADĂUGAT:
                // Rar, dar util dacă aplicația întâmpină probleme la închidere.
                MessageBox.Show($"A apărut o problemă la închiderea aplicației:\n{ex.Message}",
                                "Eroare",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void TogglePasswordVisibility(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                // ADĂUGAT:
                // Protecție pentru eventuale erori de UI.
                MessageBox.Show($"A apărut o eroare la afișarea parolei:\n{ex.Message}",
                                "Eroare interfață",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }
    }
}