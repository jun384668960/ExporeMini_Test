using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ExporeMini_Test
{
    public partial class PlanetSetting : Form
    {
        public PlanetSetting()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        internal void SetDesktopLocation()
        {
            throw new NotImplementedException();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void PlanetSetting_Load(object sender, EventArgs e)
        {

        }

        public string getIp()
        {
            return this.txt_tcpIp.Text;
        }
        public int getPort()
        {
            return Int32.Parse(this.txt_tcpPort.Text);
        }
        public string getUrl()
        {
            return this.textBox1.Text;
        }
        public int getBuffTime()
        {
            return Int32.Parse(this.textBox2.Text);
        }
    }
}
