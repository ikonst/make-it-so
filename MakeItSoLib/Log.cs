using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MakeItSoLib
{
    /// <summary>
    /// Logs text to the console and to a log file called MakeItSo.log 
    /// in the working folder.
    /// </summary>
    public class Log
    {
        #region Public methods

        /// <summary>
        /// Deleted the log file, if it exists.
        /// </summary>
        public static void clear()
        {
            File.Delete("MakeItSo.log");
        }

        /// <summary>
        /// Writes the text passed in to the log file.
        /// </summary>
        public static void log(string message)
        {
            // We write to the console
            Console.WriteLine(message);

            // And to the log...
            StreamWriter writer = File.AppendText("MakeItSo.log");
            writer.WriteLine(message);
            writer.Close();
        }

        #endregion

    }
}
