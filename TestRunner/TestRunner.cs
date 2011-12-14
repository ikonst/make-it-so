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
using MakeItSoLib;
using System.Threading;

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
        #region Events

        // Raised when a test has been completed on a worker thread...
        private event EventHandler<EventArgs> TestCompleted;

        #endregion

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
                // We will run a number of tests in parallel...
                int cores = Environment.ProcessorCount;
                ThreadPool.SetMaxThreads(cores, cores);
                TestCompleted += onTestCompleted;

                // Finds the solutions to convert and test...
                findSolutions();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "TestRunner");
            }
        }

        /// <summary>
        /// Called when a test has been completed. (It s called back
        /// on the UI thread, even though the event was raised by a 
        /// worker thread.)
        /// </summary>
        private void onTestCompleted(object sender, EventArgs e)
        {
            try
            {
                // We update the screen...
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "TestRunner");
            }
        }

        /// <summary>
        /// Runs each test. 
        /// </summary>
        private void runTests()
        {
            // We loop through the selected solutions...
            foreach(int index in ctrlSolutions.CheckedIndices)
            {
                // We queue each test to run on a worker thread...
                SolutionInfo solutionInfo = (SolutionInfo)ctrlSolutions.Items[index];
                ThreadPool.QueueUserWorkItem(runTestOnWorkerThread, solutionInfo);
            }
        }

        /// <summary>
        /// Runs a test on a worker thread.
        /// </summary>
        private void runTestOnWorkerThread(object context)
        {
            try
            {
                // We test one solution:
                // - Convert it with MakeItSo
                // - Run a bash script with cygwin that builds and runs it
                // - Check the output 
                SolutionInfo solutionInfo = (SolutionInfo)context;

                TestResults results;
                try
                {

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
                switch (results.Result)
                {
                    case TestResults.PassFail.PASSED:
                        solutionInfo.TestResult = "PASS";
                        solutionInfo.BackgroundColor = Color.LightGreen;
                        break;

                    case TestResults.PassFail.FAILED:
                        solutionInfo.TestResult = String.Format("FAIL ({0})", results.Description);
                        solutionInfo.BackgroundColor = Color.LightPink;
                        break;
                }

                // We notify the UI thread that the test has completed,
                // and that it can update the screen...
                Utils.raiseEvent(TestCompleted, this, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "TestRunner");
            }
        }

        /// <summary>
        /// Runs MakeItSo to convert the solution to a makefile.
        /// </summary>
        private static void runMakeItSo(SolutionInfo solutionInfo)
        {
            Process makeItSoProcess = new Process();
            makeItSoProcess.StartInfo.FileName = "MakeItSo.exe";
            makeItSoProcess.StartInfo.Arguments = String.Format("-file={0} -cygwin=true", Utils.quote(solutionInfo.SolutionName));
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
                List<string> tokens = Utils.split(line, '=');
                if (tokens.Count != 2)
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

                // And show it in the list view...
                ctrlSolutions.Items.Add(solutionInfo, true);
            }
        }

        /// <summary>
        /// Called when the 'Run tests' button is pressed.
        /// </summary>
        private void cmdRunTests_Click(object sender, EventArgs e)
        {
            try
            {
                // We convert, build, run and test each solution...
                runTests();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "TestRunner");
            }
        }

        /// <summary>
        /// Called when the 'Select all' button is pressed.
        /// </summary>
        private void cmdSelectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < ctrlSolutions.Items.Count; ++i)
            {
                ctrlSolutions.SetItemChecked(i, true);
            }
        }

        /// <summary>
        /// Called when the 'Unselect all' button is pressed.
        /// </summary>
        private void cmdUnselectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < ctrlSolutions.Items.Count; ++i)
            {
                ctrlSolutions.SetItemChecked(i, false);
            }
        }

        #endregion

        #region Private data

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
