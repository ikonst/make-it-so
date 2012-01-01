using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace App
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            userControl11.setText("!dlroW ,olleH");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string text = userControl11.getText();
            File.WriteAllText("testOutput.txt", text);
            Close();
        }
    }
}
