using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HelloLib;
using WorldLib;

namespace TextLib
{
    public class Text
    {
        public static string getText()
        {
            return Hello.getText() + ", " + World.getText() + "!";
        }
    }
}
