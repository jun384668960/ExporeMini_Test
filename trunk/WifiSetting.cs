using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ExporeMini_Test
{
    public partial class WifiSetting : Form
    {
        public WifiSetting()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
        public string getSsid()
        {
            return this.ssid.Text;
        }
        public string getPasswd()
        {
            return this.passwd.Text;
        }
    }
}
