using System.Windows;

namespace Activity_Finder
{
    public partial class CustomMessageBox : Window
    {
        public CustomMessageBox(string message, string title = "Hobby App")
        {
            InitializeComponent();

            TitleText.Text = title;
            MessageText.Text = message;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static void Show(string message, string title = "Hobby App")
        {
            CustomMessageBox box = new CustomMessageBox(message, title);
            box.ShowDialog();
        }
    }
}