using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ProLeagueDesk.Models;

namespace ProLeagueDesk
{
    public partial class CoachWindow : Window
    {
        private User _currentUser;
        private ProLeagueDeskContext _context;
        private Team? _coachTeam;

        private readonly string _defaultImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image", "default.jpg");

        public CoachWindow(User user)
        {
            InitializeComponent();
            _context = new ProLeagueDeskContext();

            _currentUser = _context.Users.FirstOrDefault(u => u.UserId == user.UserId) ?? user;

            LoadUserProfile();
            LoadCoachData();
            ShowPanel(panelTeam);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _context?.Dispose();
        }

        #region Profiel & Initialisatie
        private BitmapImage ZetFoto(string pad)
        {
            BitmapImage bitmap = new BitmapImage();
            using (FileStream stream = new FileStream(pad, FileMode.Open, FileAccess.Read))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }
            bitmap.Freeze();
            return bitmap;
        }

        private void LoadUserProfile()
        {
            lblWelcome.Text = $"Welkom, {_currentUser.Username}";

            try
            {
                if (!string.IsNullOrEmpty(_currentUser.ProfileImagePath) && File.Exists(_currentUser.ProfileImagePath))
                {
                    imgProfileBrush.ImageSource = ZetFoto(_currentUser.ProfileImagePath);
                }
                else if (File.Exists(_defaultImagePath))
                {
                    imgProfileBrush.ImageSource = ZetFoto(_defaultImagePath);
                }
                else
                {
                    imgProfileBrush.ImageSource = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij het laden van de profielfoto: {ex.Message}", "Fout");
                imgProfileBrush.ImageSource = null;
            }
        }

        private void LoadCoachData()
        {
            _context?.Dispose();
            _context = new ProLeagueDeskContext();

            _coachTeam = _context.Teams.FirstOrDefault(t => t.CoachId == _currentUser.UserId);

            if (_coachTeam == null)
            {
                lblTeamNameHeader.Text = "Mijn Team: Geen team toegewezen";
                lblTeamPoints.Text = "-";
                lblTeamGoalsFor.Text = "-";
                lblTeamGoalsAgainst.Text = "-";
                dgTeamMatches.ItemsSource = null;

                lblNextHome.Text = "Geen team";
                lblNextAway.Text = "toegewezen";
                lblNextDate.Text = "-";
                lblNextMatchday.Text = "Matchday -";

                UpdatePresidentSatisfaction(0, 1);
                return;
            }

            lblTeamNameHeader.Text = $"Mijn Team: {_coachTeam.Name}";
            lblTeamPoints.Text = _coachTeam.Points.ToString();
            lblTeamGoalsFor.Text = _coachTeam.GoalsFor.ToString();
            lblTeamGoalsAgainst.Text = _coachTeam.GoalsAgainst.ToString();

            var matches = _context.Matches
                .Include(m => m.Matchday)
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.HomeTeamId == _coachTeam.TeamId || m.AwayTeamId == _coachTeam.TeamId)
                .ToList();

            var today = DateTime.Today;

            var matchRows = matches.Select(m =>
            {
                bool isPlayed = m.IsPlayed;
                if (!isPlayed && DateTime.TryParse(m.Datum, out DateTime matchDate))
                {
                    isPlayed = matchDate.Date < today;
                }
                return new
                {
                    MatchdayNumber = m.Matchday != null ? m.Matchday.Number : 0,
                    HomeTeamName = m.HomeTeam != null ? m.HomeTeam.Name : "Onbekend",
                    AwayTeamName = m.AwayTeam != null ? m.AwayTeam.Name : "Onbekend",
                    Date = m.Datum,
                    ScoreDisplay = isPlayed ? $"{m.HomeScore} - {m.AwayScore}" : "Nog te spelen"
                };
            })
            .OrderBy(m => m.MatchdayNumber)
            .ToList();

            dgTeamMatches.ItemsSource = matchRows;

            var nextMatch = matches
                .Where(m => DateTime.TryParse(m.Datum, out DateTime matchDate) && matchDate.Date >= today && !m.IsPlayed)
                .OrderBy(m => DateTime.Parse(m.Datum))
                .FirstOrDefault();

            if (nextMatch != null)
            {
                lblNextHome.Text = nextMatch.HomeTeam != null ? nextMatch.HomeTeam.Name : "Onbekend";
                lblNextAway.Text = nextMatch.AwayTeam != null ? nextMatch.AwayTeam.Name : "Onbekend";
                lblNextDate.Text = nextMatch.Datum;
                lblNextMatchday.Text = nextMatch.Matchday != null ? $"Matchdag {nextMatch.Matchday.Number}" : "Matchdag -";
            }
            else
            {
                lblNextHome.Text = "Geen aankomende";
                lblNextAway.Text = "Wedstrijden meer";
                lblNextDate.Text = "-";
                lblNextMatchday.Text = "Einde seizoen";
            }

            var allTeams = _context.Teams.ToList();
            var sortedTeams = allTeams
                .OrderByDescending(t => t.Points)
                .ThenByDescending(t => t.GoalsFor - t.GoalsAgainst)
                .ToList();

            int huidigePositie = sortedTeams.FindIndex(t => t.TeamId == _coachTeam.TeamId) + 1;
            int totaalTeams = sortedTeams.Count;

            UpdatePresidentSatisfaction(huidigePositie, totaalTeams);
        }

        #endregion

        #region Navigatie

        private void ShowPanel(StackPanel panelToShow)
        {
            panelTeam.Visibility = Visibility.Collapsed;
            panelMatches.Visibility = Visibility.Collapsed;
            panelStandings.Visibility = Visibility.Collapsed;

            panelToShow.Visibility = Visibility.Visible;
        }

        private void btnNavTeam_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(panelTeam);
            LoadCoachData();
        }

        private void btnNavMatches_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(panelMatches);
            LoadCoachData();
        }

        private void btnNavStandings_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(panelStandings);
            LoadStandings();
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }

        #endregion

        #region Extra Data Laden (Ranglijst & Komende Match)

        private void LoadStandings()
        {
            var sortedTeams = _context.Teams
                .ToList()
                .Select(t => new
                {
                    TeamName = t.Name,
                    Points = t.Points,
                    GoalsFor = t.GoalsFor,
                    GoalsAgainst = t.GoalsAgainst,
                    GoalDifference = t.GoalsFor - t.GoalsAgainst
                })
                .OrderByDescending(t => t.Points)
                .ThenByDescending(t => t.GoalDifference)
                .Select((t, index) => new
                {
                    Position = index + 1,
                    t.TeamName,
                    t.Points,
                    t.GoalsFor,
                    t.GoalsAgainst,
                    t.GoalDifference
                })
                .ToList();

            dgStandings.ItemsSource = sortedTeams;
        }

        private void UpdatePresidentSatisfaction(int positie, int totaalTeams)
        {
            if (positie == 0)
            {
                lblSatisfactionPercentage.Text = "0%";
                barSatisfaction.Width = 0;
                lblPresidentComment.Text = "Er is momenteel geen team gekoppeld om te beoordelen.";
                return;
            }
            double factor = (double)(totaalTeams - positie) / (totaalTeams - 1 == 0 ? 1 : totaalTeams - 1);
            int percentage = (int)(20 + (factor * 80));

            lblSatisfactionPercentage.Text = $"{percentage}%";
            barSatisfaction.Width = (percentage / 100.0) * 615;

            if (percentage >= 75)
            {
                barSatisfaction.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
            }
            else if (percentage >= 50)
            {
                barSatisfaction.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 196, 15));
            }
            else
            {
                barSatisfaction.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
            }

            if (positie == 1)
            {
                lblPresidentComment.Text = "🏆 \"Fantastisch werk! We staan bovenaan de competitie. De hele club spreekt vol lof over jouw tactiek.\"";
            }
            else if (positie <= totaalTeams / 3)
            {
                lblPresidentComment.Text = "📈 \"Ik ben zeer tevreden met onze huidige positie. Ga zo door en we halen Europees voetbal!\"";
            }
            else if (positie <= (totaalTeams / 3) * 2)
            {
                lblPresidentComment.Text = "⚖️ \"De resultaten zijn oké, maar deze club hoort hoger te staan. Er is werk aan de winkel.\"";
            }
            else
            {
                lblPresidentComment.Text = "⚠️ \"Dit kan zo niet langer. De resultaten vallen zwaar tegen en we moeten snel punten gaan pakken!\"";
            }
        }

        #endregion

        #region Foto Uploaden & Verwijderen

        private void btnAddPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg";

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;

                string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProfileImages");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string newFileName = $"user_{_currentUser.UserId}{Path.GetExtension(selectedFilePath)}";
                string destinationPath = Path.Combine(directoryPath, newFileName);

                try
                {
                    imgProfileBrush.ImageSource = null;

                    File.Copy(selectedFilePath, destinationPath, true);

                    var userInDb = _context.Users.FirstOrDefault(u => u.UserId == _currentUser.UserId);
                    if (userInDb != null)
                    {
                        userInDb.ProfileImagePath = destinationPath;
                        _context.SaveChanges();

                        _currentUser.ProfileImagePath = destinationPath;

                        LoadUserProfile();

                        MessageBox.Show("Profielfoto succesvol gewijzigd!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fout bij opslaan foto: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadUserProfile();
                }
            }
        }

        private void btnRemovePhoto_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentUser.ProfileImagePath))
            {
                MessageBox.Show("Je hebt momenteel al de standaard profielfoto.", "Informatie", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show("Weet je zeker dat je je profielfoto wilt verwijderen?", "Foto Verwijderen", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    imgProfileBrush.ImageSource = null;

                    string fotoTeVerwijderen = _currentUser.ProfileImagePath;

                    var userInDb = _context.Users.FirstOrDefault(u => u.UserId == _currentUser.UserId);
                    if (userInDb != null)
                    {
                        userInDb.ProfileImagePath = null;
                        _context.SaveChanges();

                        _currentUser.ProfileImagePath = null;
                        LoadUserProfile();

                        // Optioneel: Verwijder het fysieke bestand ook echt van de schijf
                        if (File.Exists(fotoTeVerwijderen))
                        {
                            File.Delete(fotoTeVerwijderen);
                        }

                        MessageBox.Show("Profielfoto succesvol verwijderd. Standaardfoto is hersteld!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fout bij het verwijderen van de foto: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadUserProfile();
                }
            }
        }

        #endregion
    }
}