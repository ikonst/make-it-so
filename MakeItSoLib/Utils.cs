using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using Thread = System.Threading.Thread;
using System.IO;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace MakeItSoLib
{
    /// <summary>
    /// Utility functions.
    /// </summary>
    public class Utils
    {
        #region Public methods

        /// <summary>
        /// Calls a function, retrying if necessary.
        /// </summary><remarks>
        /// DTE functions call into an instance of Visual Studio running in
        /// a separate process using COM interop. These calls can fail if 
        /// Visual Studio is busy, and in these cases we get a COM exception.
        /// 
        /// This function will retry calling the function if this happens, and
        /// will only fail if it has retried 20 times without success.
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
        public static T call<T>(Func<T> fn)
        {
            int numTries = 20;
            int intervalMS = 50;

            // We will try to call the function a number of times...
            for (int i = 0; i < numTries; ++i)
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
                    Thread.Sleep(intervalMS);
                }
            }

            throw new Exception(String.Format("'call' failed to call function after {0} tries.", numTries));
        }

        /// <summary>
        /// Calls a function with no return value, retrying if necessary.
        /// </summary>
        public static void callVoidFunction(Action fn)
        {
            int numTries = 20;
            int intervalMS = 50;

            // We will try to call the function a number of times...
            for (int i = 0; i < numTries; ++i)
            {
                try
                {
                    // We call the function passed in, and return
                    // if it succeeds...
                    fn();
                    return;
                }
                catch (COMException)
                {
                    // We've caught a COM exception, which is most likely
                    // a Server is Busy exception. So we sleep for a short
                    // while, and then try again...
                    Thread.Sleep(intervalMS);
                }
            }

            throw new Exception(String.Format("'callVoidFunction' failed to call function after {0} tries.", numTries));
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
        /// The path is the path to a file.
        /// </summary>
        public static string addPrefixToFilePath(string path, string prefix)
        {
            string folder = Path.GetDirectoryName(path);
            folder = addPrefixToFolderPath(folder, prefix);
            string filename = Path.GetFileName(path);
            return folder + "/" + filename;
        }

        /// <summary>
        /// Adds the prefix passed in to the last folder in the path passed in.
        /// The path must just be a path to a folder, not to a file.
        /// 
        /// For example:
        ///   path = ../Test/Output/Release
        ///   prefix = gcc
        ///   result = ../Test/Output/gccRelease
        /// </summary>
        public static string addPrefixToFolderPath(string path, string prefix)
        {
            // We convert backslashes to forward slashes, and remove a 
            // trailing slash if there is one...
            path = path.Replace("\\", "/");
            path = path.TrimEnd('/');
            
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
        /// Splits the string passed in by the delimiters passed in.
        /// Quoted sections are not split, and all tokens have whitespace
        /// trimmed from the start and end.
        /// </summary>
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

        /// <summary>
        /// Removes spaces from the final-folder part of the path passed in.
        /// </summary>
        public static string removeSpacesFromFolder(string path)
        {
            int lastSlashIndex = path.LastIndexOf('/');

            StringBuilder result = new StringBuilder();
            for (int index=0; index<path.Length; ++index)
            {
                char currentCharacter = path[index];
                if(index<lastSlashIndex || currentCharacter != ' ')
                {
                    result.Append(currentCharacter);
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Fires the event passed in in a thread-safe way. 
        /// </summary><remarks>
        /// This method loops through the targets of the event and invokes each in turn. If the
        /// target supports ISychronizeInvoke (such as forms or controls) and is set to run 
        /// on a different thread, then we call BeginInvoke to marshal the event to the target
        /// thread. If the target does not support this interface (such as most non-form classes)
        /// or we are on the same thread as the target, then the event is fired on the same
        /// thread as this is called from.
        /// </remarks>
        public static void raiseEvent<T>(EventHandler<T> theEvent, object sender, T args) where T : System.EventArgs
        {
            // Is the event set up?
            if (theEvent == null)
            {
                return;
            }

            // We loop through each of the delegate handlers for this event. For each of 
            // them we need to decide whether to invoke it on the current thread or to
            // make a cross-thread invocation...
            foreach (EventHandler<T> handler in theEvent.GetInvocationList())
            {
                try
                {
                    ISynchronizeInvoke target = handler.Target as ISynchronizeInvoke;
                    if (target == null || target.InvokeRequired == false)
                    {
                        // Either the target is not a form or control, or we are already
                        // on the right thread for it. Either way we can just fire the
                        // event as normal...
                        handler(sender, args);
                    }
                    else
                    {
                        // The target is most likely a form or control that needs the
                        // handler to be invoked on its own thread...
                        target.BeginInvoke(handler, new object[] { sender, args });
                    }
                }
                catch (Exception)
                {
                    // The event handler may have been detached while processing the events.
                    // We just ignore this and invoke the remaining handlers.
                }
            }
        }

        /// <summary>
        /// Returns true if the two paths are the same, regardless of case,
        /// slash-direction or additional folder traversals. For example,
        /// these two paths would be considered the same:
        ///   d:\temp\f1\..\f2\file.txt
        ///   d:/TEMP/f2/file.txt
        /// </summary>
        public static bool isSamePath(string path1, string path2)
        {
            // We have to be careful with empty paths.
            // If both paths are empty, they are the same...
            if (path1 == "" && path2 == "")
            {
                return true;
            }

            // They are not both empty, so if either path is empty
            // then they're not the same...
            if(path1 == "" || path2 == "")
            {
                return false;
            }

            path1 = path1.ToLower();
            path1 = Path.GetFullPath(path1);
            path1.TrimEnd('\\');

            path2 = path2.ToLower();
            path2 = Path.GetFullPath(path2);
            path2.TrimEnd('\\');

            return path1 == path2;
        }

        /// <summary>
        /// Perform a deep copy of the object.
        /// </summary><remarks>
        /// Code from: http://stackoverflow.com/questions/78536/cloning-objects-in-c-sharp
        /// </remarks>
        public static T clone<T>(T source)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "source");
            }

            // Don't serialize a null object, simply return the default for that object
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }

        /// <summary>
        /// Calculates the Levenshtein distance between two strings. The closer
        /// the two strings are two each other, the smaller the number will be.
        /// </summary><remarks>
        /// Code from: http://www.codeproject.com/KB/recipes/Levenshtein.aspx
        /// </remarks>
        public static int levenshteinDistance(String s1, String s2)
        {
            string sNew = s1.ToLower();
            string sOld = s2.ToLower();

            int[,] matrix;              // matrix
            int sNewLen = sNew.Length;  // length of sNew
            int sOldLen = sOld.Length;  // length of sOld
            int sNewIdx; // iterates through sNew
            int sOldIdx; // iterates through sOld
            char sNew_i; // ith character of sNew
            char sOld_j; // jth character of sOld
            int cost; // cost

            /// Test string length
            if (Math.Max(sNew.Length, sOld.Length) > Math.Pow(2, 31))
                throw (new Exception("\nMaximum string length in Levenshtein.LD is " + Math.Pow(2, 31) + ".\nYours is " + Math.Max(sNew.Length, sOld.Length) + "."));

            // Step 1

            if (sNewLen == 0)
            {
                return sOldLen;
            }

            if (sOldLen == 0)
            {
                return sNewLen;
            }

            matrix = new int[sNewLen + 1, sOldLen + 1];

            // Step 2

            for (sNewIdx = 0; sNewIdx <= sNewLen; sNewIdx++)
            {
                matrix[sNewIdx, 0] = sNewIdx;
            }

            for (sOldIdx = 0; sOldIdx <= sOldLen; sOldIdx++)
            {
                matrix[0, sOldIdx] = sOldIdx;
            }

            // Step 3

            for (sNewIdx = 1; sNewIdx <= sNewLen; sNewIdx++)
            {
                sNew_i = sNew[sNewIdx - 1];

                // Step 4

                for (sOldIdx = 1; sOldIdx <= sOldLen; sOldIdx++)
                {
                    sOld_j = sOld[sOldIdx - 1];

                    // Step 5

                    if (sNew_i == sOld_j)
                    {
                        cost = 0;
                    }
                    else
                    {
                        cost = 1;
                    }

                    // Step 6

                    matrix[sNewIdx, sOldIdx] = minimum(matrix[sNewIdx - 1, sOldIdx] + 1, matrix[sNewIdx, sOldIdx - 1] + 1, matrix[sNewIdx - 1, sOldIdx - 1] + cost);

                }
            }

            // Step 7

            /// Value between 0 - 100
            /// 0==perfect match 100==totaly different
            int max = System.Math.Max(sNewLen, sOldLen);
            return (100 * matrix[sNewLen, sOldLen]) / max;
        }

        #endregion

        #region Private functions

        /// <summary>
        /// Returns the smallest of the three numbers passed in.
        /// (Used by the levenshteinDistance() function.)
        /// </summary>
        private static int minimum(int a, int b, int c)
        {
            int mi = a;

            if (b < mi)
            {
                mi = b;
            }
            if (c < mi)
            {
                mi = c;
            }

            return mi;
        }
        
        #endregion
    }
}
