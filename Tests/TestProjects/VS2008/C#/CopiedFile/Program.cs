using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CopiedFile
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string text = File.ReadAllText("Hello.txt");
                Console.Write(text);
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
        }
    }
}
