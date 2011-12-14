using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.VCProjectEngine;
using System.IO;
using MakeItSoLib;

namespace SolutionParser_VS2008
{
    /// <summary>
    /// Parses a C++ project.
    /// </summary><remarks>
    /// We extract information from a VCProject object, and fill in a Project structure.
    /// </remarks>
    public class ProjectParser_CPP
    {
        #region Public methods and properties

        /// <summary>
        /// Constructor
        /// </summary>
        public ProjectParser_CPP(VCProject vcProject, string solutionRootFolder)
        {
            try
            {
                m_vcProject = vcProject;
                m_solutionRootFolder = solutionRootFolder;

                // We get the project name...
                m_parsedProject.Name = Utils.dteCall<string>(() => (m_vcProject.Name));
                Log.log("- parsing project " + m_parsedProject.Name);

                // and parse the project...
                parseProject();
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
        public ProjectInfo_CPP Project 
        {
            get { return m_parsedProject; }
        }

        #endregion

        #region Private functions

        /// <summary>
        /// Parses the project.
        /// </summary>
        private void parseProject()
        {
            parseProject_RootFolder();
            parseProject_SourceFiles();
            parseProject_Configurations();
        }

        /// <summary>
        /// Finds the configurations, e.g. debug, release etc.
        /// </summary>
        private void parseProject_Configurations()
        {
            // We loop through the collection of configurations for the project...
            IVCCollection configurations = Utils.dteCall<IVCCollection>(() => (m_vcProject.Configurations as IVCCollection));
            int numConfigurations = Utils.dteCall<int>(() => (configurations.Count));
            for (int i = 1; i <= numConfigurations; ++i)
            {
                // We parse this configuration, and add the parsed data to the collection
                // for this project...
                VCConfiguration vcConfiguration = Utils.dteCall<VCConfiguration>(() => (configurations.Item(i) as VCConfiguration));
                parseConfiguration(vcConfiguration);
            }
        }

        /// <summary>
        /// Parses the configuration (e.g. Debug, Release) passed in.
        /// </summary>
        private void parseConfiguration(VCConfiguration vcConfiguration)
        {
            ProjectConfigurationInfo_CPP parsedConfiguration = new ProjectConfigurationInfo_CPP();
            parsedConfiguration.ParentProject = m_parsedProject;

            // The configuration name...
            parsedConfiguration.Name = Utils.dteCall<string>(() => (vcConfiguration.ConfigurationName));

            // The project type. 
            // Note: we are assuming that all the configurations for the project build the
            //       same type of target. 
            m_parsedProject.ProjectType = parseConfiguration_Type(vcConfiguration);

            // We get the intermediates folder and output folder...
            parsedConfiguration.IntermediateFolder = parseConfiguration_Folder(vcConfiguration, () => (vcConfiguration.IntermediateDirectory));
            parsedConfiguration.OutputFolder = parseConfiguration_Folder(vcConfiguration, () => (vcConfiguration.OutputDirectory));

            // We get compiler settings, such as the include path and 
            // preprocessor definitions...
            parseConfiguration_CompilerSettings(vcConfiguration, parsedConfiguration);

            // We get linker settings, such as any libs to link and the library path...
            parseConfiguration_LinkerSettings(vcConfiguration, parsedConfiguration);

            // We parse librarian settings (how libraries are linked)...
            parseConfiguration_LibrarianSettings(vcConfiguration, parsedConfiguration);

            // We add the configuration to the collection of them for the project...
            m_parsedProject.addConfiguration(parsedConfiguration);

        }

        /// <summary>
        /// We parse the librarian settings, ie link options for libraries.
        /// </summary>
        private void parseConfiguration_LibrarianSettings(VCConfiguration vcConfiguration, ProjectConfigurationInfo_CPP parsedConfiguration)
        {
            // We get the librarian 'tool'...
            IVCCollection tools = Utils.dteCall<IVCCollection>(() => (vcConfiguration.Tools as IVCCollection));
            VCLibrarianTool librarianTool = Utils.dteCall<VCLibrarianTool>(() => (tools.Item("VCLibrarianTool") as VCLibrarianTool));
            if (librarianTool == null)
            {
                // Not all projects have a librarian tool...
                return;
            }

            // We find if this library is set to link together other libraries it depends on...
            // (We are assuming that all configurations of the project have the same link-library-dependencies setting.)
            m_parsedProject.LinkLibraryDependencies = Utils.dteCall<bool>(() => (librarianTool.LinkLibraryDependencies));
        }

        /// <summary>
        /// Finds the linker settings, such as the collection of libraries to link,
        /// for the configuration passed in.
        /// </summary>
        private void parseConfiguration_LinkerSettings(VCConfiguration vcConfiguration, ProjectConfigurationInfo_CPP parsedConfiguration)
        {
            // We get the linker-settings 'tool'...
            IVCCollection tools = Utils.dteCall<IVCCollection>(() => (vcConfiguration.Tools as IVCCollection));
            VCLinkerTool linkerTool = Utils.dteCall<VCLinkerTool>(() => (tools.Item("VCLinkerTool") as VCLinkerTool));
            if (linkerTool == null)
            {
                // Not all projects have a linker tools...
                return;
            }

            // And extract various details from it...
            parseLinkerSettings_LibraryPath(vcConfiguration, linkerTool, parsedConfiguration);
            parseLinkerSettings_Libraries(vcConfiguration, linkerTool, parsedConfiguration);
            parseLinkerSettings_Misc(vcConfiguration, linkerTool, parsedConfiguration);
        }

        /// <summary>
        /// Reads miscellaneous linker settings.
        /// </summary>
        private void parseLinkerSettings_Misc(VCConfiguration vcConfiguration, VCLinkerTool linkerTool, ProjectConfigurationInfo_CPP parsedConfiguration)
        {
            // Whether we implicitly link in libraries we depend on.
            // (We are assuming that all configurations of the project have the
            // same link-library-dependencies setting.)
            m_parsedProject.LinkLibraryDependencies = Utils.dteCall<bool>(() => (linkerTool.LinkLibraryDependencies));

            // Generate debug info...
            bool debugInfo = Utils.dteCall<bool>(() => (linkerTool.GenerateDebugInformation));
            if (debugInfo == true 
                && 
                parsedConfiguration.getPreprocessorDefinitions().Contains("NDEBUG") == false)
            {
                parsedConfiguration.addCompilerFlag("-g");
            }
        }

        /// <summary>
        /// Finds the library path for the configuration passed in.
        /// </summary>
        private void parseLinkerSettings_LibraryPath(VCConfiguration vcConfiguration, VCLinkerTool linkerTool, ProjectConfigurationInfo_CPP parsedConfiguration)
        {
            // We:
            // 1. Read the additional library paths (which are in a semi-colon-delimited string)
            // 2. Split it into separate paths
            // 3. Resolve any symbols
            // 4. Make sure all paths are relative to the project root folder

            // 1 & 2...
            string strAdditionalLibraryDirectories = Utils.dteCall<string>(() => (linkerTool.AdditionalLibraryDirectories));
            if (strAdditionalLibraryDirectories == null)
            {
                return;
            }

            // We get the project config, so we can check if paths should be removed...
            MakeItSoConfig_Project projectConfig = MakeItSoConfig.Instance.getProjectConfig(m_parsedProject.Name);

            List<string> additionalLibraryDirectories = Utils.split(strAdditionalLibraryDirectories, ';');
            foreach (string additionalLibraryDirectory in additionalLibraryDirectories)
            {
                // The string may be quoted. We need to remove the quotes...
                string unquotedLibraryDirectory = additionalLibraryDirectory.Trim('"');
                if (unquotedLibraryDirectory == "")
                {
                    continue;
                }

                // 3 & 4...
                string resolvedPath = Utils.dteCall<string>(() => (vcConfiguration.Evaluate(unquotedLibraryDirectory)));
                string relativePath = Utils.makeRelativePath(m_parsedProject.RootFolderAbsolute, resolvedPath);
                parsedConfiguration.addLibraryPath(relativePath);
            }
        }

        /// <summary>
        /// Finds the collection of additional libraries to link into this project.
        /// </summary>
        private void parseLinkerSettings_Libraries(VCConfiguration vcConfiguration, VCLinkerTool linkerTool, ProjectConfigurationInfo_CPP parsedConfiguration)
        {
            // The collection of libraries is stored in a space-delimited string...
            string strAdditionalLibraries = Utils.dteCall<string>(() => (linkerTool.AdditionalDependencies));
            if (strAdditionalLibraries == null)
            {
                return;
            }

            // We get the project config, so we can check if libraries should be removed...
            MakeItSoConfig_Project projectConfig = MakeItSoConfig.Instance.getProjectConfig(m_parsedProject.Name);

            List<string> additionalLibraries = Utils.split(strAdditionalLibraries, ' ');
            foreach(string additionalLibrary in additionalLibraries)
            {
                // We add the library to the project...
                string rawName = Path.GetFileNameWithoutExtension(additionalLibrary);
                parsedConfiguration.addLibraryRawName(rawName);
            }
        }

        /// <summary>
        /// Finds compiler settings, such as the include path, for the configuration
        /// passed in.
        /// </summary>
        private void parseConfiguration_CompilerSettings(VCConfiguration vcConfiguration, ProjectConfigurationInfo_CPP parsedConfiguration)
        {
            // We get the compiler-settings 'tool'...
            IVCCollection tools = Utils.dteCall<IVCCollection>(() => (vcConfiguration.Tools as IVCCollection));
            VCCLCompilerTool compilerTool = Utils.dteCall<VCCLCompilerTool>(() => (tools.Item("VCCLCompilerTool") as VCCLCompilerTool));

            // And extract various details from it...
            parseCompilerSettings_IncludePath(vcConfiguration, compilerTool, parsedConfiguration);
            parseCompilerSettings_PreprocessorDefinitions(vcConfiguration, compilerTool, parsedConfiguration);
            parseCompilerSettings_CompilerFlags(vcConfiguration, compilerTool, parsedConfiguration);
        }

        /// <summary>
        /// Finds compiler flags.
        /// </summary>
        private void parseCompilerSettings_CompilerFlags(VCConfiguration vcConfiguration, VCCLCompilerTool compilerTool, ProjectConfigurationInfo_CPP parsedConfiguration)
        {
            // Warning level...
            warningLevelOption warningLevel = Utils.dteCall<warningLevelOption>(() => (compilerTool.WarningLevel));
            switch (warningLevel)
            {
                case warningLevelOption.warningLevel_0:
                    parsedConfiguration.addCompilerFlag("-w");
                    break;

                case warningLevelOption.warningLevel_4:
                    parsedConfiguration.addCompilerFlag("-Wall");
                    break;
            }

            // Warnings as errors...
            bool warningsAsErrors = Utils.dteCall<bool>(() => (compilerTool.WarnAsError));
            if (warningsAsErrors == true)
            {
                parsedConfiguration.addCompilerFlag("-Werror");
            }

            // Optimization...
            optimizeOption optimization = Utils.dteCall<optimizeOption>(() => (compilerTool.Optimization));
            switch (optimization)
            {
                case optimizeOption.optimizeDisabled:
                    parsedConfiguration.addCompilerFlag("-O0");
                    break;

                case optimizeOption.optimizeMinSpace:
                    parsedConfiguration.addCompilerFlag("-Os");
                    break;

                case optimizeOption.optimizeMaxSpeed:
                    parsedConfiguration.addCompilerFlag("-O2");
                    break;

                case optimizeOption.optimizeFull:
                    parsedConfiguration.addCompilerFlag("-O3");
                    break;
            }
        }

        /// <summary>
        /// Finds the collection of include paths for the configuration passed in.
        /// </summary>
        private void parseCompilerSettings_IncludePath(VCConfiguration vcConfiguration, VCCLCompilerTool compilerTool, ProjectConfigurationInfo_CPP parsedConfiguration)
        {
            // We:
            // 1. Read the additional include paths (which are in a semi-colon-delimited string)
            // 2. Split it into separate paths
            // 3. Resolve any symbols
            // 4. Make sure all paths are relative to the project root folder

            // 1 & 2...
            string strAdditionalIncludeDirectories = Utils.dteCall<string>(() => (compilerTool.AdditionalIncludeDirectories));
            if (strAdditionalIncludeDirectories == null)
            {
                return;
            }

            List<string> additionalIncludeDirectories = Utils.split(strAdditionalIncludeDirectories, ';', ',');
            foreach (string additionalIncludeDirectory in additionalIncludeDirectories)
            {
                // The string may be quoted. We need to remove the quotes...
                string unquotedIncludeDirectory = additionalIncludeDirectory.Trim('"');

                // 3 & 4...
                string resolvedPath = Utils.dteCall<string>(() => (vcConfiguration.Evaluate(unquotedIncludeDirectory)));
                string relativePath = Utils.makeRelativePath(m_parsedProject.RootFolderAbsolute, resolvedPath);

                parsedConfiguration.addIncludePath(relativePath);
            }
        }

        /// <summary>
        /// Finds the collection of preprocessor definitions for the configuration passed in.
        /// </summary>
        private void parseCompilerSettings_PreprocessorDefinitions(VCConfiguration vcConfiguration, VCCLCompilerTool compilerTool, ProjectConfigurationInfo_CPP parsedConfiguration)
        {
            // We read the delimited string of preprocessor definitions, and
            // split them...
            string strPreprocessorDefinitions = Utils.dteCall<string>(() => (compilerTool.PreprocessorDefinitions));
            List<string> preprocessorDefinitions = Utils.split(strPreprocessorDefinitions, ';');

            // We find project and configuration config to see if any 
            // definitions should be added or removed...
            MakeItSoConfig_Project projectConfig = MakeItSoConfig.Instance.getProjectConfig(parsedConfiguration.ParentProject.Name);
            MakeItSoConfig_Configuration configurationConfig = projectConfig.getConfiguration(parsedConfiguration.Name);

            // We add the definitions to the parsed configuration (removing ones that 
            // aren't relevant to a linux build)...
            foreach(string definition in preprocessorDefinitions)
            {
                parsedConfiguration.addPreprocessorDefinition(definition);
            }
        }

        /// <summary>
        /// Gets the configuration type.
        /// </summary>
        private ProjectInfo.ProjectTypeEnum parseConfiguration_Type(VCConfiguration vcConfiguration)
        {
            ProjectInfo.ProjectTypeEnum result = ProjectInfo.ProjectTypeEnum.INVALID;

            // We get the Visual Studio confiuration type...
            ConfigurationTypes configurationType = Utils.dteCall<ConfigurationTypes>(() => (vcConfiguration.ConfigurationType));

            // And convert it to our enum type...
            switch (configurationType)
            {
                case ConfigurationTypes.typeApplication:
                    result = ProjectInfo.ProjectTypeEnum.CPP_EXECUTABLE;
                    break;

                case ConfigurationTypes.typeStaticLibrary:
                    result = ProjectInfo.ProjectTypeEnum.CPP_STATIC_LIBRARY;
                    break;

                case ConfigurationTypes.typeDynamicLibrary:
                    result = ProjectInfo.ProjectTypeEnum.CPP_DLL;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Gets a folder name from the function passed in, and returns it as a path
        /// relative to the project root folder.
        /// </summary>
        private string parseConfiguration_Folder(VCConfiguration vcConfiguration, Func<string> folderFn)
        {
            // We get the folder name, which may contain symbols e.g. £(ConfgurationName)...
            string pathWithSymbols = Utils.dteCall<string>(folderFn);

            // We resolve the symbols...
            string evaluatedPath = Utils.dteCall<string>(() => (vcConfiguration.Evaluate(pathWithSymbols)));

            // If we ave an absolute path, we convert it to a relative one...
            string relativePath = evaluatedPath;
            if (Path.IsPathRooted(evaluatedPath))
            {
                relativePath = Utils.makeRelativePath(m_parsedProject.RootFolderAbsolute, evaluatedPath);
            }

            return relativePath;
        }

        /// <summary>
        /// Finds the project's root folder.
        /// </summary>
        private void parseProject_RootFolder()
        {
            // The project root folder, both abolute and relative to the solution root...
            m_parsedProject.RootFolderAbsolute = Utils.dteCall<string>(() => (m_vcProject.ProjectDirectory));
            m_parsedProject.RootFolderRelative = Utils.makeRelativePath(m_solutionRootFolder, m_parsedProject.RootFolderAbsolute);
        }

        /// <summary>
        /// Finds the collection of source files in the project.
        /// </summary>
        private void parseProject_SourceFiles()
        {
            // We loop through the collection of files in the project...
            IVCCollection files = Utils.dteCall<IVCCollection>(() => (m_vcProject.Files as IVCCollection));
            int numFiles = Utils.dteCall<int>(() => (files.Count));
            for (int i = 1; i <= numFiles; ++i)
            {
                // We get one file...
                VCFile file = Utils.dteCall<VCFile>(() => (files.Item(i) as VCFile));
                string path = Utils.dteCall<string>(() => (file.FullPath));

                // We find the extension, and see if it is one we treat 
                // as a source file...
                string extension = Path.GetExtension(path).ToLower();
                switch (extension)
                {
                    // It looks like a source file...
                    case ".cpp":
                    case ".c":
                        // We add it to the project...
                        string relativePath = Utils.makeRelativePath(m_parsedProject.RootFolderAbsolute, path);
                        m_parsedProject.addFile(relativePath);
                        break;
                }
            }
        }

        #endregion

        #region Private data

        // Holds the parsed project data...
        private ProjectInfo_CPP m_parsedProject = new ProjectInfo_CPP();

        // The root folder of the solution that this project is part of...
        private string m_solutionRootFolder = "";

        // The Visual Studio project object...
        private VCProject m_vcProject = null;

        #endregion
    }
}
