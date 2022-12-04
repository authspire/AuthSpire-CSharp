using System;

namespace AuthSpire_CSharp_Console
{
    internal class Program
    {
        public static void Logo()
        {
            string logo = @"
Yb  dP                        db              
 YbdP  .d8b. 8   8 8d8b      dPYb   88b. 88b. 
  YP   8' .8 8b d8 8P       dPwwYb  8  8 8  8 
  88   `Y8P' `Y8P8 8       dP    Yb 88P' 88P' 
                                    8    8    
";

            Console.Write(logo);
        }



        public static API authSpire = new API(
            name: "",
            userid: "",
            secret: "",
            currentVersion: "1.0",
            publicKey: ""
        );

        static void Main()
        {
            // Step 1. Initializing your application

            authSpire.InitApp();

            Logo();

            Console.WriteLine("[1] Register");
            Console.WriteLine("[2] Login");
            Console.WriteLine("[3] License only");
            Console.WriteLine("[4] Add Log");

            Console.Write(">> ");
            string option = Console.ReadLine();
            Console.WriteLine();

            if (option == "1")
            {
                Console.Write("Username: ");
                string username = Console.ReadLine();
                Console.Write("Password: ");
                string password = Console.ReadLine();
                Console.Write("License: ");
                string license = Console.ReadLine();
                Console.Write("Email: ");
                string email = Console.ReadLine();

                bool registered = authSpire.Register(username, password, license, email);
                if (registered)
                {
                    Console.WriteLine("Thanks for registering!");
                }
            }
            else if (option == "2")
            {
                Console.Write("Username: ");
                string username = Console.ReadLine();
                Console.Write("Password: ");
                string password = Console.ReadLine();

                bool loggedIn = authSpire.Login(username, password);
                if (loggedIn)
                {
                    Console.WriteLine("Welcome back " + authSpire.user.Username);
                    Console.WriteLine();
                    Console.WriteLine(authSpire.user.Email);
                    Console.WriteLine(authSpire.user.IP);
                    Console.WriteLine(authSpire.user.Expires);
                    Console.WriteLine(authSpire.user.HWID);
                    Console.WriteLine(authSpire.user.Last_Login);
                    Console.WriteLine(authSpire.user.Created_At);
                    Console.WriteLine(authSpire.user.Variable);
                    Console.WriteLine(authSpire.user.Level);
                }
            }
            else if (option == "3")
            {
                Console.Write("License: ");
                string license = Console.ReadLine();


                bool loggedIn = authSpire.LicenseOnly(license);
                if (loggedIn)
                {
                    Console.WriteLine("Welcome back " + authSpire.user.Username);
                    Console.WriteLine();
                    Console.WriteLine(authSpire.user.Email);
                    Console.WriteLine(authSpire.user.IP);
                    Console.WriteLine(authSpire.user.Expires);
                    Console.WriteLine(authSpire.user.HWID);
                    Console.WriteLine(authSpire.user.Last_Login);
                    Console.WriteLine(authSpire.user.Created_At);
                    Console.WriteLine(authSpire.user.Variable);
                    Console.WriteLine(authSpire.user.Level);
                }
            }
            else if (option == "4")
            {
                Console.Write("Username: ");
                string username = Console.ReadLine();
                Console.Write("Action: ");
                string action = Console.ReadLine();

                authSpire.AddLog(username, action);
                Console.WriteLine("Log added!");
            }
            Console.Read();
        }
    }
}
