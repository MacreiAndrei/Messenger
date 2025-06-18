using Messenger.Models;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Messenger
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string CurrentSessionToken { get; set; }
        public static User CurrentUser { get; set; }
    }

}
