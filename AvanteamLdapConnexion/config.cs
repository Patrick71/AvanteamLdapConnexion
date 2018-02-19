using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvanteamLdapConnexion
{
    /// <summary>
    /// Configuration LDAP
    /// </summary>
    public static  class Config
    {
        public static string Path { get; set; }
        public static string Login { get; set; }
        public static string Password { get; set; }
        public static  string Filter { get; set; }
        public static readonly List<string> Attributs = new List<string>();

        public static string ToWrite()
        {
            var attributs = string.Join(", ", Attributs);
            return 
                "\tPath: " + Path + "\n" +
                "\tFilter: " + Filter + "\n" +
                "\tLogin: " + Login + "\n" +
                "\tPassword: " + Password + "\n" +
                "\tAttributs: " + attributs + "\n";
        }
    }
}
