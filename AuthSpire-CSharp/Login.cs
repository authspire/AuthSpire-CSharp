using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AuthSpire_CSharp.API;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace AuthSpire_CSharp
{
    public partial class Login : Form
    {
       

        public Login()
        {
            InitializeComponent();
        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            bool loggedIn = Program.authSpire.Login(textBoxUsername.Text, textBoxPassword.Text);
            if (loggedIn)
            {
                Main main = new Main();
                main.Show();
                this.Hide();
            }
        }

        private void linkLabelRegister_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Register register = new Register();
            register.Show();
            this.Hide();
        }

        private void Login_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void linkLabelLicenseOnly_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            License license = new License();
            license.Show();
            this.Hide();
        }
    }
}
