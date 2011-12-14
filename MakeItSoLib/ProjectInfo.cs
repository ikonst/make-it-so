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
            CPP_DLL
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
        /// Gets or sets the project type.
        /// </summary>
        public ProjectTypeEnum ProjectType
        {
            get { return m_projectType; }
            set { m_projectType = value; }
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
        /// Adds a source file to the project.
        /// </summary>
        public void addFile(string file)
        {
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
        /// Adds a project to the collection that this project depends on.
        /// </summary>
        public void addRequiredProject(ProjectInfo_CPP project)
        {
            m_requiredProjects.Add(project);
        }

        /// <summary>
        /// Gets the collection of projects (in fact project names) that this project depends on.
        /// </summary>
        public HashSet<ProjectInfo_CPP> getRequiredProjects()
        {
            return m_requiredProjects;
        }

        #endregion

        #region Private and protected data

        // The project's name...
        protected string m_name = "";

        // The project's root folder, relative to the solution root...
        protected string m_rootFolderRelative = "";

        // The absolute path to the project root folder...
        protected string m_rootFolderAbsolute = "";

        // The project type, e.g exe, library etc...
        protected ProjectTypeEnum m_projectType = ProjectTypeEnum.INVALID;

        // The collection of source files in the project...
        protected HashSet<string> m_files = new HashSet<string>();

        // The collection of projects that this project depends on...
        protected HashSet<ProjectInfo_CPP> m_requiredProjects = new HashSet<ProjectInfo_CPP>();

        #endregion
    }
}
