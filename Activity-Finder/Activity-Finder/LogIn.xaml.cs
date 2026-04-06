using Activity_Finder.Models; // Nu uita de acest using
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
            // Preluăm datele (inclusiv logica dacă parola e vizibilă sau ascunsă)
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Visibility == Visibility.Visible ? PasswordBox.Password : PasswordTextBox.Text;

            // Verificăm dacă sunt goale
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Te rog introdu username-ul și parola!", "Atenție", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Ne conectăm la Baza de Date
            using (var context = new AppDbContext())
            {
                // Căutăm un user care are EXACT acest username și această parolă
                var user = context.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

                if (user != null)
                {
                    // Dacă user-ul a fost găsit, logarea are succes!
                    MessageBox.Show($"Te-ai logat cu succes! Bine ai venit, {user.Username}!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Aici, mai târziu, vei deschide fereastra principală a aplicației (cea cu activități)
                    // DashboardWindow dashboard = new DashboardWindow(user); // Îi poți da obiectul user ca să știi cine e logat
                    // dashboard.Show();
                    // this.Close();
                }
                else
                {
                    MessageBox.Show("Username sau parolă incorecte!", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CreateAccount_Click(object sender, RoutedEventArgs e)
        {
            // Deschidem fereastra de Sign Up
            SignUp signUpWindow = new SignUp();
            signUpWindow.Show();
            this.Close(); // Închidem fereastra curentă (Login)
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Restul codului (TogglePasswordVisibility, Window_MouseDown etc.) rămâne la fel...
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