using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MakeItSoLib;
using Microsoft.VisualStudio.VCProjectEngine;
using VSLangProj80;

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
        public ProjectParser_CSharp(VSProject2 vsProject, string solutionRootFolder)
        {
            try
            {
                m_vsProject = vsProject;
                m_solutionRootFolder = solutionRootFolder;
                m_dteProject = Utils.dteCall<EnvDTE.Project>(() => vsProject.Project);

                // We get the project name...
                m_parsedProject.Name = Utils.dteCall<string>(() => (m_dteProject.Name));
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
        public ProjectInfo_CSharp Project 
        {
            get { return m_parsedProject; }
        }

        #endregion

        #region Private data

        // Holds the parsed project data...
        private ProjectInfo_CSharp m_parsedProject = new ProjectInfo_CSharp();

        // The root folder of the solution that this project is part of...
        private string m_solutionRootFolder = "";

        // The Visual Studio project objects. We need two of these: the
        // EnvDte project which as overall project info, and the VSProject2
        // which has C#-specific info...
        private EnvDTE.Project m_dteProject = null;
        private VSProject2 m_vsProject = null;

        #endregion
    }
}
