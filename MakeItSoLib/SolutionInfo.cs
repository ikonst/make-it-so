using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MakeItSoLib
{
    /// <summary>
    /// Holds data representing the Visual Studio solution we are converting. The
    /// data is parsed from the solution.
    /// </summary>
    public class SolutionInfo
    {
        #region Public methods and properties

        /// <summary>
        /// Gets or sets the solution name.
        /// </summary>
        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        /// <summary>
        /// Gets or sets the root folder of the solution.
        /// </summary>
        public string RootFolderAbsolute
        {
            get { return m_rootFolderAbsolute; }
            set { m_rootFolderAbsolute = value; }
        }

        /// <summary>
        /// Adds a project to the collection in the solution.
        /// </summary>
        public void addProjectInfo(string projectName, ProjectInfo project)
        {
            m_projects.Add(projectName, project);
        }

        /// <summary>
        /// Gets the collection of projects in the solution.
        /// </summary>
        public List<ProjectInfo> getProjectInfos()
        {
            return m_projects.Values.ToList();
        }

        /// <summary>
        /// Adds a required project to the project passed in.
        /// </summary>
        public void addRequiredProjectToProject(string projectName, string requiredProjectName)
        {
            // We find the two projects, and add the required-project
            // to the project...
            ProjectInfo project, requiredProject;
            if (m_projects.TryGetValue(projectName, out project) == false) return;
            if (m_projects.TryGetValue(requiredProjectName, out requiredProject) == false) return;
            project.addRequiredProject(requiredProject);
        }

        /// <summary>
        /// Checks if any librararies need to be linked into projects
        /// because of implicit link rules via project dependencies.
        /// </summary>
        public void setupImplicitLinking()
        {
            foreach(ProjectInfo project in m_projects.Values)
            {
                // We only need to set up implicit linking for C++ projects...
                ProjectInfo_CPP cppProject = project as ProjectInfo_CPP;
                if (cppProject != null)
                {
                    cppProject.setupImplicitLinking();
                }
            }
        }

        #endregion

        #region Private data

        // The solution name...
        private string m_name = "";

        // The root folder of the solution...
        private string m_rootFolderAbsolute = "";

        // The collection of projects in the solution, keyed by
        // the project name...
        private Dictionary<string, ProjectInfo> m_projects = new Dictionary<string, ProjectInfo>();

        #endregion

    }
}
