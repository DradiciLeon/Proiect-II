using Activity_Finder.Models;
using System.Windows;

namespace Activity_Finder
{
    public partial class HomePage : Window
    {
        // Variabilă privată pentru a stoca userul logat în această pagină
        private User _loggedInUser;

        // Constructorul modificat să primească un User
        public HomePage(User user)
        {
            InitializeComponent();
            _loggedInUser = user;
        }

        // Event pentru butonul POST A HOBBY
       private void PostHobby_Click(object sender, RoutedEventArgs e)
        {
            // Deschidem fereastra de postare (presupunând că se numește PostHobbyWindow)
            // Trimitem ID-ul userului mai departe
            var postWindow = new PostHobby(_loggedInUser.Id);
            postWindow.ShowDialog();
        }
       
        private void Categories_Click(object sender, RoutedEventArgs e)
        {
            ExploreCateg exploreWindow = new ExploreCateg();
            exploreWindow.Show();
        }
    }
}