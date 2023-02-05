using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AuthSpire_CSharp
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        public static API authSpire = new API(
            name: "",
            userid: "",
            secret: "",
            currentVersion: "1.0",
            publicKey: ""
        );


        [STAThread]
        static void Main()
        {
            // Initialize app
            authSpire.InitApp();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Login());
        }
    }
}
