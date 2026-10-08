using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ProLeagueDesk.Models;

namespace ProLeagueDesk
{
    public partial class RegistreerWindow : Window
    {
        private ProLeagueDeskContext _context = new ProLeagueDeskContext();

        public RegistreerWindow()
        {
            InitializeComponent();
        }
        private void btnRegisteren_Click(object sender, RoutedEventArgs e)
        {
            string username = tbUsername.Text.Trim();
            string password = tbPasswoord.Password;
            string role = GetSelectedRole();

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(role))
            {
                MessageBox.Show("Vul alle velden in!", "Validatie", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (role == "Admin")
            {
                AdminCheckWindow check = new AdminCheckWindow();
                check.ShowDialog();

                if (!check.IsValid)
                {
                    MessageBox.Show("Geen toegang tot admin registratie.", "Toegang Geweigerd", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }
            }

            try
            {
                bool exists = _context.Users.Any(u => u.Username.ToLower() == username.ToLower());
                if (exists)
                {
                    MessageBox.Show("Gebruikersnaam bestaat al!", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                User newUser = new User
                {
                    Username = username,
                    Password = BCrypt.Net.BCrypt.HashPassword(password),
                    Role = role,
                    ProfileImagePath = null
                };

                _context.Users.Add(newUser);

                _context.SaveChanges();

                if (role == "Coach")
                {
                    Coach newCoach = new Coach
                    {
                        UserId = newUser.UserId,
                        Name = newUser.Username,
                        PhoneNumber = 0         
                    };
                    _context.Coaches.Add(newCoach);
                }
                else if (role == "Scheidsrechter")
                {
                    Referee newReferee = new Referee
                    {
                        UserId = newUser.UserId,
                        Name = newUser.Username,
                        MatchesOfficiated = 0
                    };
                    _context.Referees.Add(newReferee);
                }
                else if (role == "Admin")
                {
                    Admin newAdmin = new Admin
                    {
                        UserId = newUser.UserId,
                        AdminNumber = 1000 + newUser.UserId 
                    };
                    _context.Admins.Add(newAdmin);
                }

                _context.SaveChanges();

                MessageBox.Show($"Account succesvol aangemaakt! Je bent geregistreerd als {role}.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                MainWindow mw = new MainWindow();
                mw.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Er ging iets mis bij het opslaan in de database: {ex.Message}", "Database Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Terug_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mw = new MainWindow();
            mw.Show();
            this.Close();
        }

        private string GetSelectedRole()
        {
            var role = RolePanel.Children
                .OfType<System.Windows.Controls.RadioButton>()
                .FirstOrDefault(r => r.IsChecked == true);

            return role?.Content?.ToString();
        }

        private void Button_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbPasswoordVisible.Text = tbPasswoord.Password;

            tbPasswoord.Visibility = Visibility.Collapsed;
            tbPasswoordVisible.Visibility = Visibility.Visible;
        }

        private void Button_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            tbPasswoord.Password = tbPasswoordVisible.Text;

            tbPasswoordVisible.Visibility = Visibility.Collapsed;
            tbPasswoord.Visibility = Visibility.Visible;
        }
    }
}