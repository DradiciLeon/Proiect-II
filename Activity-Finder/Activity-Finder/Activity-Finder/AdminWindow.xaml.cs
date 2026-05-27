using Activity_Finder.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Activity_Finder
{
    public partial class AdminWindow : Window
    {
        private int _ticketToSolveId = 0;
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
                    var usersList = context.Users
                        .Include(u => u.Hobbies)
                        .Select(u => new
                        {
                            u.Id,
                            u.Username,
                            u.Email,
                            Score = u.Hobbies.Count * 10
                        })
                        .ToList();
                    UsersGrid.ItemsSource = usersList;

                    var postsDb = context.Hobbies
                        .Include(h => h.User)
                        .Include(h => h.Users)
                        .ToList();

                    var postsList = postsDb.Select(h => new
                    {
                        h.Id,
                        Title = h.Name,
                        Category = h.Category,
                        Author = h.User != null ? h.User.Username : "Utilizator șters",
                        Description = h.Description,
                        Location = h.City,
                        Time = h.Date.HasValue ? h.Date.Value.ToString("HH:mm") : "Nesetată",
                        Date = h.Date.HasValue ? h.Date.Value.ToString("dd MMM yyyy") : "Nesetată",
                        Participants = h.Users != null && h.Users.Any()
                            ? string.Join(", ", h.Users.Select(u => u.Username))
                            : "❌ Niciun participant înscris încă"
                    }).ToList();
                    PostsGrid.ItemsSource = postsList;

                    var supportDb = context.SupportMessages
    .Include(s => s.User)
    .OrderBy(s => s.IsSolved)
    .ThenByDescending(s => s.SentAt)
    .ToList(); // Executăm SQL-ul mai întâi și aducem în memorie

                    var supportList = supportDb.Select(s => new
                    {
                        s.Id,
                        Author = s.User != null ? s.User.Username : "Anonim",
                        s.Message,
                        Date = s.SentAt.ToString("dd MMM yyyy HH:mm"),
                        Status = s.IsSolved ? "Rezolvat" : "În așteptare",
                        IsNotSolved = !s.IsSolved,
                        s.AdminReply,
                        ReplyVisibility = !string.IsNullOrWhiteSpace(s.AdminReply) ? Visibility.Visible : Visibility.Collapsed
                    }).ToList();

                    SupportGrid.ItemsSource = supportList;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Eroare la încărcarea datelor de administrare: " + ex.Message);
            }
        }

        private void SolveTicket_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            dynamic selectedTicket = btn.DataContext;
            _ticketToSolveId = selectedTicket.Id;

            TxtAdminReply.Clear();
            ReplyPopup.Visibility = Visibility.Visible;
        }

        private void CancelReply_Click(object sender, RoutedEventArgs e)
        {
            ReplyPopup.Visibility = Visibility.Collapsed;
            _ticketToSolveId = 0;
        }

        private void ConfirmSolve_Click(object sender, RoutedEventArgs e)
        {
            if (_ticketToSolveId == 0) return;

            using (var context = new AppDbContext())
            {
                var ticketDb = context.SupportMessages.Find(_ticketToSolveId);
                if (ticketDb != null)
                {
                    ticketDb.IsSolved = true;
                    ticketDb.AdminReply = TxtAdminReply.Text.Trim();
                    context.SaveChanges();
                    LoadData();
                }
            }

            ReplyPopup.Visibility = Visibility.Collapsed;
            _ticketToSolveId = 0;
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