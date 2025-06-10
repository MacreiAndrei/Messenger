using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Messenger.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void DeleteAllConversations_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Ești sigur că vrei să ștergi toate conversațiile?\n\nAceastă acțiune nu poate fi anulată!",
                "Confirmare ștergere",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                // Aici ar fi logica pentru ștergerea conversațiilor
                MessageBox.Show(
                    "Toate conversațiile au fost șterse.",
                    "Conversații șterse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "ATENȚIE: Vrei să ștergi definitiv contul?\n\n" +
                "Această acțiune va șterge:\n" +
                "• Toate conversațiile tale\n" +
                "• Toate datele personale\n" +
                "• Accesul la aplicație\n\n" +
                "Această acțiune NU poate fi anulată!",
                "Confirmare ștergere cont",
                MessageBoxButton.YesNo,
                MessageBoxImage.Stop
            );

            if (result == MessageBoxResult.Yes)
            {
                // Confirmare suplimentară
                var finalConfirm = MessageBox.Show(
                    "Ultima confirmare: Ștergi definitiv contul?",
                    "Confirmare finală",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Stop
                );

                if (finalConfirm == MessageBoxResult.Yes)
                {
                    // Aici ar fi logica pentru ștergerea contului
                    MessageBox.Show(
                        "Contul a fost șters. Aplicația se va închide.",
                        "Cont șters",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    // Închide aplicația
                    Application.Current.Shutdown();
                }
            }
        }
    }
}