using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ProLeagueDesk.Models;

namespace ProLeagueDesk
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnRegistreer_Click(object sender, RoutedEventArgs e)
        {
            RegistreerWindow rw = new RegistreerWindow();
            rw.Show();
            this.Hide();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = tbUsername.Text;
            string password = tbPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Vul alle velden in!");
                return;
            }

            ProLeagueDeskContext context = new ProLeagueDeskContext();

            var user = context.Users.FirstOrDefault(u => u.Username.ToLower() == username.ToLower());

            if (user == null)
            {
                MessageBox.Show("Gebruiker niet gevonden!");
                return;
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);

            if (!isPasswordValid)
            {
                MessageBox.Show("Fout wachtwoord!");
                return;
            }

            if (user.Role == "Admin")
            {
                MessageBox.Show("Welkom Admin !");
                AdminWindow aw = new AdminWindow(user);
                aw.Show();
            }
            else if (user.Role == "Scheidsrechter")
            {
                MessageBox.Show("Welkom Scheidsrechter !");
                RefereeWindow rw = new RefereeWindow(user);
                rw.Show();
            }
            else if (user.Role == "Coach")
            {
                MessageBox.Show("Welkom Coach !");
                CoachWindow cw = new CoachWindow(user);
                cw.Show();
            }

            this.Close();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Button_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbPasswordVisible.Text = tbPassword.Password;

            tbPassword.Visibility = Visibility.Collapsed;
            tbPasswordVisible.Visibility = Visibility.Visible;
        }

        private void Button_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            tbPassword.Password = tbPasswordVisible.Text;

            tbPasswordVisible.Visibility = Visibility.Collapsed;
            tbPassword.Visibility = Visibility.Visible;
        }
    }
}