using Activity_Finder.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Activity_Finder
{
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();
            LoadData();
        }

        // Metodă care încarcă datele în ambele tabele
        private void LoadData()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    // 1. Încărcare Useri (pentru primul tab)
                    // Calculăm un "Score" simplu bazat pe numărul de hobby-uri postate de fiecare user
                    var usersList = context.Users
                        .Include(u => u.Hobbies)
                        .Select(u => new
                        {
                            u.Id,
                            u.Username,
                            u.Email,
                            Score = u.Hobbies.Count * 10 // Ranking: 10 puncte per postare
                        })
                        .ToList();

                    UsersGrid.ItemsSource = usersList;

                    // 2. Încărcare Postări (pentru al doilea tab)
                    var postsDb = context.Hobbies.Include(h => h.User).ToList();

                    // Acum le formatăm în memorie:
                    var postsList = postsDb.Select(h => new
                    {
                        h.Id,
                        Title = h.Name,
                        Category = h.Category,
                        Author = h.User != null ? h.User.Username : "Utilizator șters",
                        Description = h.Description,
                        // Verificăm dacă Data are valoare, altfel afișăm ceva default
                        Date = h.Date.HasValue ? h.Date.Value.ToString("dd MMM yyyy HH:mm") : "Nesetată",
                        PeopleCount = h.MaxPeople
                    }).ToList();

                    PostsGrid.ItemsSource = postsList;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Eroare la încărcarea datelor de administrare: " + ex.Message);
            }
        }

        // Eveniment pentru butonul BAN USER
        private void BanUser_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            dynamic selectedUser = btn.DataContext;
            int userId = selectedUser.Id;
            string username = selectedUser.Username;

            var confirm = MessageBox.Show($"Sigur dorești să ștergi definitiv utilizatorul {username}? Toate postările lui vor fi eliminate.",
                                        "Confirmare BAN", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                using (var context = new AppDbContext())
                {
                    var userDb = context.Users.Find(userId);
                    if (userDb != null)
                    {
                        context.Users.Remove(userDb);
                        context.SaveChanges();
                        LoadData(); // Refresh tabele
                    }
                }
            }
        }

        // Eveniment pentru butonul DELETE POST
        private void DeletePost_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            dynamic selectedPost = btn.DataContext;
            int postId = selectedPost.Id;

            var confirm = MessageBox.Show("Sigur dorești să elimini această postare?",
                                        "Confirmare Ștergere", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                using (var context = new AppDbContext())
                {
                    var postDb = context.Hobbies.Find(postId);
                    if (postDb != null)
                    {
                        context.Hobbies.Remove(postDb);
                        context.SaveChanges();
                        LoadData(); // Refresh tabele
                    }
                }
            }
        }

        // Funcționalitate Search (Filtrare în timp real)
        private void TxtSearchAdmin_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = TxtSearchAdmin.Text.ToLower();
            if (string.IsNullOrWhiteSpace(filter))
            {
                LoadData();
                return;
            }

            using (var context = new AppDbContext())
            {
                // 1. Filtrăm userii (Aici e totul ok)
                UsersGrid.ItemsSource = context.Users
                    .Where(u => u.Username.ToLower().Contains(filter) || u.Email.ToLower().Contains(filter))
                    .Select(u => new { u.Id, u.Username, u.Email, Score = u.Hobbies.Count * 10 })
                    .ToList();

                // 2. Filtrăm postările și le EXTRAGEM din baza de date folosind .ToList() PRIMA DATĂ
                var filteredPostsDb = context.Hobbies
                    .Include(h => h.User)
                    .Where(h => h.Name.ToLower().Contains(filter) || h.Category.ToLower().Contains(filter) || (h.User != null && h.User.Username.ToLower().Contains(filter)))
                    .ToList(); // Executăm SQL-ul și aducem datele în memoria RAM

                // 3. Acum că datele sunt în C#, putem folosi HasValue și ToString cu format
                PostsGrid.ItemsSource = filteredPostsDb
                    .Select(h => new {
                        h.Id,
                        Title = h.Name,
                        Category = h.Category,
                        Author = h.User != null ? h.User.Username : "Anonim",
                        Description = h.Description,
                        // Linia magică ce repară eroarea:
                        Date = h.Date.HasValue ? h.Date.Value.ToString("g") : "Nesetată",
                        PeopleCount = h.MaxPeople
                    })
                    .ToList();
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            LogIn loginWin = new LogIn();
            loginWin.Show();
            this.Close();
        }
    }
}