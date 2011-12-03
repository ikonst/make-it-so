using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace TestRunner
{
    /// <summary>
    /// Information about a solution that we show in the list box.
    /// </summary>
    class SolutionInfo
    {
        // Constructor...
        public SolutionInfo()
        {
            BackgroundColor = SystemColors.Window;
        }

        public string Name { get; set; }            // e.g. 'SimpleHelloWorld' 
        public string SolutionName { get; set; }    // e.g. 'SimpleHelloWorld.sln' 
        public string RelativePath { get; set; }    // e.g. './VS2008/SimpleHelloWorld/SimpleHelloWorld.sln'
        public string FullPath { get; set; }        // e.g. 'd:/Tests/VS2008/SimpleHelloWorld/SimpleHelloWorld.sln'
        public string Folder { get; set; }          // e.g. './VS2008/SimpleHelloWorld'
        public string CygwinFolder { get; set; }    // e.g. '/cygdrive/d/Tests/VS2008/SimpleHelloWorld'
        public string TestResult { get; set; }      // The test result as a string.
        public Color BackgroundColor { get; set; }  // The background color for the row showing this info.

        // The string to show in the list box...
        public override string ToString()
        {
            if (TestResult == null)
            {
                return RelativePath;
            }
            else
            {
                return String.Format("[{0}] - {1}", TestResult, RelativePath);
            }
        }

    }
}
