using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ProLeagueDesk.Models;

namespace ProLeagueDesk
{
    public partial class RefereeWindow : Window
    {
        private User _currentUser;
        private ProLeagueDeskContext _context;
        private int? _selectedMatchId = null;
        private System.Collections.Generic.HashSet<int> _savedMatchIds = new System.Collections.Generic.HashSet<int>();

        private readonly string _defaultImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image", "default.jpg");

        public RefereeWindow(User user)
        {
            InitializeComponent();
            _currentUser = user;
            _context = new ProLeagueDeskContext();

            LoadUserProfile();
            LoadRefereeData();
            ShowPanel(panelScores);
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

            string imagePathToLoad = null;

            if (!string.IsNullOrEmpty(_currentUser.ProfileImagePath) && File.Exists(_currentUser.ProfileImagePath))
            {
                imagePathToLoad = _currentUser.ProfileImagePath;
            }
            else if (File.Exists(_defaultImagePath))
            {
                imagePathToLoad = _defaultImagePath;
            }

            if (imagePathToLoad != null)
            {
                try
                {
                    imgProfileBrush.ImageSource = ZetFoto(imagePathToLoad);
                }
                catch (Exception ex)
                {
                    imgProfileBrush.ImageSource = null;
                }
            }
            else
            {
                imgProfileBrush.ImageSource = null;
            }
        }

        private void LoadRefereeData()
        {
            var referee = _context.Referees.FirstOrDefault(r => r.UserId == _currentUser.UserId);
            if (referee == null) return;

            var refereeMatches = _context.Matches
                .Include(m => m.Matchday)
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.RefereeId == referee.RefereeId)
                .ToList();

            var vandaag = DateTime.Today;

            var upcomingMatches = refereeMatches
                .Where(m => {
                    if (_savedMatchIds.Contains(m.MatchId))
                    {
                        return false;
                    }

                    if (DateTime.TryParse(m.Datum, out DateTime matchDatum))
                    {
                        return matchDatum.Date >= vandaag && !m.IsPlayed;
                    }
                    return true;
                })
                .Select(m => new
                {
                    m.MatchId,
                    MatchdayNumber = m.Matchday != null ? m.Matchday.Number : 0,
                    HomeTeamName = m.HomeTeam != null ? m.HomeTeam.Name : "Onbekend",
                    AwayTeamName = m.AwayTeam != null ? m.AwayTeam.Name : "Onbekend",
                    Date = m.Datum
                }).ToList();

            dgUpcomingMatches.ItemsSource = upcomingMatches;

            var allMatches = refereeMatches
                .Select(m => {
                    bool isGespeeld = m.IsPlayed || _savedMatchIds.Contains(m.MatchId);
                    if (!isGespeeld && DateTime.TryParse(m.Datum, out DateTime d))
                    {
                        isGespeeld = d.Date < vandaag;
                    }
                    return new
                    {
                        MatchdayNumber = m.Matchday != null ? m.Matchday.Number : 0,
                        HomeTeamName = m.HomeTeam != null ? m.HomeTeam.Name : "Onbekend",
                        AwayTeamName = m.AwayTeam != null ? m.AwayTeam.Name : "Onbekend",
                        Date = m.Datum,
                        ScoreDisplay = !isGespeeld ? "Nog te spelen" : $"{m.HomeScore} - {m.AwayScore}"
                    };
                }).ToList();

            dgAllAssignedMatches.ItemsSource = allMatches;

            var historyMatches = refereeMatches
                .Where(m => {
                    if (m.IsPlayed || _savedMatchIds.Contains(m.MatchId))
                    {
                        return true;
                    }

                    if (DateTime.TryParse(m.Datum, out DateTime matchDatum))
                    {
                        return matchDatum.Date < vandaag;
                    }
                    return false;
                })
                .Select(m => new
                {
                    MatchdayNumber = m.Matchday != null ? m.Matchday.Number : 0,
                    HomeTeamName = m.HomeTeam != null ? m.HomeTeam.Name : "Onbekend",
                    AwayTeamName = m.AwayTeam != null ? m.AwayTeam.Name : "Onbekend",
                    Date = m.Datum,
                    ScoreDisplay = $"{m.HomeScore} - {m.AwayScore}"
                }).ToList();

            dgHistoryMatches.ItemsSource = historyMatches;
        }
        #endregion

        #region Navigatie Buttons

        private void ShowPanel(StackPanel panelToShow)
        {
            panelScores.Visibility = Visibility.Collapsed;
            panelAgenda.Visibility = Visibility.Collapsed;
            panelHistorie.Visibility = Visibility.Collapsed;

            panelToShow.Visibility = Visibility.Visible;
        }

        private void btnNavScores_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(panelScores);
            LoadRefereeData();
        }

        private void btnNavAgenda_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(panelScores);
            LoadRefereeData();
        }

        private void btnNavHistorie_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(panelHistorie);
            LoadRefereeData();
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }

        #endregion

        #region Uitslagen Registreren Logica

        private void dgUpcomingMatches_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedMatch = dgUpcomingMatches.SelectedItem as dynamic;

            if (selectedMatch != null)
            {
                _selectedMatchId = selectedMatch.MatchId;
                lblHomeTeam.Text = selectedMatch.HomeTeamName;
                lblAwayTeam.Text = selectedMatch.AwayTeamName;

                txtHomeScore.Clear();
                txtAwayScore.Clear();
                btnSaveScore.IsEnabled = true;
            }
            else
            {
                _selectedMatchId = null;
                lblHomeTeam.Text = "Thuisteam";
                lblAwayTeam.Text = "Uitteam";
                btnSaveScore.IsEnabled = false;
            }
        }

        private void btnSaveScore_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMatchId == null) return;

            if (string.IsNullOrWhiteSpace(txtHomeScore.Text) || string.IsNullOrWhiteSpace(txtAwayScore.Text))
            {
                MessageBox.Show("Vul alstublieft voor beide teams een score in!", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtHomeScore.Text, out int homeScore) || !int.TryParse(txtAwayScore.Text, out int awayScore))
            {
                MessageBox.Show("Voer geldige numerieke scores in!", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (homeScore < 0 || awayScore < 0)
            {
                MessageBox.Show("Scores kunnen niet negatief zijn!", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int huidigeMatchId = _selectedMatchId.Value;

            var match = _context.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .FirstOrDefault(m => m.MatchId == huidigeMatchId);

            if (match != null)
            {
                match.HomeScore = homeScore;
                match.AwayScore = awayScore;
                match.IsPlayed = true;

                _context.Entry(match).State = EntityState.Modified;

                if (match.HomeTeam != null && match.AwayTeam != null)
                {
                    match.HomeTeam.GoalsFor += homeScore;
                    match.HomeTeam.GoalsAgainst += awayScore;
                    match.AwayTeam.GoalsFor += awayScore;
                    match.AwayTeam.GoalsAgainst += homeScore;

                    if (homeScore > awayScore)
                    {
                        match.HomeTeam.Points += 3;
                    }
                    else if (awayScore > homeScore)
                    {
                        match.AwayTeam.Points += 3;
                    }
                    else
                    {
                        match.HomeTeam.Points += 1;
                        match.AwayTeam.Points += 1;
                    }
                }

                var referee = _context.Referees.FirstOrDefault(r => r.UserId == _currentUser.UserId);
                if (referee != null)
                {
                    referee.MatchesOfficiated += 1;
                    _context.Entry(referee).State = EntityState.Modified;
                }

                _savedMatchIds.Add(huidigeMatchId);

                _context.SaveChanges();

                MessageBox.Show("Uitslag succesvol opgeslagen en klassement bijgewerkt!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                btnSaveScore.IsEnabled = false;
                lblHomeTeam.Text = "Thuisteam";
                lblAwayTeam.Text = "Uitteam";
                txtHomeScore.Clear();
                txtAwayScore.Clear();

                dgUpcomingMatches.SelectedItem = null;
                _selectedMatchId = null;

                LoadRefereeData();
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