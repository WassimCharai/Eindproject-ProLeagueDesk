using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ProLeagueDesk.Models;

namespace ProLeagueDesk
{
    public partial class AdminWindow : Window
    {
        #region Private Fields & Constructor
        private User _user;
        private List<MatchPreviewItem> _generatedMatches = new List<MatchPreviewItem>();

        public AdminWindow(User user)
        {
            InitializeComponent();
            _user = user;
            txtAdminInfo.Text = $"Welkom, {_user.Username}!";

            LaadProfielFoto();
            LaadTeams();
            LaadSeizoenen();
            LaadScheidsrechters();
            LaadCoaches();
        }
        #endregion

        #region Profile Foto Methodes
        private void ZetFoto(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                imgProfileBrush.ImageSource = null;
                return;
            }

            try
            {
                BitmapImage bitmap = new BitmapImage();
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                }

                bitmap.Freeze();
                imgProfileBrush.ImageSource = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij laden afbeelding: {ex.Message}");
            }
        }

        private void LaadProfielFoto()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image");
            string defaultPath = Path.Combine(folder, "default.jpg");

            if (!string.IsNullOrEmpty(_user.ProfileImagePath) && File.Exists(_user.ProfileImagePath))
            {
                ZetFoto(_user.ProfileImagePath);
            }
            else
            {
                ZetFoto(defaultPath);
            }
        }
        #endregion

        #region Data Laden
        private void LaadTeams()
        {
            using (var context = new ProLeagueDeskContext())
            {
                if (!context.Teams.Any())
                {
                    var testTeams = new List<Team>
                    {
                        new Team { Name = "Club Brugge", Points = 0, GoalsFor = 0, GoalsAgainst = 0 },
                        new Team { Name = "RSC Anderlecht", Points = 0, GoalsFor = 0, GoalsAgainst = 0 },
                        new Team { Name = "KRC Genk", Points = 0, GoalsFor = 0, GoalsAgainst = 0 },
                        new Team { Name = "Antwerp FC", Points = 0, GoalsFor = 0, GoalsAgainst = 0 },
                        new Team { Name = "AA Gent", Points = 0, GoalsFor = 0, GoalsAgainst = 0 },
                        new Team { Name = "Union SG", Points = 0, GoalsFor = 0, GoalsAgainst = 0 }
                    };

                    context.Teams.AddRange(testTeams);
                    context.SaveChanges();
                }
                var alleTeams = context.Teams.ToList();

                lbTeams.ItemsSource = alleTeams;
                lbTeams.DisplayMemberPath = "Name";

                cbTeamsCoach.ItemsSource = alleTeams;
                cbTeamsCoach.DisplayMemberPath = "Name";
            }
        }

        private void LaadCoaches()
        {
            using (var context = new ProLeagueDeskContext())
            {
                var lijstMetCoaches = context.Users.Where(u => u.Role == "Coach").ToList();

                cbCoaches.ItemsSource = lijstMetCoaches;
                cbCoaches.DisplayMemberPath = "Username";
            }
        }

        private void LaadGebruikers()
        {
            using (var context = new ProLeagueDeskContext())
            {
                dgUsers.ItemsSource = context.Users.ToList();
            }
        }

        private void LaadSeizoenen()
        {
            List<string> seizoenen = new List<string>
            {
                "2025-2026",
                "2026-2027",
                "2027-2028"
            };
            cbSeason.ItemsSource = seizoenen;
            cbSeason.SelectedIndex = 0;
        }

        private void LaadScheidsrechters()
        {
            try
            {
                using (var context = new ProLeagueDeskContext())
                {
                    var alleScheidsrechters = context.Referees.ToList();

                    cbReferees.ItemsSource = alleScheidsrechters;
                    cbReferees.DisplayMemberPath = "Name";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij het laden van scheidsrechters: {ex.Message}", "Fout");
            }
        }
        #endregion

        #region Navigatie & Panel Switch
        private void btnGenereer_Click(object sender, RoutedEventArgs e)
        {
            panelSpeelschema.Visibility = Visibility.Visible;
            panelGebruikers.Visibility = Visibility.Collapsed;
            panelStatistieken.Visibility = Visibility.Collapsed;

            LaadScheidsrechters();
        }

        private void btnBeheer_Click(object sender, RoutedEventArgs e)
        {
            panelSpeelschema.Visibility = Visibility.Collapsed;
            panelGebruikers.Visibility = Visibility.Visible;
            panelStatistieken.Visibility = Visibility.Collapsed;

            LaadGebruikers();
        }

        private void btnStats_Click(object sender, RoutedEventArgs e)
        {
            panelSpeelschema.Visibility = Visibility.Collapsed;
            panelGebruikers.Visibility = Visibility.Collapsed;
            panelStatistieken.Visibility = Visibility.Visible;

            using (var context = new ProLeagueDeskContext())
            {
                var ranglijst = context.Teams.ToList()
                    .OrderByDescending(t => t.Points)
                    .ThenByDescending(t => t.GoalsFor - t.GoalsAgainst)
                    .Select((t, index) => new
                    {
                        Positie = index + 1,
                        Team = t.Name,
                        Punten = t.Points,
                        DV = t.GoalsFor,
                        DT = t.GoalsAgainst,
                        Saldo = t.GoalsFor - t.GoalsAgainst
                    })
                    .ToList();

                dgStats.ItemsSource = ranglijst;
            }
        }
        #endregion

        #region Account & Photo Management
        private void btnAddPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Image files (*.jpg;*.png)|*.jpg;*.png";

            if (dlg.ShowDialog() == true)
            {
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image");
                Directory.CreateDirectory(folder);

                string fileName = Path.GetFileName(dlg.FileName);
                string newPath = Path.Combine(folder, fileName);

                File.Copy(dlg.FileName, newPath, true);

                using (var context = new ProLeagueDeskContext())
                {
                    var user = context.Users.FirstOrDefault(u => u.UserId == _user.UserId);
                    if (user != null)
                    {
                        user.ProfileImagePath = newPath;
                        context.SaveChanges();
                        _user.ProfileImagePath = newPath;
                    }
                }
                ZetFoto(newPath);
            }
        }

        private void btnRemovePhoto_Click(object sender, RoutedEventArgs e)
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image");
            string defaultPath = Path.Combine(folder, "default.jpg");

            using (var context = new ProLeagueDeskContext())
            {
                var user = context.Users.FirstOrDefault(u => u.UserId == _user.UserId);
                if (user != null)
                {
                    user.ProfileImagePath = null;
                    context.SaveChanges();
                    _user.ProfileImagePath = null;
                }
            }
            ZetFoto(defaultPath);
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Weet je zeker dat je wilt uitloggen?",
                "Bevestiging",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                MainWindow mw = new MainWindow();
                mw.Show();
                this.Close();
            }
        }
        #endregion

        #region Team Management
        private void btnAddTeam_Click(object sender, RoutedEventArgs e)
        {
            string teamName = Microsoft.VisualBasic.Interaction.InputBox("Voer de naam van het nieuwe team in:", "Team Toevoegen", "");

            if (string.IsNullOrWhiteSpace(teamName))
            {
                return;
            }

            using (var context = new ProLeagueDeskContext())
            {
                if (context.Teams.Any(t => t.Name.ToLower() == teamName.ToLower()))
                {
                    MessageBox.Show("Dit team bestaat al!", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Team nieuwTeam = new Team { Name = teamName, Points = 0, GoalsFor = 0, GoalsAgainst = 0 };
                context.Teams.Add(nieuwTeam);
                context.SaveChanges();
            }

            MessageBox.Show($"{teamName} is succesvol toegevoegd!", "Succes");
            LaadTeams();
        }

        private void btnRemoveTeam_Click(object sender, RoutedEventArgs e)
        {
            if (lbTeams.SelectedItem == null)
            {
                MessageBox.Show("Selecteer eerst een team uit de lijst om te verwijderen.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var geselecteerdTeam = (Team)lbTeams.SelectedItem;

            var result = MessageBox.Show($"Weet je zeker dat je {geselecteerdTeam.Name} wilt verwijderen?",
                                         "Team Verwijderen", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.No) return;

            try
            {
                using (var context = new ProLeagueDeskContext())
                {
                    bool heeftWedstrijden = context.Matches.Any(m => m.HomeTeamId == geselecteerdTeam.TeamId || m.AwayTeamId == geselecteerdTeam.TeamId);

                    if (heeftWedstrijden)
                    {
                        MessageBox.Show($"Kan {geselecteerdTeam.Name} niet verwijderen omdat er nog wedstrijden voor dit team in het speelschema staan. " +
                                        "Wis eerst het speelschema via de knop 'Huidig Schema Wissen'.",
                                        "Verwijderen Mislukt", MessageBoxButton.OK, MessageBoxImage.Stop);
                        return;
                    }

                    var dbTeam = context.Teams.FirstOrDefault(t => t.TeamId == geselecteerdTeam.TeamId);
                    if (dbTeam != null)
                    {
                        context.Teams.Remove(dbTeam);
                        context.SaveChanges();

                        MessageBox.Show($"{dbTeam.Name} is succesvol verwijderd.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                LaadTeams();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Er ging iets mis bij het verwijderen van het team: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region User & Role Management
        private void btnChangeRole_Click(object sender, RoutedEventArgs e)
        {
            if (!(dgUsers.SelectedItem is User geselecteerdeUser))
            {
                MessageBox.Show("Selecteer eerst een gebruiker uit de lijst.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string gekozenRol = (cbNewRole.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (string.IsNullOrEmpty(gekozenRol))
            {
                MessageBox.Show("Selecteer eerst een nieuwe rol uit de keuzelijst.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new ProLeagueDeskContext())
                {
                    var dbUser = context.Users
                        .Include(u => u.Referees)
                        .Include(u => u.Coaches)
                        .Include(u => u.Admins)
                        .FirstOrDefault(u => u.UserId == geselecteerdeUser.UserId);

                    if (dbUser == null)
                    {
                        MessageBox.Show("Gebruiker niet gevonden in de database.", "Fout");
                        return;
                    }

                    dbUser.Role = gekozenRol;

                    if (gekozenRol == "Scheidsrechter")
                    {
                        if (!dbUser.Referees.Any())
                        {
                            Referee nieuweReferee = new Referee()
                            {
                                Name = dbUser.Username,
                                MatchesOfficiated = 0
                            };
                            dbUser.Referees.Add(nieuweReferee);
                        }
                    }
                    else if (gekozenRol == "Coach")
                    {
                        if (!dbUser.Coaches.Any())
                        {
                            Coach nieuweCoach = new Coach()
                            {
                                Name = dbUser.Username,
                                PhoneNumber = 0
                            };
                            dbUser.Coaches.Add(nieuweCoach);
                        }
                    }
                    else if (gekozenRol == "Admin")
                    {
                        if (!dbUser.Admins.Any())
                        {
                            Admin nieuweAdmin = new Admin()
                            {
                                AdminNumber = 1
                            };
                            dbUser.Admins.Add(nieuweAdmin);
                        }
                    }
                    context.SaveChanges();
                    LaadScheidsrechters();
                    LaadGebruikers();

                    MessageBox.Show($"Rol succesvol gewijzigd naar {gekozenRol} en het gekoppelde record is nu écht aangemaakt in de database!",
                                    "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                string foutBericht = ex.Message;
                if (ex.InnerException != null)
                {
                    foutBericht += "\n\nDetails: " + ex.InnerException.Message;
                }
                MessageBox.Show($"Database fout: {foutBericht}", "Fout bij opslaan", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (!(dgUsers.SelectedItem is User geselecteerdeUser))
            {
                MessageBox.Show("Selecteer eerst een gebruiker uit de lijst.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var resultaat = MessageBox.Show($"Weet je zeker dat je '{geselecteerdeUser.Username}' wilt verwijderen? Dit verwijdert ook diens rol als eventuele scheidsrechter of coach.",
                                            "Gebruiker Verwijderen", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultaat != MessageBoxResult.Yes) return;

            try
            {
                using (var context = new ProLeagueDeskContext())
                {
                    var dbUser = context.Users
                        .Include(u => u.Referees)
                        .Include(u => u.Coaches)
                        .Include(u => u.Admins)
                        .FirstOrDefault(u => u.UserId == geselecteerdeUser.UserId);

                    if (dbUser != null)
                    {
                        if (dbUser.Referees.Any())
                        {
                            context.Referees.RemoveRange(dbUser.Referees);
                        }
                        if (dbUser.Coaches.Any())
                        {
                            context.Coaches.RemoveRange(dbUser.Coaches);
                        }
                        if (dbUser.Admins.Any())
                        {
                            context.Admins.RemoveRange(dbUser.Admins);
                        }
                        context.Users.Remove(dbUser);

                        context.SaveChanges();

                        dgUsers.ItemsSource = context.Users.ToList();

                        if (panelSpeelschema.Visibility == Visibility.Visible)
                        {
                            LaadScheidsrechters();
                        }
                        MessageBox.Show("Gebruiker en alle gekoppelde rol-gegevens zijn succesvol verwijderd!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                string foutBericht = ex.Message;
                if (ex.InnerException != null)
                {
                    foutBericht += "\n\nDetails: " + ex.InnerException.Message;
                }
                MessageBox.Show($"Fout bij het verwijderen: {foutBericht}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Coach & Referee Toewijzing
        private void btnAssignCoach_Click(object sender, RoutedEventArgs e)
        {
            if (!(cbTeamsCoach.SelectedItem is Team geselecteerdTeam))
            {
                MessageBox.Show("Selecteer eerst een team uit de keuzelijst.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!(cbCoaches.SelectedItem is User geselecteerdeGebruiker))
            {
                MessageBox.Show("Selecteer eerst een coach uit de keuzelijst.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new ProLeagueDeskContext())
                {
                    var vorigTeamVanCoach = context.Teams.FirstOrDefault(t => t.CoachId == geselecteerdeGebruiker.UserId);
                    if (vorigTeamVanCoach != null)
                    {
                        vorigTeamVanCoach.CoachId = null;
                    }
                    var dbTeam = context.Teams.FirstOrDefault(t => t.TeamId == geselecteerdTeam.TeamId);
                    if (dbTeam != null)
                    {
                        dbTeam.CoachId = geselecteerdeGebruiker.UserId;

                        context.SaveChanges();

                        MessageBox.Show($"'{geselecteerdeGebruiker.Username}' is nu succesvol ingesteld als de coach van {dbTeam.Name}!",
                                        "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                LaadTeams();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Er ging iets mis bij het wijzigen van de coach: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRemoveCoach_Click(object sender, RoutedEventArgs e)
        {
            if (!(cbTeamsCoach.SelectedItem is Team geselecteerdTeam))
            {
                MessageBox.Show("Selecteer eerst het team waarvan je de coach wilt ontkoppelen.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Weet je zeker dat je de huidige coach wilt verwijderen van {geselecteerdTeam.Name}?",
                                         "Coach Ontkoppelen", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.No) return;

            try
            {
                using (var context = new ProLeagueDeskContext())
                {
                    var dbTeam = context.Teams.FirstOrDefault(t => t.TeamId == geselecteerdTeam.TeamId);
                    if (dbTeam != null)
                    {
                        dbTeam.CoachId = null;
                        context.SaveChanges();

                        MessageBox.Show($"De coach is succesvol ontkoppeld van {dbTeam.Name}.",
                                        "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                LaadTeams();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Er ging iets mis bij het ontkoppelen: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAssignReferee_Click(object sender, RoutedEventArgs e)
        {
            if (dgPreview.SelectedItem == null)
            {
                MessageBox.Show("Selecteer eerst een wedstrijd uit het speelschema.", "Toewijzing Mislukt", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!(cbReferees.SelectedItem is Referee geselecteerdeReferee))
            {
                MessageBox.Show("Selecteer eerst een scheidsrechter uit de keuzelijst.", "Toewijzing Mislukt", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new ProLeagueDeskContext())
                {
                    if (dgPreview.SelectedItem is MatchPreviewItem previewItem)
                    {
                        var dbMatch = context.Matches.FirstOrDefault(m =>
                            m.HomeTeamId == previewItem.HomeTeamId &&
                            m.AwayTeamId == previewItem.AwayTeamId &&
                            m.Datum == previewItem.Date);

                        if (dbMatch != null)
                        {
                            dbMatch.RefereeId = geselecteerdeReferee.RefereeId;
                            context.SaveChanges();

                            previewItem.RefereeName = geselecteerdeReferee.Name;
                            dgPreview.Items.Refresh();

                            MessageBox.Show($"Scheidsrechter '{geselecteerdeReferee.Name}' is succesvol toegewezen!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Deze specifieke wedstrijd kon niet worden gevonden in de database. Mogelijk is het schema nog niet opgeslagen.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else if (dgPreview.SelectedItem is Match geselecteerdeMatch)
                    {
                        var dbMatch = context.Matches.FirstOrDefault(m => m.MatchId == geselecteerdeMatch.MatchId);
                        if (dbMatch != null)
                        {
                            dbMatch.RefereeId = geselecteerdeReferee.RefereeId;
                            context.SaveChanges();

                            geselecteerdeMatch.RefereeId = geselecteerdeReferee.RefereeId;
                            geselecteerdeMatch.Referee = geselecteerdeReferee;
                            dgPreview.Items.Refresh();

                            MessageBox.Show($"Scheidsrechter '{geselecteerdeReferee.Name}' is succesvol toegewezen!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij het toewijzen van de scheidsrechter: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Schema Genereren & Export
        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            using (var context = new ProLeagueDeskContext())
            {
                var teams = context.Teams.ToList();

                if (teams.Count < 2)
                {
                    MessageBox.Show("Er moeten minstens 2 teams in de database staan om een schema te genereren!",
                                    "Te weinig teams", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (teams.Count % 2 != 0)
                {
                    MessageBox.Show($"Je hebt momenteel een oneven aantal teams ({teams.Count}). " +
                                    "Voeg eerst nog een team toe (of verwijder er één) zodat het aantal even is voordat je het speelschema genereert!",
                                    "Oneven aantal teams", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Random rng = new Random();
                teams = teams.OrderBy(t => rng.Next()).ToList();

                DateTime startDate = dpStartDate.SelectedDate ?? DateTime.Now;

                int numTeams = teams.Count;
                int numDaysHeen = numTeams - 1;
                int numMatchesPerDay = numTeams / 2;

                _generatedMatches.Clear();

                for (int day = 0; day < numDaysHeen; day++)
                {
                    DateTime matchDate = startDate.AddDays(day * 7);
                    string formattedDate = matchDate.ToString("dd-MM-yyyy");

                    for (int matchIdx = 0; matchIdx < numMatchesPerDay; matchIdx++)
                    {
                        int homeIdx = (day + matchIdx) % (numTeams - 1);
                        int awayIdx = (numTeams - 1 - matchIdx + day) % (numTeams - 1);

                        if (matchIdx == 0)
                        {
                            awayIdx = numTeams - 1;
                        }

                        var homeTeam = teams[homeIdx];
                        var awayTeam = teams[awayIdx];

                        if (homeTeam.TeamId != -1 && awayTeam.TeamId != -1)
                        {
                            if (day % 2 == 1)
                            {
                                var temp = homeTeam;
                                homeTeam = awayTeam;
                                awayTeam = temp;
                            }

                            _generatedMatches.Add(new MatchPreviewItem
                            {
                                MatchdayNumber = day + 1,
                                HomeTeamName = homeTeam.Name,
                                AwayTeamName = awayTeam.Name,
                                HomeTeamId = homeTeam.TeamId,
                                AwayTeamId = awayTeam.TeamId,
                                Date = formattedDate
                            });
                        }
                    }
                }
                for (int day = 0; day < numDaysHeen; day++)
                {
                    int terugrondeDagIndex = numDaysHeen + day;
                    DateTime matchDate = startDate.AddDays(terugrondeDagIndex * 7);
                    string formattedDate = matchDate.ToString("dd-MM-yyyy");

                    var heenMatchenVanDezeDag = _generatedMatches.Where(m => m.MatchdayNumber == day + 1).ToList();

                    foreach (var heenMatch in heenMatchenVanDezeDag)
                    {
                        _generatedMatches.Add(new MatchPreviewItem
                        {
                            MatchdayNumber = terugrondeDagIndex + 1,
                            HomeTeamName = heenMatch.AwayTeamName,
                            AwayTeamName = heenMatch.HomeTeamName,
                            HomeTeamId = heenMatch.AwayTeamId,
                            AwayTeamId = heenMatch.HomeTeamId,
                            Date = formattedDate
                        });
                    }
                }
                _generatedMatches = _generatedMatches.OrderBy(m => m.MatchdayNumber).ToList();

                dgPreview.ItemsSource = null;
                dgPreview.ItemsSource = _generatedMatches;

                MessageBox.Show($"Speelschema succesvol gegenereerd met {_generatedMatches.Count} wedstrijden!");
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_generatedMatches == null || !_generatedMatches.Any())
            {
                MessageBox.Show("Er is geen gegenereerd schema om op te slaan. Klik eerst op 'Genereer Speelschema'.");
                return;
            }

            try
            {
                using (var context = new ProLeagueDeskContext())
                {
                    context.Matches.RemoveRange(context.Matches);
                    context.MatchDays.RemoveRange(context.MatchDays);

                    foreach (var team in context.Teams)
                    {
                        team.Points = 0;
                        team.GoalsFor = 0;
                        team.GoalsAgainst = 0;
                    }

                    context.SaveChanges();

                    var groupedByDay = _generatedMatches.GroupBy(m => m.MatchdayNumber);

                    foreach (var group in groupedByDay)
                    {
                        var matchDay = context.MatchDays.FirstOrDefault(md => md.Number == group.Key);
                        if (matchDay == null)
                        {
                            matchDay = new MatchDay { Number = group.Key };
                            context.MatchDays.Add(matchDay);
                        }

                        foreach (var item in group)
                        {
                            Match nieuweMatch = new Match
                            {
                                Matchday = matchDay,
                                HomeTeamId = item.HomeTeamId,
                                AwayTeamId = item.AwayTeamId,
                                Datum = item.Date,
                                HomeScore = 0,
                                AwayScore = 0,
                                RefereeId = null
                            };

                            context.Matches.Add(nieuweMatch);
                        }
                    }

                    context.SaveChanges();
                    MessageBox.Show("Het speelschema is succesvol opgeslagen in de database!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                    var opgeslagenWedstrijden = context.Matches
                        .Include(m => m.HomeTeam)
                        .Include(m => m.AwayTeam)
                        .Include(m => m.Referee)
                        .Include(m => m.Matchday)
                        .ToList();

                    dgPreview.ItemsSource = null;

                    var geupdateLijst = opgeslagenWedstrijden.Select(m => new MatchPreviewItem
                    {
                        MatchdayNumber = m.Matchday?.Number ?? 0,
                        HomeTeamName = m.HomeTeam?.Name ?? "Onbekend",
                        AwayTeamName = m.AwayTeam?.Name ?? "Onbekend",
                        HomeTeamId = m.HomeTeamId ?? 0,
                        AwayTeamId = m.AwayTeamId ?? 0,
                        Date = m.Datum,
                        RefereeName = m.Referee?.Name ?? "Geen scheidsrechter"
                    }).ToList();

                    dgPreview.ItemsSource = geupdateLijst;
                    _generatedMatches.Clear();
                }
            }
            catch (Exception ex)
            {
                string foutBericht = ex.Message;
                if (ex.InnerException != null)
                {
                    foutBericht += "\n\nDetails: " + ex.InnerException.Message;
                }
                MessageBox.Show($"Er ging iets mis bij het opslaan: {foutBericht}");
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (_generatedMatches == null || !_generatedMatches.Any())
            {
                MessageBox.Show("Er is geen gegenereerd speelschema om te exporteren!", "Geen data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            saveFileDialog.FileName = $"Speelschema_{cbSeason.SelectedItem?.ToString() ?? "Seizoen"}.txt";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                    {
                        writer.WriteLine($"=========================================");
                        writer.WriteLine($" SPEELSCHEMA - SEIZOEN {cbSeason.SelectedItem}");
                        writer.WriteLine($" Gegenereerd door: {_user.Username}");
                        writer.WriteLine($" Datum van export: {DateTime.Now.ToString("dd-MM-yyyy HH:mm")}");
                        writer.WriteLine($"=========================================\n");

                        var groupedMatches = _generatedMatches.GroupBy(m => m.MatchdayNumber);

                        foreach (var speeldag in groupedMatches)
                        {
                            writer.WriteLine($"--- SPEELDAG {speeldag.Key} ({speeldag.First().Date}) ---");
                            foreach (var match in speeldag)
                            {
                                writer.WriteLine($"    {match.HomeTeamName} vs {match.AwayTeamName}");
                            }
                            writer.WriteLine();
                        }
                    }

                    MessageBox.Show("Speelschema succesvol geëxporteerd!", "Export Geslaagd", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fout bij het exporteren: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnClearSchedule_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Weet je zeker dat je het VOLLEDIGE huidige speelschema wilt verwijderen? Dit kan niet ongedaan worden gemaakt.",
                                         "Speelschema Wissen", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.No) return;

            try
            {
                using (var context = new ProLeagueDeskContext())
                {
                    if (!context.Matches.Any())
                    {
                        MessageBox.Show("Er is momenteel geen speelschema om te verwijderen.", "Informatie", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    context.Matches.RemoveRange(context.Matches);
                    context.MatchDays.RemoveRange(context.MatchDays);

                    foreach (var team in context.Teams)
                    {
                        team.Points = 0;
                        team.GoalsFor = 0;
                        team.GoalsAgainst = 0;
                    }

                    context.SaveChanges();

                    dgPreview.ItemsSource = null;
                    dgStats.ItemsSource = null;

                    MessageBox.Show("Het speelschema is succesvol volledig gewist en de teamstatistieken zijn gereset!",
                                    "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Er ging iets mis bij het wissen van het schema: {ex.Message}",
                                "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Nested Class
        public class MatchPreviewItem
        {
            public int MatchdayNumber { get; set; }
            public string HomeTeamName { get; set; }
            public string AwayTeamName { get; set; }
            public string Date { get; set; }
            public int HomeTeamId { get; set; }
            public int AwayTeamId { get; set; }
            public string RefereeName { get; set; }
            public string ScoreDisplay { get; set; }
        }
        #endregion
    }
}