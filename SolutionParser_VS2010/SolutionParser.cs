using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MakeItSoLib;
using EnvDTE80;
using System.IO;

namespace SolutionParser_VS2010
{
    /// <summary>
    /// Parses a Visual Studio 2010 solution.
    /// </summary><remarks>
    /// Uses the EnvDTE COM automation libraries to parse the solution
    /// and the projects in the solution.
    /// </remarks>
    public class SolutionParser : SolutionParserBase
    {
        #region Public methods

        /// <summary>
        /// Parses the solution passed in.
        /// </summary>
        public override void parse(string solutionFilename)
        {
            try
            {
                m_parsedSolution.Name = solutionFilename;

                // We create the COM automation objects to open the solution...
                openSolution();

                // We get the root collection of projects and parse them.
                //EnvDTE.Projects rootProjects = Utils.call(() => (m_dteSolution.Projects));
                //parseProjects(rootProjects);

                // We find the dependencies between projects...
                //parseDependencies();
            }
            catch (Exception ex)
            {
                // There was an error parsing this solution...
                string message = String.Format("Failed to parse solution {0} [{1}].", solutionFilename, ex.Message);
                throw new Exception(message);
            }
            finally
            {
                // We always quit the DTE object, to make sure that the instance
                // of Visual Studio we are automating is closed down...
                if (m_dte != null)
                {
                    m_dte.Quit();
                }
            }
        }

        #endregion

        #region Private functions

        /// <summary>
        /// Creates COM automation objects and opens the solution...
        /// </summary>
        private void openSolution()
        {
            // We create a DTE object to automate our interaction
            // with Visual Studio.
            Type type = Type.GetTypeFromProgID("VisualStudio.DTE.10.0");
            Object obj = System.Activator.CreateInstance(type, true);
            m_dte = (DTE2)obj;

            // We open the solution. (This needs to be a full path.)
            string path = Path.GetFullPath(m_parsedSolution.Name);
            m_dteSolution = Utils.call(() => (m_dte.Solution));
            Utils.callVoidFunction(() => { m_dteSolution.Open(path); });

            // We get the root folder for the solution...
            m_parsedSolution.RootFolderAbsolute = Path.GetDirectoryName(path) + "\\";
        }

        #endregion

        #region Private data

        // The DTE object that we use to automate Visual Studio...
        private DTE2 m_dte = null;

        // The automation object representing the solution...
        private EnvDTE.Solution m_dteSolution = null;

        // Types of Visual Studio project that we know how to parse
        // and convert...
        private enum ProjectType
        {
            UNKNOWN,
            CPP_PROJECT,
            CSHARP_PROJECT
        }

        #endregion
    }
}
