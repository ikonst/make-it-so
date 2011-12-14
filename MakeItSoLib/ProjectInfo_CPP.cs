using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MakeItSoLib
{
    /// <summary>
    /// Holds information about C++ Visual Studio projects. This data is parsed 
    /// from a project in a Visual Studio solution. 
    /// </summary>
    public class ProjectInfo_CPP : ProjectInfo
    {
        #region Public methods and properties

        /// <summary>
        /// Constructor
        /// </summary>
        public ProjectInfo_CPP()
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
                case ProjectInfo.ProjectTypeEnum.CPP_EXECUTABLE:
                    {
                        // We find the libraries that this executable depends on...
                        List<ImplicitLinkInfo> infos = new List<ImplicitLinkInfo>();
                        findImplicitlyLinkedLibraries(infos);

                        // And add them to the library path...
                        foreach (ImplicitLinkInfo info in infos)
                        {
                            // We find the configuration for this info and add
                            // the library to it...
                            ProjectConfigurationInfo_CPP configuration = getConfigurations().Find((cfg) => (cfg.Name == info.ConfigurationName));
                            if (configuration == null)
                            {
                                // We may fail to find the library to implicitly link
                                // if the configuration names do not match. (For example
                                // if our project has a Debug configuration, and the
                                // library to link has a DebugStatic configuration...
                                Log.log(String.Format("Project {0} could not implicitly link {1}. Could not find the {2} configuration.", Name, info.LibraryRawName, info.ConfigurationName));
                                continue;
                            }

                            // We add the library and the library path...
                            configuration.addLibraryRawName(info.LibraryRawName);
                            string libraryPath = Utils.makeRelativePath(RootFolderAbsolute, info.OutputFolderAbsolute);
                            configuration.addLibraryPath(libraryPath);
                        }
                    }
                    break;

                case ProjectInfo_CPP.ProjectTypeEnum.CPP_STATIC_LIBRARY:
                    {
                        // We find the collection of object files used by any 
                        // libraries this library depends on...
                        List<ImplicitLinkInfo> infos = new List<ImplicitLinkInfo>();
                        findImplicitlyLinkedObjectFiles(infos);

                        // We add the files to the configurations...
                        foreach (ImplicitLinkInfo info in infos)
                        {
                            // We find the configuration for this info...
                            ProjectConfigurationInfo_CPP configuration = getConfigurations().Find((cfg) => (cfg.Name == info.ConfigurationName));

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
        /// Adds a configuration to the project.
        /// </summary>
        public void addConfiguration(ProjectConfigurationInfo_CPP configuration)
        {
            m_configurations.Add(configuration);
        }

        /// <summary>
        /// Gets or the collection of configurations (debug, release etc) for the project.
        /// </summary>
        public List<ProjectConfigurationInfo_CPP> getConfigurations()
        {
            return m_configurations;
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
            foreach (ProjectInfo_CPP requiredProject in getRequiredProjects())
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
                foreach (ProjectConfigurationInfo_CPP configuration in requiredProject.getConfigurations())
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
            foreach (ProjectInfo_CPP requiredProject in getRequiredProjects())
            {
                // Is the required project a static library?
                if (requiredProject.ProjectType != ProjectTypeEnum.CPP_STATIC_LIBRARY)
                {
                    continue;
                }

                // We've found a library, so we add its object files to our collection...
                foreach (ProjectConfigurationInfo_CPP configuration in requiredProject.getConfigurations())
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

        // The collection of configurations (debug, release)...
        private List<ProjectConfigurationInfo_CPP> m_configurations = new List<ProjectConfigurationInfo_CPP>();

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
