using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MakeItSoLib
{
    /// <summary>
    /// A base class for Project classes. 
    /// 
    /// These hold information about one Visual Studio project type, for
    /// example C++ projects. They also know how to write makefiles that 
    /// will build the project type.
    /// 
    /// This base class holds data that is common across all project types,
    /// and helper functions across project types as well.
    /// </summary>
    public class Project
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
        /// Constructor
        /// </summary>
        public Project()
        {
        }

        /// <summary>
        /// Sets up implicit linking for this project.
        /// </summary>
        public void setupImplicitLinking()
        {
            // Implicit linking can be a bit tricky, and works differently
            // depending on whether the project is an executable or a 
            // library.

            // Executables
            // -----------
            // 1. We check if the project is set to link dependencies.
            //    If so, we:
            // 2. Find the libraries that the executable depends and link 
            //    them in.
            // 3. If the libraries themselves depend on other libraries,
            //    we need to link them in as well. We recurse through the 
            //    chain of dependencies, to find all the libraries that 
            //    the executable depends on. (Subject to rule 4, below.)
            // 4. If any library is set to link its dependencies, we 
            //    link it, but we do not link any of the libraries it
            //    depends on (as they are already linked into the
            //    library itself).

            // Libraries
            // ---------
            // 1. We check if the library is set to link dependencies.
            //    If so, we:
            // 2. Find the libraries that the project depends on.
            //    We find the list of object files that these libraries
            //    are made up from, and add them to the collection to
            //    create the main library from.
            // 3. We recurse through any libraries that the libraries
            //    from step 2 depend on. We add their files to this
            //    library as well.

            // We check if this project should implicitly link the projects
            // it depends on...
            if (LinkLibraryDependencies == false)
            {
                return;
            }

            // We want to link projects we depend on...
            switch (ProjectType)
            {
                case Project.ProjectTypeEnum.CPP_EXECUTABLE:
                    {
                        // We find the libraries that this executable depends on...
                        List<ImplicitLinkInfo> infos = new List<ImplicitLinkInfo>();
                        findImplicitlyLinkedLibraries(infos);

                        // And add them to the library path...
                        foreach (ImplicitLinkInfo info in infos)
                        {
                            // We find the configuration for this info and add
                            // the library to it...
                            ProjectConfiguration configuration = getConfigurations().Find((cfg) => (cfg.Name == info.ConfigurationName));
                            configuration.addLibraryRawName(info.LibraryRawName);

                            // We add the library path to it...
                            string libraryPath = Utils.makeRelativePath(RootFolderAbsolute, info.OutputFolderAbsolute);
                            configuration.addLibraryPath(libraryPath);
                        }
                    }
                    break;

                case Project.ProjectTypeEnum.CPP_STATIC_LIBRARY:
                    {
                        // We find the collection of object files used by any 
                        // libraries this library depends on...
                        List<ImplicitLinkInfo> infos = new List<ImplicitLinkInfo>();
                        findImplicitlyLinkedObjectFiles(infos);

                        // We add the files to the configurations...
                        foreach (ImplicitLinkInfo info in infos)
                        {
                            // We find the configuration for this info...
                            ProjectConfiguration configuration = getConfigurations().Find((cfg) => (cfg.Name == info.ConfigurationName));

                            // We add the collection of object files to the configuration...
                            string intermediateFolderAbsolute = Utils.addPrefixToFolder(info.IntermediateFolderAbsolute, "gcc");
                            foreach (string objectFile in info.ObjectFileNames)
                            {
                                string absolutePath = intermediateFolderAbsolute + "/" + objectFile;
                                string relativePath = Utils.makeRelativePath(RootFolderAbsolute, absolutePath);
                                configuration.addImplicitlyLinkedObjectFile(relativePath);
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Gets or sets the project's name.
        /// </summary>
        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
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
        /// Adds a configuration to the project.
        /// </summary>
        public void addConfiguration(ProjectConfiguration configuration)
        {
            m_configurations.Add(configuration);
        }

        /// <summary>
        /// Gets or the collection of configurations (debug, release etc) for the project.
        /// </summary>
        public List<ProjectConfiguration> getConfigurations()
        {
            return m_configurations;
        }

        /// <summary>
        /// Adds a project to the collection that this project depends on.
        /// </summary>
        public void addRequiredProject(Project project)
        {
            m_requiredProjects.Add(project);
        }

        /// <summary>
        /// Gets the collection of projects (in fact project names) that this project depends on.
        /// </summary>
        public HashSet<Project> getRequiredProjects()
        {
            return m_requiredProjects;
        }

        /// <summary>
        /// If true, we automatically link in any libraries that we depend
        /// on, even if they are not explicitly in the Libraries collection.
        /// </summary>
        public bool LinkLibraryDependencies
        {
            get { return m_linkLibraryDependencies; }
            set { m_linkLibraryDependencies = value; }
        }

        #endregion

        #region Private functions

        /// <summary>
        /// Finds extra libraries that this executable project needs to link 
        /// as the result of dependencies.
        /// </summary>
        private void findImplicitlyLinkedLibraries(List<ImplicitLinkInfo> infos)
        {
            // We loop through the projects that this project depends on...
            foreach (Project requiredProject in getRequiredProjects())
            {
                // Is the required project a static library?
                if (requiredProject.ProjectType != ProjectTypeEnum.CPP_STATIC_LIBRARY
                    &&
                    requiredProject.ProjectType != ProjectTypeEnum.CPP_DLL)
                {
                    continue;
                }

                // We've found a library, so we add it to our collection 
                // of items to link in...
                foreach (ProjectConfiguration configuration in requiredProject.getConfigurations())
                {
                    ImplicitLinkInfo info = new ImplicitLinkInfo();
                    info.LibraryRawName = requiredProject.Name;
                    info.ConfigurationName = configuration.Name;
                    info.OutputFolderAbsolute = configuration.OutputFolderAbsolute;
                    infos.Add(info);
                }

                // As long as this library is *not* set to link its own
                // dependencies, we recurse into it to see if it has any
                // library dependencies of its own...
                if (requiredProject.LinkLibraryDependencies == false)
                {
                    requiredProject.findImplicitlyLinkedLibraries(infos);
                }
            }
        }

        /// <summary>
        /// We find the collection of object files used by any libraries
        /// that this project depends on.
        /// </summary>
        private void findImplicitlyLinkedObjectFiles(List<ImplicitLinkInfo> infos)
        {
            // We loop through the projects that this project depends on...
            foreach (Project requiredProject in getRequiredProjects())
            {
                // Is the required project a static library?
                if (requiredProject.ProjectType != ProjectTypeEnum.CPP_STATIC_LIBRARY)
                {
                    continue;
                }

                // We've found a library, so we add its object files to our collection...
                foreach (ProjectConfiguration configuration in requiredProject.getConfigurations())
                {
                    ImplicitLinkInfo info = new ImplicitLinkInfo();
                    info.ConfigurationName = configuration.Name;
                    info.IntermediateFolderAbsolute = configuration.IntermediateFolderAbsolute;
                    foreach (string file in requiredProject.getFiles())
                    {
                        string objectFile = Path.ChangeExtension(file, ".o");
                        info.ObjectFileNames.Add(objectFile);
                    }

                    infos.Add(info);
                }

                // We find object files in any libraries this project depends on...
                requiredProject.findImplicitlyLinkedObjectFiles(infos);
            }
        }

        #endregion

        #region Private data

        // The project's name...
        private string m_name = "";

        // The project's root folder, relative to the solution root...
        private string m_rootFolderRelative = "";

        // The absolute path to the project root folder...
        private string m_rootFolderAbsolute = "";

        // The project type, e.g exe, library etc...
        private ProjectTypeEnum m_projectType = ProjectTypeEnum.INVALID;

        // The collection of source files in the project...
        private HashSet<string> m_files = new HashSet<string>();

        // The collection of configurations (debug, release)...
        private List<ProjectConfiguration> m_configurations = new List<ProjectConfiguration>();

        // The collection of projects that this project depends on...
        private HashSet<Project> m_requiredProjects = new HashSet<Project>();

        // True if we need to implicitly link libraries we depend on...
        private bool m_linkLibraryDependencies = false;

        // Holds information about files we need to link in implicitly...
        private class ImplicitLinkInfo
        {
            // Constructor...
            public ImplicitLinkInfo()
            {
                ObjectFileNames = new List<string>();
            }

            // The configuration that these settings aply to...
            public string ConfigurationName { get; set; }

            // Data for implicitly linking libraries into executables...
            public string LibraryRawName { get; set; }
            public string OutputFolderAbsolute { get; set; }

            // Data for implicitly linking object files into libraries...
            public string IntermediateFolderAbsolute { get; set; }
            public List<string> ObjectFileNames { get; set; }
        }

        #endregion
    }
}
