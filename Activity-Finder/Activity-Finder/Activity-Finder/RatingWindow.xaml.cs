using Activity_Finder.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Activity_Finder
{
    public partial class RatingWindow : Window
    {
        private Hobby _hobby;
        private User _me;

        public RatingWindow(Hobby hobby, User currentUser)
        {
            InitializeComponent();
            _hobby = hobby;
            _me = currentUser;
            TxtHobbyName.Text = hobby.Name;
        }

        private void Rate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag != null)
                {
                    int stars = int.Parse(btn.Tag.ToString());

                    if (_hobby == null || _me == null || _hobby.UserId == null)
                        return;

                    using (var context = new AppDbContext())
                    {
                        var hobbyDb = context.Hobbies
                            .Include(h => h.Users)
                            .FirstOrDefault(h => h.Id == _hobby.Id);

                        if (hobbyDb == null)
                        {
                            CustomMessageBox.Show("Activitatea nu a fost găsită.");
                            return;
                        }

                        if (hobbyDb.Date > DateTime.Now)
                        {
                            CustomMessageBox.Show("Poți da rating doar după ce activitatea s-a terminat.", "Rating blocat");
                            return;
                        }

                        if (hobbyDb.UserId == _me.Id)
                        {
                            CustomMessageBox.Show("Nu poți da rating propriei activități.", "Rating blocat");
                            return;
                        }

                        bool participated = hobbyDb.Users.Any(u => u.Id == _me.Id);

                        if (!participated)
                        {
                            CustomMessageBox.Show("Poți da rating doar dacă ai participat la activitate.", "Rating blocat");
                            return;
                        }

                        bool alreadyRated = context.Ratings.Any(r =>
                            r.HobbyId == hobbyDb.Id &&
                            r.FromUserId == _me.Id &&
                            r.ToUserId == hobbyDb.UserId);

                        if (alreadyRated)
                        {
                            CustomMessageBox.Show("Ai acordat deja rating pentru această activitate.", "Rating existent");
                            return;
                        }

                        var rating = new Rating
                        {
                            HobbyId = hobbyDb.Id,
                            FromUserId = _me.Id,
                            ToUserId = (int)hobbyDb.UserId,
                            Stars = stars,
                            CreatedAt = DateTime.Now
                        };

                        context.Ratings.Add(rating);
                        context.SaveChanges();
                    }

                    this.Close();
                    CustomMessageBox.Show("Rating salvat! Mulțumim.", "Succes");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la salvarea notei: " + ex.Message);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}