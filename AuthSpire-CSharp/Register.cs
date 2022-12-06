using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace AuthSpire_CSharp
{
    public partial class Register : Form
    {
        public Register()
        {
            InitializeComponent();
        }

        private void linkLabelLogin_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Login login = new Login();
            this.Hide();
            login.Show();
        }

        private void buttonRegister_Click(object sender, EventArgs e)
        {
            bool registered = Program.authSpire.Register(textBoxUsername.Text, textBoxPassword.Text, textBoxLicense.Text, textBoxEmail.Text);
            if (registered)
            {
                Login login = new Login();
                this.Hide();
                login.Show();
            }
        }

        private void Register_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void linkLabelLicenseOnly_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            License license = new License();
            this.Hide();
            license.Show();
        }
    }
}
