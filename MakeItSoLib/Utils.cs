using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using Thread = System.Threading.Thread;
using System.IO;

namespace MakeItSoLib
{
    /// <summary>
    /// Utility functions.
    /// </summary>
    public class Utils
    {
        #region Public methods

        /// <summary>
        /// Calls a DTE function, retrying if necessary.
        /// </summary><remarks>
        /// DTE functions call into an instance of Visual Studio running in
        /// a separate process using COM interop. These calls can fail if 
        /// Visual Studio is busy, and in these cases we get a COM exception.
        /// 
        /// This function will retry calling the function if this happens, and
        /// will only fail if it has retried 100 times without success.
        /// 
        /// You pass in the function - or property - to call usually as a
        /// lambda. For example, to get the projects.Count property you would 
        /// call:
        /// 
        ///   int count = dteCall[int](() => (projects.Count));
        ///   
        /// (Note: replace the [] above with angle-brackets to specify the 
        ///        return type.)
        /// </remarks>
        public static T dteCall<T>(Func<T> fn)
        {
            // We will try to call the function up to 100 times...
            for (int i=0; i<100; ++i)
            {
                try
                {
                    // We call the function passed in and return the result...
                    return fn();
                }
                catch (COMException)
                {
                    // We've caught a COM exception, which is most likely
                    // a Server is Busy exception. So we sleep for a short
                    // while, and then try again...
                    Thread.Sleep(1);
                }
            }

            throw new Exception("dteCall failed to call function after 100 tries.");
        }

        /// <summary>
        /// Returns the relative path between the two paths passed in.
        /// </summary><remarks>
        /// For example, you could turn an absolute path into a relative path
        /// from a root folder. Say you have these paths:
        ///   root =     d:\f1\f2\
        ///   absolute = d:\f1\f2\f3\file.ext
        /// makeRelativePath(root, absolutePath) will return f3/file.ext
        /// 
        /// Note1: The root folder must end with '\'
        /// Note2: Relative paths are returned in Unix format using forward slashes.
        /// </remarks>
        public static string makeRelativePath(String fromPath, String toPath)
        {
            if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            string relativePath = "";
            if (Path.IsPathRooted(toPath) == false)
            {
                // The to-path is already a relative path, so we just return it,
                // making sure that it has unix-style forward slashes.
                // (It's not entirely clear, of course, that it is relative to the
                // from-path passed in, but we've not got anything else to go on.)
                relativePath = toPath.Replace('\\', '/');
            }
            else
            {
                // The to-path is absolute, so we try to find the relative path 
                // from the root from-path...
                Uri fromUri = new Uri(fromPath);
                Uri toUri = new Uri(toPath);

                Uri relativeUri = fromUri.MakeRelativeUri(toUri);
                relativePath = Uri.UnescapeDataString(relativeUri.ToString());
            }

            // If the paths are the same, we return a "."
            if (relativePath == "")
            {
                relativePath = ".";
            }

            return relativePath;
        }

        /// <summary>
        /// Adds the prefix passed in to the last folder in the path passed in.
        /// 
        /// For example:
        ///   path = ../Test/Output/Release
        ///   prefix = gcc
        ///   result = ../Test/Output/gccRelease
        /// </summary>
        public static string addPrefixToFolder(string path, string prefix)
        {
            path = path.Replace("\\", "/");
            int i = path.LastIndexOf('/');
            if (i == -1)
            {
                return String.Format("{0}{1}", prefix, path);
            }
            else
            {
                string firstPart = path.Substring(0, i + 1);
                string lastPart = path.Substring(i + 1, path.Length - i - 1);
                return String.Format("{0}{1}{2}", firstPart, prefix, lastPart);
            }
        }

        /// <summary>
        /// Converts a Linux-style library name to a raw name, 
        /// e.g. libMath.a => Math
        /// </summary>
        public static string convertLinuxLibraryNameToRawName(string libraryName)
        {
            if (libraryName.StartsWith("lib") == true)
            {
                libraryName = libraryName.Substring(3);
            }
            libraryName = Path.GetFileNameWithoutExtension(libraryName);
            return libraryName;
        }

        /// <summary>
        /// Splits the string passed in by the delimiter passed in.
        /// Quoted sections are not split, and all tokens have whitespace
        /// removed.
        public static List<string> split(string stringToSplit, params char[] delimiters)
        {
            List<string> results = new List<string>();

            bool inQuote = false;
            StringBuilder currentToken = new StringBuilder();
            for (int index = 0; index < stringToSplit.Length; ++index)
            {
                char currentCharacter = stringToSplit[index];
                if (currentCharacter == '"')
                {
                    // When we see a ", we need to decide whether we are
                    // at the start or send of a quoted section...
                    inQuote = !inQuote;
                }
                else if (delimiters.Contains(currentCharacter) && inQuote == false)
                {
                    // We've come to the end of a token, so we find the token,
                    // trim it and add it to the collection of results...
                    string result = currentToken.ToString().Trim();
                    if (result != "") results.Add(result);

                    // We start a new token...
                    currentToken = new StringBuilder();
                }
                else
                {
                    // We've got a 'normal' character, so we add it to
                    // the curent token...
                    currentToken.Append(currentCharacter);
                }
            }

            // We've come to the end of the string, so we add the last token...
            string lastResult = currentToken.ToString().Trim();
            if (lastResult != "") results.Add(lastResult);

            return results;
        }

        /// <summary>
        /// Returns the input string surrounded by quotes.
        /// </summary>
        public static string quote(string input)
        {
            return String.Format("\"{0}\"", input);
        }

        /// <summary>
        /// Returns the input string surrounded by quotes and with a trailing space.
        /// </summary>
        public static string quoteAndSpace(string input)
        {
            return String.Format("\"{0}\" ", input);
        }

        #endregion
    }
}
