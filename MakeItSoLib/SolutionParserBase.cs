using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MakeItSoLib
{
    /// <summary>
    /// An base class for solution-parsers.
    /// </summary><remarks>
    /// To parse a new type of solution (e.g. VS2008, VS2010 etc) you should:
    /// - Create an assembly for the parser.
    /// - Create a class in it that implements ISolutionParser
    /// - Register the class with te main MakeItSo project
    /// </remarks>
    public abstract class SolutionParserBase
    {
        #region Abstract methods

        /// <summary>
        /// Parses the solution in the file passed in.
        /// </summary>
        public abstract void parse(string solutionFilename);

        #endregion

        #region Public methods and properties

        /// <summary>
        /// Returns the parsed solution.
        /// </summary>
        public Solution ParsedSolution
        {
            get { return m_parsedSolution; }
        }

        /// <summary>
        /// Updates the solution with any changes specified in the MakeItSo.config file.
        /// </summary>
        public void updateSolutionFromConfig()
        {
            // We check each configuration in each project...
            foreach (Project project in m_parsedSolution.getProjects())
            {
                foreach (ProjectConfiguration configuration in project.getConfigurations())
                {
                    updateLibraries(configuration);
                    updateLibraryPaths(configuration);
                    updatePreprocessorDefinitions(configuration);
                    updateCompilerFlags(configuration);
                }
            }
        }

        #endregion

        #region Private functions

        /// <summary>
        /// Updates compiler flags from the config settings.
        /// </summary>
        private void updateCompilerFlags(ProjectConfiguration configuration)
        {
            MakeItSoConfig_Project projectSettings = MakeItSoConfig.Instance.getProjectConfig(configuration.ParentProject.Name);

            // We check if any definitions should be removed...
            List<string> flags = new List<string>(configuration.getCompilerFlags());
            foreach (string flag in flags)
            {
                if (projectSettings.compilerFlagShouldBeRemoved(flag) == true)
                {
                    configuration.removeCompilerFlag(flag);
                }
            }

            // We add any new definitions...
            List<string> flagsToAdd = projectSettings.getConfiguration(configuration.Name).getCompilerFlagsToAdd();
            foreach (string flag in flagsToAdd)
            {
                configuration.addCompilerFlag(flag);
            }
        }

        /// <summary>
        /// Updates preprocessor definitions from config settings.
        /// </summary>
        private void updatePreprocessorDefinitions(ProjectConfiguration configuration)
        {
            MakeItSoConfig_Project projectSettings = MakeItSoConfig.Instance.getProjectConfig(configuration.ParentProject.Name);

            // By default we replace WIN32 with GCC_BUILD...
            configuration.removePreprocessorDefinition("WIN32");
            configuration.addPreprocessorDefinition("GCC_BUILD");

            // We check if any definitions should be removed...
            List<string> definitions = new List<string>(configuration.getPreprocessorDefinitions());
            foreach (string definition in definitions)
            {
                if(projectSettings.preprocessorDefinitionShouldBeRemoved(definition) == true)
                {
                    configuration.removePreprocessorDefinition(definition);
                }
            }

            // We add any new definitions...
            List<string> definitionsToAdd = projectSettings.getConfiguration(configuration.Name).getPreprocessorDefinitionsToAdd();
            foreach (string definition in definitionsToAdd)
            {
                configuration.addPreprocessorDefinition(definition);
            }
        }

        /// <summary>
        /// Updates library paths from config settings.
        /// </summary>
        private void updateLibraryPaths(ProjectConfiguration configuration)
        {
            MakeItSoConfig_Project projectSettings = MakeItSoConfig.Instance.getProjectConfig(configuration.ParentProject.Name);

            string projectRootFolder = configuration.ParentProject.RootFolderAbsolute;

            // We check if any library paths should be removed...
            List<string> libraryPaths = new List<string>(configuration.getLibraryPaths());
            foreach(string libraryPath in libraryPaths)
            {
                // We remove the library (and re-add it if we need to, but
                // with the name changed)...
                configuration.removeLibraryPath(libraryPath);

                // We find the full path, and add it if we are not
                // configured to remove it...
                string fullPath = Path.Combine(projectRootFolder, libraryPath);
                if (projectSettings.libraryPathShouldBeRemoved(fullPath) == false)
                {
                    string gccPath = Utils.addPrefixToFolder(libraryPath, "gcc");
                    configuration.addLibraryPath(gccPath);
                }
            }

            // We add any new paths...
            List<string> pathsToAdd = projectSettings.getConfiguration(configuration.Name).getLibraryPathsToAdd();
            foreach(string pathToAdd in pathsToAdd)
            {
                string relativePath = Utils.makeRelativePath(projectRootFolder, pathToAdd);
                configuration.addLibraryPath(relativePath);
            }
        }

        /// <summary>
        /// Updates libraries from config settings.
        /// </summary>
        private void updateLibraries(ProjectConfiguration configuration)
        {
            MakeItSoConfig_Project projectSettings = MakeItSoConfig.Instance.getProjectConfig(configuration.ParentProject.Name);

            // We check if any of the libraries in the configuration should be removed...
            HashSet<string> libraries = new HashSet<string>(configuration.getLibraryRawNames());
            foreach (string library in libraries)
            {
                if (projectSettings.libraryShouldBeRemoved(library) == true)
                {
                    configuration.removeLibraryRawName(library);
                }
            }

            // We add any that need adding...
            List<string> librariesToAdd = projectSettings.getConfiguration(configuration.Name).getLibrariesToAdd();
            foreach (string library in librariesToAdd)
            {
                string rawName = Utils.convertLinuxLibraryNameToRawName(library);
                configuration.addLibraryRawName(rawName);
            }
        }

        #endregion

        #region Protected data

        // Holds the parsed solution data, including the 
        // collection of projects in it...
        protected Solution m_parsedSolution = new Solution();

        #endregion
    }
}
