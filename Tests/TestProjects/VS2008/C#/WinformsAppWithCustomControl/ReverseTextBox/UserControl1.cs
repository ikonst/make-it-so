using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ReverseTextBox
{
    public partial class UserControl1 : UserControl
    {
        public UserControl1()
        {
            InitializeComponent();
        }

        public void setText(string text)
        {
            char[] charArray = text.ToCharArray();
            Array.Reverse(charArray);
            label1.Text = new String(charArray);
        }

        public string getText()
        {
            return label1.Text;
        }
    }
}
