using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fclp;

namespace AvanteamLdapConnexion
{
    class Program
    {
        static void Main(string[] args)
        {
            Config.Path = ConfigurationManager.AppSettings["path"];
            Config.Filter = ConfigurationManager.AppSettings["filter"];
            Config.Attributs.AddRange(ConfigurationManager.AppSettings["attributs"].Split(','));

            // create a generic parser for the ApplicationArguments type
            var parser = new FluentCommandLineParser<ApplicationArguments>();
            parser
                .Setup(arg => arg.Login)
                .As('l', "login");
            parser
                .Setup(arg => arg.Password)
                .As('p', "password");
            var result = parser.Parse(args);
            if (result.HasErrors)
            {
                Console.Write(result.ErrorText);
                return;
            }

            Config.Login = parser.Object.Login;
            Config.Password= parser.Object.Password;

            Console.Write(Config.ToWrite());
            Application.Run();
        }
    }
}
