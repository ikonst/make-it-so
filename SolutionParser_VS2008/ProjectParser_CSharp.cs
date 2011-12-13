using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MakeItSoLib;
using Microsoft.VisualStudio.VCProjectEngine;

namespace SolutionParser_VS2008
{
    /// <summary>
    /// Parses a C# project.
    /// </summary><remarks>
    /// We extract information from a VCProject object, and fill in a Project structure.
    /// </remarks>
    public class ProjectParser_CSharp
    {
        #region Public methods and properties

        /// <summary>
        /// Constructor
        /// </summary>
        public ProjectParser_CSharp(VCProject vcProject, string solutionRootFolder)
        {
            try
            {
                m_vcProject = vcProject;
                m_solutionRootFolder = solutionRootFolder;

                // We get the project name...
                m_parsedProject.Name = Utils.dteCall<string>(() => (m_vcProject.Name));
                Log.log("- parsing project " + m_parsedProject.Name);

                // and parse the project...
                //parseProject();
                Log.log("  - done");
            }
            catch (Exception ex)
            {
                Log.log(String.Format("  - FAILED ({0})", ex.Message));
            }
        }

        /// <summary>
        /// Gets the parsed project.
        /// </summary>
        public Project Project 
        {
            get { return m_parsedProject; }
        }

        #endregion

        #region Private data

        // Holds the parsed project data...
        private Project m_parsedProject = new Project();

        // The root folder of the solution that this project is part of...
        private string m_solutionRootFolder = "";

        // The Visual Studio project object...
        private VCProject m_vcProject = null;

        #endregion
    }
}
