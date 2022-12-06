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
    public partial class License : Form
    {
        public License()
        {
            InitializeComponent();
        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            bool loggedIn = Program.authSpire.License(textBoxLicense.Text);
            if (loggedIn)
            {
                Main main = new Main();
                main.Show();
                this.Hide();
            }
        }
    }
}
