using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.VCProjectEngine;
using MakeItSoLib;
using System.IO;

namespace SolutionParser_VS2010
{
    /// <summary>
    /// Parses a C++ project.
    /// </summary><remarks>
    /// We extract information from a VCProject object, and fill in a  ProjectInfo structure.
    /// </remarks>
    internal class ProjectParser_CPP
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
                m_projectInfo.Name = Utils.call(() => (m_vcProject.Name));
                Log.log("- parsing project " + m_projectInfo.Name);

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
            get { return m_projectInfo; }
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
            IVCCollection configurations = Utils.call(() => (m_vcProject.Configurations as IVCCollection));
            int numConfigurations = Utils.call(() => (configurations.Count));
            for (int i = 1; i <= numConfigurations; ++i)
            {
                // We parse this configuration, and add the parsed data to the collection
                // for this project...
                VCConfiguration vcConfiguration = Utils.call(() => (configurations.Item(i) as VCConfiguration));
                parseConfiguration(vcConfiguration);
            }
        }

        /// <summary>
        /// Parses the configuration (e.g. Debug, Release) passed in.
        /// </summary>
        private void parseConfiguration(VCConfiguration vcConfiguration)
        {
            ProjectConfigurationInfo_CPP configurationInfo = new ProjectConfigurationInfo_CPP();
            configurationInfo.ParentProjectInfo = m_projectInfo;

            // The configuration name...
            configurationInfo.Name = Utils.call(() => (vcConfiguration.ConfigurationName));

            // The project type. 
            // Note: we are assuming that all the configurations for the project build the
            //       same type of target. 
            m_projectInfo.ProjectType = parseConfiguration_Type(vcConfiguration);

            // We get the intermediates folder and output folder...
            configurationInfo.IntermediateFolder = parseConfiguration_Folder(vcConfiguration, () => (vcConfiguration.IntermediateDirectory));
            configurationInfo.OutputFolder = parseConfiguration_Folder(vcConfiguration, () => (vcConfiguration.OutputDirectory));

            // We get compiler settings, such as the include path and 
            // preprocessor definitions...
            parseConfiguration_CompilerSettings(vcConfiguration, configurationInfo);

            // We get linker settings, such as any libs to link and the library path...
            parseConfiguration_LinkerSettings(vcConfiguration, configurationInfo);

            // We parse librarian settings (how libraries are linked)...
            parseConfiguration_LibrarianSettings(vcConfiguration, configurationInfo);

            // We add the configuration to the collection of them for the project...
            m_projectInfo.addConfigurationInfo(configurationInfo);
        }

        /// <summary>
        /// We parse the librarian settings, ie link options for libraries.
        /// </summary>
        private void parseConfiguration_LibrarianSettings(VCConfiguration vcConfiguration, ProjectConfigurationInfo_CPP configurationInfo)
        {
            // We get the librarian 'tool'...
            IVCCollection tools = Utils.call(() => (vcConfiguration.Tools as IVCCollection));
            VCLibrarianTool librarianTool = Utils.call(() => (tools.Item("VCLibrarianTool") as VCLibrarianTool));
            if (librarianTool == null)
            {
                // Not all projects have a librarian tool...
                return;
            }

            // We find if this library is set to link together other libraries it depends on...
            // (We are assuming that all configurations of the project have the same link-library-dependencies setting.)
            m_projectInfo.LinkLibraryDependencies = Utils.call(() => (librarianTool.LinkLibraryDependencies));
        }

        /// <summary>
        /// Finds the linker settings, such as the collection of libraries to link,
        /// for the configuration passed in.
        /// </summary>
        private void parseConfiguration_LinkerSettings(VCConfiguration vcConfiguration, ProjectConfigurationInfo_CPP configurationInfo)
        {
            // We get the linker-settings 'tool'...
            IVCCollection tools = Utils.call(() => (vcConfiguration.Tools as IVCCollection));
            VCLinkerTool linkerTool = Utils.call(() => (tools.Item("VCLinkerTool") as VCLinkerTool));
            if (linkerTool == null)
            {
                // Not all projects have a linker tools...
                return;
            }

            // And extract various details from it...
            parseLinkerSettings_LibraryPath(vcConfiguration, linkerTool, configurationInfo);
            parseLinkerSettings_Libraries(vcConfiguration, linkerTool, configurationInfo);
            parseLinkerSettings_Misc(vcConfiguration, linkerTool, configurationInfo);
        }

        /// <summary>
        /// Reads miscellaneous linker settings.
        /// </summary>
        private void parseLinkerSettings_Misc(VCConfiguration vcConfiguration, VCLinkerTool linkerTool, ProjectConfigurationInfo_CPP configurationInfo)
        {
            // Whether we implicitly link in libraries we depend on.
            // (We are assuming that all configurations of the project have the
            // same link-library-dependencies setting.)
            m_projectInfo.LinkLibraryDependencies = Utils.call(() => (linkerTool.LinkLibraryDependencies));

            // Generate debug info...
            bool debugInfo = Utils.call(() => (linkerTool.GenerateDebugInformation));
            if (debugInfo == true
                &&
                configurationInfo.getPreprocessorDefinitions().Contains("NDEBUG") == false)
            {
                configurationInfo.addCompilerFlag("-g");
            }
        }

        /// <summary>
        /// Finds the library path for the configuration passed in.
        /// </summary>
        private void parseLinkerSettings_LibraryPath(VCConfiguration vcConfiguration, VCLinkerTool linkerTool, ProjectConfigurationInfo_CPP configurationInfo)
        {
            // We:
            // 1. Read the additional library paths (which are in a semi-colon-delimited string)
            // 2. Split it into separate paths
            // 3. Resolve any symbols
            // 4. Make sure all paths are relative to the project root folder

            // 1 & 2...
            string strAdditionalLibraryDirectories = Utils.call(() => (linkerTool.AdditionalLibraryDirectories));
            if (strAdditionalLibraryDirectories == null)
            {
                return;
            }

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
                string resolvedPath = Utils.call(() => (vcConfiguration.Evaluate(unquotedLibraryDirectory)));
                if (resolvedPath != "")
                {
                    string relativePath = Utils.makeRelativePath(m_projectInfo.RootFolderAbsolute, resolvedPath);
                    configurationInfo.addLibraryPath(relativePath);
                }
            }
        }

        /// <summary>
        /// Finds the collection of additional libraries to link into this project.
        /// </summary>
        private void parseLinkerSettings_Libraries(VCConfiguration vcConfiguration, VCLinkerTool linkerTool, ProjectConfigurationInfo_CPP configurationInfo)
        {
            // The collection of libraries is stored in a space-delimited string...
            string strAdditionalLibraries = Utils.call(() => (linkerTool.AdditionalDependencies));
            if (strAdditionalLibraries == null)
            {
                return;
            }

            List<string> additionalLibraries = Utils.split(strAdditionalLibraries, ' ');
            foreach (string additionalLibrary in additionalLibraries)
            {
                // We add the library to the project...
                string rawName = Path.GetFileNameWithoutExtension(additionalLibrary);
                configurationInfo.addLibraryRawName(rawName);
            }
        }

        /// <summary>
        /// Finds compiler settings, such as the include path, for the configuration
        /// passed in.
        /// </summary>
        private void parseConfiguration_CompilerSettings(VCConfiguration vcConfiguration, ProjectConfigurationInfo_CPP configurationInfo)
        {
            // We get the compiler-settings 'tool'...
            IVCCollection tools = Utils.call(() => (vcConfiguration.Tools as IVCCollection));
            VCCLCompilerTool compilerTool = Utils.call(() => (tools.Item("VCCLCompilerTool") as VCCLCompilerTool));

            // And extract various details from it...
            parseCompilerSettings_IncludePath(vcConfiguration, compilerTool, configurationInfo);
            parseCompilerSettings_PreprocessorDefinitions(vcConfiguration, compilerTool, configurationInfo);
            parseCompilerSettings_CompilerFlags(vcConfiguration, compilerTool, configurationInfo);
        }

        /// <summary>
        /// Finds compiler flags.
        /// </summary>
        private void parseCompilerSettings_CompilerFlags(VCConfiguration vcConfiguration, VCCLCompilerTool compilerTool, ProjectConfigurationInfo_CPP configurationInfo)
        {
            // Warning level...
            warningLevelOption warningLevel = Utils.call(() => (compilerTool.WarningLevel));
            switch (warningLevel)
            {
                case warningLevelOption.warningLevel_0:
                    configurationInfo.addCompilerFlag("-w");
                    break;

                case warningLevelOption.warningLevel_4:
                    configurationInfo.addCompilerFlag("-Wall");
                    break;
            }

            // Warnings as errors...
            bool warningsAsErrors = Utils.call(() => (compilerTool.WarnAsError));
            if (warningsAsErrors == true)
            {
                configurationInfo.addCompilerFlag("-Werror");
            }

            // Optimization...
            optimizeOption optimization = Utils.call(() => (compilerTool.Optimization));
            switch (optimization)
            {
                case optimizeOption.optimizeDisabled:
                    configurationInfo.addCompilerFlag("-O0");
                    break;

                case optimizeOption.optimizeMinSpace:
                    configurationInfo.addCompilerFlag("-Os");
                    break;

                case optimizeOption.optimizeMaxSpeed:
                    configurationInfo.addCompilerFlag("-O2");
                    break;

                case optimizeOption.optimizeFull:
                    configurationInfo.addCompilerFlag("-O3");
                    break;
            }
        }

        /// <summary>
        /// Finds the collection of include paths for the configuration passed in.
        /// </summary>
        private void parseCompilerSettings_IncludePath(VCConfiguration vcConfiguration, VCCLCompilerTool compilerTool, ProjectConfigurationInfo_CPP configurationInfo)
        {
            // We:
            // 1. Read the additional include paths (which are in a semi-colon-delimited string)
            // 2. Split it into separate paths
            // 3. Resolve any symbols
            // 4. Make sure all paths are relative to the project root folder

            // 1 & 2...
            string strAdditionalIncludeDirectories = Utils.call(() => (compilerTool.AdditionalIncludeDirectories));
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
                string resolvedPath = Utils.call(() => (vcConfiguration.Evaluate(unquotedIncludeDirectory)));
                if (resolvedPath != "")
                {
                    string relativePath = Utils.makeRelativePath(m_projectInfo.RootFolderAbsolute, resolvedPath);
                    configurationInfo.addIncludePath(relativePath);
                }
            }
        }

        /// <summary>
        /// Finds the collection of preprocessor definitions for the configuration passed in.
        /// </summary>
        private void parseCompilerSettings_PreprocessorDefinitions(VCConfiguration vcConfiguration, VCCLCompilerTool compilerTool, ProjectConfigurationInfo_CPP configurationInfo)
        {
            // We read the delimited string of preprocessor definitions, and
            // split them...
            string strPreprocessorDefinitions = Utils.call(() => (compilerTool.PreprocessorDefinitions));
            List<string> preprocessorDefinitions = Utils.split(strPreprocessorDefinitions, ';');

            // We add the definitions to the parsed configuration (removing ones that 
            // aren't relevant to a linux build)...
            foreach (string definition in preprocessorDefinitions)
            {
                configurationInfo.addPreprocessorDefinition(definition);
            }
        }

        /// <summary>
        /// Gets the configuration type.
        /// </summary>
        private ProjectInfo.ProjectTypeEnum parseConfiguration_Type(VCConfiguration vcConfiguration)
        {
            ProjectInfo.ProjectTypeEnum result = ProjectInfo.ProjectTypeEnum.INVALID;

            // We get the Visual Studio confiuration type...
            ConfigurationTypes configurationType = Utils.call(() => (vcConfiguration.ConfigurationType));

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
            string pathWithSymbols = Utils.call(folderFn);

            // We resolve the symbols...
            string evaluatedPath = Utils.call(() => (vcConfiguration.Evaluate(pathWithSymbols)));

            // If we ave an absolute path, we convert it to a relative one...
            string relativePath = evaluatedPath;
            if (Path.IsPathRooted(evaluatedPath))
            {
                relativePath = Utils.makeRelativePath(m_projectInfo.RootFolderAbsolute, evaluatedPath);
            }

            return relativePath;
        }

        /// <summary>
        /// Finds the project's root folder.
        /// </summary>
        private void parseProject_RootFolder()
        {
            // The project root folder, both absolute and relative to the solution root...
            m_projectInfo.RootFolderAbsolute = Utils.call(() => (m_vcProject.ProjectDirectory));
            m_projectInfo.RootFolderRelative = Utils.makeRelativePath(m_solutionRootFolder, m_projectInfo.RootFolderAbsolute);
        }

        /// <summary>
        /// Finds the collection of source files in the project.
        /// </summary>
        private void parseProject_SourceFiles()
        {
            // We loop through the collection of files in the project...
            IVCCollection files = Utils.call(() => (m_vcProject.Files as IVCCollection));
            int numFiles = Utils.call(() => (files.Count));
            for (int i = 1; i <= numFiles; ++i)
            {
                // We get one file...
                VCFile file = Utils.call(() => (files.Item(i) as VCFile));
                string path = Utils.call(() => (file.FullPath));

                // We find the extension, and see if it is one we treat 
                // as a source file...
                string extension = Path.GetExtension(path).ToLower();
                switch (extension)
                {
                    // It looks like a source file...
                    case ".cpp":
                    case ".c":
                    case ".cc":
                    case ".cp":
                    case ".cxx":
                    case ".c++":
                        // We add it to the project...
                        string relativePath = Utils.makeRelativePath(m_projectInfo.RootFolderAbsolute, path);
                        m_projectInfo.addFile(relativePath);
                        break;
                }
            }
        }

        #endregion

        #region Private data

        // Holds the parsed project data...
        private ProjectInfo_CPP m_projectInfo = new ProjectInfo_CPP();

        // The root folder of the solution that this project is part of...
        private string m_solutionRootFolder = "";

        // The Visual Studio project object...
        private VCProject m_vcProject = null;

        #endregion
    }
}
