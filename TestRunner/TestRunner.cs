using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace TestRunner
{
    /// <summary>
    /// Tests that MakeItSo can successfully convert a number of sample 
    /// Visual Studio projects. We:
    /// - Find the collection of solutions in the runtime folder (and its sub-folders) .
    /// - We show each one in a list box.
    /// - You can choose to run them all, or a selection of them.
    /// - For each one selected we:
    ///   - Convert it with MakeItSo.
    ///   - Using cygwin: run a shell script that builds and runs the app.
    ///   - We test that the output of the app is what we expected.
    /// </summary>
    public partial class TestRunner : Form
    {
        #region Public methods and properties

        public TestRunner()
        {
            InitializeComponent();
        }

        #endregion

        #region Private functions

        /// <summary>
        /// The main entry-point for the app.
        /// </summary>
        private void TestRunner_Load(object sender, EventArgs e)
        {
            try
            {
                // Finds the solutions to convert and test...
                findSolutions();

                // We convert, build, run and test each solution...
                runTests();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "TestRunner");
            }
        }

        /// <summary>
        /// Runs each test. 
        /// </summary>
        private void runTests()
        {
            // We test each solution. For each one we:
            // - Convert it with MakeItSo
            // - Run a bash script with cygwin that builds and runs it
            // - Check the output 

            // We loop through the solutions by index. (These are
            // the same indexes as the ones in the list view, which
            // lets us color the rows.)
            for (int i=0; i<m_solutionInfos.Count; ++i)
            {
                TestResults results;
                try
                {
                    SolutionInfo solutionInfo = m_solutionInfos[i];

                    // We run MakeItSo for this solution...
                    runMakeItSo(solutionInfo);

                    // We build and run the project using cygwin...
                    cygwinBuildAndRun(solutionInfo);

                    // We check the expected results...
                    results = checkResults(solutionInfo);
                }
                catch (Exception ex)
                {
                    results = new TestResults();
                    results.Result = TestResults.PassFail.FAILED;
                    results.Description = "Exception caught: " + ex.Message;
                }

                // We update the display with the results...
                ListViewItem listViewItem = ctrlSolutions.Items[i];
                switch (results.Result)
                {
                    case TestResults.PassFail.PASSED:
                        listViewItem.BackColor = Color.LightGreen;
                        break;

                    case TestResults.PassFail.FAILED:
                        listViewItem.BackColor = Color.Pink;
                        listViewItem.ToolTipText = results.Description;
                        break;
                }
            }
        }

        /// <summary>
        /// Runs MakeItSo to convert the solution to a makefile.
        /// </summary>
        private static void runMakeItSo(SolutionInfo solutionInfo)
        {
            Process makeItSoProcess = new Process();
            makeItSoProcess.StartInfo.FileName = "MakeItSo.exe";
            makeItSoProcess.StartInfo.Arguments = String.Format("-file={0} -cygwin=true", solutionInfo.SolutionName);
            makeItSoProcess.StartInfo.WorkingDirectory = solutionInfo.Folder;
            makeItSoProcess.StartInfo.UseShellExecute = false;
            makeItSoProcess.Start();
            makeItSoProcess.WaitForExit();
        }

        /// <summary>
        /// Runs a bash script file with cygwin to build and run the solution.
        /// </summary>
        private static void cygwinBuildAndRun(SolutionInfo solutionInfo)
        {
            Process cygwinProcess = new Process();
            cygwinProcess.StartInfo.FileName = "c:/cygwin/bin/bash.exe";
            cygwinProcess.StartInfo.Arguments = String.Format("-li '{0}/testMakeAndTest.sh'", solutionInfo.CygwinFolder);
            cygwinProcess.StartInfo.WorkingDirectory = solutionInfo.Folder;
            cygwinProcess.StartInfo.UseShellExecute = false;
            cygwinProcess.Start();
            cygwinProcess.WaitForExit();
        }

        /// <summary>
        /// Checks the output produced when the converted solution
        /// was run by cygwin.
        /// </summary>
        private static TestResults checkResults(SolutionInfo solutionInfo)
        {
            TestResults results = new TestResults();
            results.Result = TestResults.PassFail.PASSED;

            // The solution folder should contain a file called testExpectedResults.txt.
            // This contains lines like:
            // [output-file-name] = [expected-output]
            // (There may be multiple lines so that we can test multiple configurations
            // of the solutions.)

            // We read each line from the expected-results file...
            string expectedResultsFilename = String.Format("{0}/testExpectedResults.txt", solutionInfo.Folder);
            string[] lines = File.ReadAllLines(expectedResultsFilename);
            foreach (string line in lines)
            {
                // We get the filename and expected result from the line...
                string[] tokens = line.Split('=');
                if (tokens.Length != 2)
                {
                    throw new Exception(String.Format("Lines should be in the format [output-file-name] = [expected-output]. File={0}", expectedResultsFilename) );
                }
                string file = solutionInfo.Folder + "/" + tokens[0].Trim();
                string expectedResult = tokens[1].Trim();

                // We read the data from the output file, and compare it
                // with the expected results...
                string actualResult = File.ReadAllText(file);
                if (actualResult != expectedResult)
                {
                    results.Result = TestResults.PassFail.FAILED;
                    results.Description += String.Format("Expected '{0}', got '{1}'.", expectedResult, actualResult);
                }
            }

            return results;
        }

        /// <summary>
        /// Finds all solutions to convert in sub-folders and shows them
        /// in a list box.
        /// </summary>
        private void findSolutions()
        {
            // We get the collection of solutions and loop through them...
            string[] solutionFiles = Directory.GetFiles(".", "*.sln", SearchOption.AllDirectories);
            foreach (string solutionFile in solutionFiles)
            {
                // We store info for each solution...
                SolutionInfo solutionInfo = new SolutionInfo();
                solutionInfo.RelativePath = solutionFile;
                solutionInfo.FullPath = Path.GetFullPath(solutionFile);
                solutionInfo.SolutionName = Path.GetFileName(solutionFile);
                solutionInfo.Name = Path.GetFileNameWithoutExtension(solutionFile);
                solutionInfo.Folder = Path.GetDirectoryName(solutionFile);
                
                // We work out the folder to use when launching cygwin...
                string cygwinFolder = Path.GetDirectoryName(solutionInfo.FullPath);
                cygwinFolder = cygwinFolder.Replace("\\", "/");
                cygwinFolder = cygwinFolder.Replace(":", "");
                cygwinFolder = "/cygdrive/" + cygwinFolder;
                solutionInfo.CygwinFolder = cygwinFolder;

                m_solutionInfos.Add(solutionInfo);

                // And show it in the list view...
                ctrlSolutions.Items.Add(solutionInfo.RelativePath);
            }
        }

        #endregion

        #region Private data

        // Holds information about one Solution that we're testing...
        private class SolutionInfo
        {
            public string Name { get; set; }            // e.g. 'SimpleHelloWorld' 
            public string SolutionName { get; set; }    // e.g. 'SimpleHelloWorld.sln' 
            public string RelativePath { get; set; }    // e.g. './VS2008/SimpleHelloWorld/SimpleHelloWorld.sln'
            public string FullPath { get; set; }        // e.g. 'd:/Tests/VS2008/SimpleHelloWorld/SimpleHelloWorld.sln'
            public string Folder { get; set; }          // e.g. './VS2008/SimpleHelloWorld'
            public string CygwinFolder { get; set; }    // e.g. '/cygdrive/d/Tests/VS2008/SimpleHelloWorld'
        }

        // A collection of information about each solution we're testing...
        private List<SolutionInfo> m_solutionInfos = new List<SolutionInfo>();

        // The results from a test of solution...
        private class TestResults
        {
            public enum PassFail { PASSED, FAILED }
            public PassFail Result { get; set; }
            public string Description { get; set; }
        }

        #endregion
    }
}
