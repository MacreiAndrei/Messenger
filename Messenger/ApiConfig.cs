using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    public static class ApiConfig
    {
        // Schimbă doar aici IP-ul și se va actualiza în toată aplicația
        public const string BASE_URL = "https://172.16.0.196:5172";



        // Endpoint-urile API
        public static class Endpoints
        {
            // Autentificare
            public static string Login => $"{BASE_URL}/Autentification/Login";
            public static string TokenValidation => $"{BASE_URL}/Autentification/Token";
            public static string Register => $"{BASE_URL}/Autentification/Register";

            // Utilizatori
            public static string GetAllUsers => $"{BASE_URL}/User/GetAllUsers";
            public static string GetUserProfile => $"{BASE_URL}/User/GetProfile";
            public static string UpdateUserStatus => $"{BASE_URL}/User/UpdateStatus";

            // Chat
            public static string GetOrCreateChat => $"{BASE_URL}/Chat/GetOrCreateChat";
            public static string GetUserChats => $"{BASE_URL}/Chat/GetUserChats";

          
        }
    }
}