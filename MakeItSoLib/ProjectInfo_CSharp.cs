using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MakeItSoLib
{
    /// <summary>
    /// Holds information parsed from one C# project in the solution.
    /// </summary>
    public class ProjectInfo_CSharp : ProjectInfo
    {
        #region Public methods and properties

        /// <summary>
        /// Adds a source file to the project.
        /// </summary>
        public void addFile(string file)
        {
            if (MakeItSoConfig.Instance.IsCygwinBuild == true)
            {
                // For a cygwin build, we seem to need path separators to be 
                // double backslashes. (Not sure why they need to be double - maybe
                // some sort of escaping thing?)
                file = file.Replace("/", @"\\");
            }
            m_files.Add(file);
        }

        /// <summary>
        /// Gets the collection of files in the project. 
        /// File paths are relative to the project's root folder.
        /// </summary>
        public HashSet<string> getFiles()
        {
            return m_files;
        }

        /// <summary>
        /// Gets or sets the output file name.
        /// </summary>
        public string OutputFileName
        {
            get { return m_outputFileName; }
            set { m_outputFileName = value; }
        }

        /// <summary>
        /// Adds a cofiguration to the collection for this project.
        /// </summary>
        public void addConfigurationInfo(ProjectConfigurationInfo_CSharp configurationInfo)
        {
            m_configurationInfos.Add(configurationInfo);
        }

        /// <summary>
        /// Returns the collection of configurations for the project.
        /// </summary>
        public List<ProjectConfigurationInfo_CSharp> getConfigurationInfos()
        {
            return m_configurationInfos;
        }

        #endregion

        #region Private data

        // The collection of source files in the project...
        protected HashSet<string> m_files = new HashSet<string>();

        // The output file name...
        private string m_outputFileName = "";

        // The collection of configurations (Debug, Release etc) for this project...
        private List<ProjectConfigurationInfo_CSharp> m_configurationInfos = new List<ProjectConfigurationInfo_CSharp>();

        #endregion
    }
}
