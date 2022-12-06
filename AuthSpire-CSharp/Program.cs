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
            name: "authspire",
            userid: "gWrFSRax",
            secret: "OlNH3EUlPjj04HZpwSUDuNQBzAqRNDXs",
            currentVersion: "1.0",
            publicKey: "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAyH0hQIqLCTCx2Fjpk0cxm3qGqN+4z25OAaJdz0u+WFEjasxyx545qTCaB+uz2nVYzlgIAktPfk9qjqngR965iXlHFYjj8gFHc9R2bWrL57IVz78qLQhQ9lpAFYaXxKts9P6ZCEBfg1fBcD330zt834/JflzsJOqHaOJ8b9NQAKGItYnt/W2NCUtE12wRbz4AiXHffDwfk+N5ZJ4HUazc6KxZTnDGvebKcESWjGGQjLz2n6naDRxRFWu/D+bBnIIDrFhvzSLcuyf7w3REroeQh4Woa/S2SL9nekpNZPW0hLIP+5LnowvvxHskQpWAwrsVTW0QDrhAzwnHLUb2MRqPcwIDAQAB\r\n"
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
