using System.Linq;
using System.Windows;
using ProLeagueDesk.Models;

namespace ProLeagueDesk
{
    public partial class AdminCheckWindow : Window
    {
        public bool IsValid { get; private set; } = false;

        public AdminCheckWindow() => InitializeComponent();

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            string ingevoerdWachtwoord = tbAdminPass.Password.Trim();

            using (var context = new ProLeagueDeskContext())
            {
                var adminSetting = context.AppSettings.FirstOrDefault(s => s.Key == "AdminGatekeeperHash");

                if (adminSetting != null && BCrypt.Net.BCrypt.Verify(ingevoerdWachtwoord, adminSetting.Value))
                {
                    IsValid = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Fout wachtwoord! Je hebt geen toegang om een admin-account aan te maken.", "Toegang Geweigerd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}