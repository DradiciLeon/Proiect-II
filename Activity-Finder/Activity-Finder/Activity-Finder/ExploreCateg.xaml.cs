using Activity_Finder.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Activity_Finder
{
    public partial class ExploreCateg : Window
    {
        public ExploreCateg()
        {
            InitializeComponent();
        }

        private void Category_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string category)
            {
                SelectedCategoryTitle.Text = category.ToUpper();

                using (var context = new AppDbContext())
                {
                    var activities = context.Hobbies
                                    .Where(h => h.Category.ToLower().Contains(category.ToLower())
             && h.Date > DateTime.Now)
                                    .OrderBy(h => h.Date)
                                    .ToList();

                    ActivitiesList.ItemsSource = activities;

                    NoActivitiesText.Visibility = activities.Count == 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }

                CategoriesView.Visibility = Visibility.Collapsed;
                ActivitiesView.Visibility = Visibility.Visible;
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            ActivitiesView.Visibility = Visibility.Collapsed;
            CategoriesView.Visibility = Visibility.Visible;
        }
    }
}