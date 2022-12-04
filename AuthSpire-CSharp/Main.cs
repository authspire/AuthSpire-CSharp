using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AuthSpire_CSharp
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            labelWelcomeBack.Text = "Welcome back " + Program.authSpire.user.Username + "!";

            richTextBox1.Text = String.Format(@"
Email: {0}
IP: {1}
Expires: {2}
HWID: {3}
Last-Login: {4}
Created-At: {5}
Variable: {6}
Level: {7}



",Program.authSpire.user.Email, Program.authSpire.user.IP, Program.authSpire.user.Expires, Program.authSpire.user.HWID, Program.authSpire.user.Last_Login,
Program.authSpire.user.Created_At, Program.authSpire.user.Variable, Program.authSpire.user.Level);
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
