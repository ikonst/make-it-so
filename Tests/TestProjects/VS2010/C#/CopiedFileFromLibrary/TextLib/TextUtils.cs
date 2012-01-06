using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TextLib
{
    public class TextUtils
    {
        public static string getText()
        {
            try
            {
                return File.ReadAllText("Hello.txt");
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
