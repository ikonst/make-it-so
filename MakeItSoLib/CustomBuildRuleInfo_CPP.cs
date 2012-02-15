using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MakeItSoLib
{
    /// <summary>
    /// Holds information about a custom build rule on one file
    /// of a C++ project.
    /// </summary>
    public class CustomBuildRuleInfo_CPP
    {
        #region Public methods and properties

        /// <summary>
        /// Gets or sets the rule name.
        /// </summary>
        public string RuleName
        {
            get { return m_ruleName; }
            set { m_ruleName = value; }
        }

        /// <summary>
        /// Gets or sets the relative path to the file the rule is run on.
        /// </summary>
        public string RelativePathToFile
        {
            get { return m_relativePathToFile; }
            set { m_relativePathToFile = value; }
        }

        /// <summary>
        /// Gets or sets the relative path to the rule executable.
        /// </summary>
        public string RelativePathToExecutable
        {
            get { return m_relativePathToExecutable; }
            set { m_relativePathToExecutable = value; }
        }

        /// <summary>
        /// Adds a parameter to the collection to be passed to the 
        /// rule executable.
        /// </summary>
        public void addParameter(string parameter)
        {
            m_parameters.Add(parameter);
        }

        /// <summary>
        /// Returns the collection of parameters to pass to the rule executable.
        /// </summary>
        public List<string> getParameters()
        {
            return m_parameters;
        }

        /// <summary>
        /// Returns the command-line for the rule.
        /// </summary>
        public string getCommandLine(string folderPrefix)
        {
            string commandLine = Utils.addPrefixToFilePath(m_relativePathToExecutable, folderPrefix);
            foreach (string parameter in m_parameters)
            {
                commandLine += (" " + parameter);
            }
            return commandLine;
        }

        #endregion

        #region Private data

        // The name of the custom build rule...
        private string m_ruleName = "";

        // The path to the file which the rule is to be run on, 
        // relative to the project root...
        private string m_relativePathToFile = "";

        // The path to the executable to run for the custom rule,
        // relative to the project root...
        private string m_relativePathToExecutable = "";

        // The collection of parameters to pass to the custom build rule...
        private List<string> m_parameters = new List<string>();

        #endregion
    }
}
