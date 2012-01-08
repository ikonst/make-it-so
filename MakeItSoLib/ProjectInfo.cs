using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MakeItSoLib
{
    /// <summary>
    /// A base class for ProjectInfo classes. There are derived classes
    /// for different project types, such as C++ projects, C# projects
    /// and so on.
    /// 
    /// These hold information about one project in a Visual Studio 
    /// solution. The information should be enough to create the
    /// makefile for the project.
    /// 
    /// The idea is that data held in this class (and its derived classes)
    /// is in a 'neutral format'. That is, not in a format specific to a
    /// particular compiler version. So we should be able to build makefiles
    /// from this data, regardless of the version of the original VS project
    /// file that it came from.
    /// 
    /// This base class holds properties that are common across different
    /// project types.
    /// </summary>
    public abstract class ProjectInfo
    {
        #region Public types

        /// <summary>
        /// An enum for the various project types that we know how to
        /// convert from Visual Studio projects to makefiles.
        /// </summary>
        public enum ProjectTypeEnum
        {
            INVALID,
            CPP_EXECUTABLE,
            CPP_STATIC_LIBRARY,
            CPP_DLL,
            CSHARP_EXECUTABLE,
            CSHARP_LIBRARY,
            CSHARP_WINFORMS_EXECUTABLE
        }

        #endregion

        #region Public methods and properties

        /// <summary>
        /// Gets or sets the project's name.
        /// Note that we strip out spaces if the name contains them.
        /// </summary>
        public string Name
        {
            get { return m_name; }
            set { m_name = value.Replace(" ", ""); }
        }

        /// <summary>
        /// Gets or sets the solution that this project is part of.
        /// </summary>
        public SolutionInfo ParentSolution
        {
            get { return m_parentSolution; }
            set { m_parentSolution = value; }
        }

        /// <summary>
        /// Gets or sets the project type.
        /// </summary>
        public ProjectTypeEnum ProjectType
        {
            get 
            { 
                return m_projectType; 
            }
            set 
            { 
                m_projectType = value; 

                // We may need to build a shared-objects library instead of a static
                // library, if this is specified in the config file...
                MakeItSoConfig_Project projectConfig = MakeItSoConfig.Instance.getProjectConfig(m_name);
                if (m_projectType == ProjectTypeEnum.CPP_STATIC_LIBRARY
                    &&
                    projectConfig.ConvertStaticLibraryToSharedObjects == true)
                {
                    m_projectType = ProjectTypeEnum.CPP_DLL;
                }
            }
        }

        /// <summary>
        /// Gets or sets the project's root folder relative to 
        /// the solution folder.
        /// </summary>
        public string RootFolderRelative
        {
            get { return m_rootFolderRelative; }
            set { m_rootFolderRelative = value; }
        }

        /// <summary>
        /// Gets or sets the full path to the project's root folder.
        /// </summary>
        public string RootFolderAbsolute
        {
            get { return m_rootFolderAbsolute; }
            set { m_rootFolderAbsolute = value; }
        }

        /// <summary>
        /// Adds a project to the collection that this project depends on.
        /// </summary>
        public void addRequiredProject(ProjectInfo project)
        {
            m_requiredProjects.Add(project);
        }

        /// <summary>
        /// Gets the collection of projects (in fact project names) that this project depends on.
        /// </summary>
        public HashSet<ProjectInfo> getRequiredProjects()
        {
            return m_requiredProjects;
        }

        #endregion

        #region Private and protected data

        // The project's name...
        protected string m_name = "";

        // The solution that this project is part of...
        protected SolutionInfo m_parentSolution = null;

        // The project's root folder, relative to the solution root...
        protected string m_rootFolderRelative = "";

        // The absolute path to the project root folder...
        protected string m_rootFolderAbsolute = "";

        // The project type, e.g exe, library etc...
        protected ProjectTypeEnum m_projectType = ProjectTypeEnum.INVALID;

        // The collection of projects that this project depends on...
        protected HashSet<ProjectInfo> m_requiredProjects = new HashSet<ProjectInfo>();

        #endregion
    }
}
