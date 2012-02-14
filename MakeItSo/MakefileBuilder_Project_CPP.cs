using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MakeItSoLib;

namespace MakeItSo
{
    /// <summary>
    /// Creates a makefile for one C++ project in the solution.
    /// </summary><remarks>
    /// Project makefiles have the name [project-name].makefile. They will
    /// mostly be invoked from the 'master' makefile at the solution root.
    /// 
    /// These makefiles have:
    /// - One main target for each configuration (e.g. debug, release) in the project
    /// - A default target that builds them all
    /// - A 'clean' target
    /// 
    ///   .PHONY: build_all_configurations
    ///   build_all_configurations: Debug Release
    ///   
    ///   .PHONY: Debug
    ///   Debug: debug/main.o debug/math.o debug/utility.o
    ///       g++ debug/main.o debug/math.o debug/utility.o -o output/hello.exe
    ///       
    ///   (And similarly for the Release configuration.)
    ///   
    /// We build the source files once for each configuration. For each one, we also
    /// build a dependency file, which we include if it is available.
    /// 
    ///   -include debug/main.d
    ///   main.o: main.cpp
    ///       g++ -c main.cpp -o debug/main.o
    ///       g++ -MM main.cpp > debug/main.d
    /// 
    /// </remarks>
    class MakefileBuilder_Project_CPP
    {
        #region Public methods and properties

        /// <summary>
        /// We create a makefile for the project passed in.
        /// </summary>
        public static void createMakefile(ProjectInfo_CPP project)
        {
            new MakefileBuilder_Project_CPP(project);
        }

        #endregion

        #region Private functions

        /// <summary>
        /// Constructor
        /// </summary>
        private MakefileBuilder_Project_CPP(ProjectInfo_CPP project)
        {
            m_projectInfo = project;
            try
            {
                // We create the file '[project-name].makefile', and set it to 
                // use unix-style line endings...
                string path = String.Format("{0}/{1}.makefile", m_projectInfo.RootFolderAbsolute, m_projectInfo.Name);
                m_file = new StreamWriter(path, false);
                m_file.NewLine = "\n";

                // We create variables...
                createCompilerVariables();
                createIncludePathVariables();
                createLibraryPathVariables();
                createLibrariesVariables();
                createPreprocessorDefinitionsVariables();
                createImplicitlyLinkedObjectsVariables();
                createCompilerFlagsVariables();

                // We create an 'all configurations' root target...
                createAllConfigurationsTarget();

                // We create one target for each configuration...
                createConfigurationTargets();

                // We create a target to create the intermediate and output folders...
                createCreateFoldersTarget();

                // Creates the target that cleans intermediate files...
                createCleanTarget();
            }
            finally
            {
                if (m_file != null)
                {
                    m_file.Close();
                    m_file.Dispose();
                }
            }
        }

        /// <summary>
        /// We define which compilers we will use.
        /// </summary>
        private void createCompilerVariables()
        {
            // We create an collection of compiler flags for each configuration...
            m_file.WriteLine("# Compiler flags...");

            MakeItSoConfig_Project projectConfig = MakeItSoConfig.Instance.getProjectConfig(m_projectInfo.Name);
            m_file.WriteLine("CPP_COMPILER = " + projectConfig.CPPCompiler);
            m_file.WriteLine("C_COMPILER = " + projectConfig.CCompiler);
            m_file.WriteLine("");
        }

        /// <summary>
        /// Creates variables for the compiler flags for each configuration.
        /// </summary>
        private void createCompilerFlagsVariables()
        {
            // We create an collection of compiler flags for each configuration...
            m_file.WriteLine("# Compiler flags...");
            foreach (ProjectConfigurationInfo_CPP configuration in m_projectInfo.getConfigurationInfos())
            {
                // The variable name...
                string variableName = getCompilerFlagsVariableName(configuration);

                // The flags...
                string flags = "";

                // If we are creating a DLL, we need the create-position-indepent-code flag
                // (unless this is a cygwin build, which doesn't)...
                if (configuration.ParentProjectInfo.ProjectType == ProjectInfo_CPP.ProjectTypeEnum.CPP_DLL
                    &&
                    MakeItSoConfig.Instance.IsCygwinBuild == false)
                {
                    flags += "-fPIC ";
                }

                foreach (string flag in configuration.getCompilerFlags())
                {
                    flags += (flag + " ");
                }

                // We write the variable...
                m_file.WriteLine("{0}={1}", variableName, flags);
            }

            m_file.WriteLine("");
        }

        /// <summary>
        /// Creates variables to hold the preprocessor defintions for each
        /// configuration we're building.
        /// </summary>
        private void createPreprocessorDefinitionsVariables()
        {
            // We create an collection of preprocessor-definitions
            // for each configuration...
            m_file.WriteLine("# Preprocessor definitions...");
            foreach (ProjectConfigurationInfo_CPP configuration in m_projectInfo.getConfigurationInfos())
            {
                // The variable name...
                string variableName = getPreprocessorDefinitionsVariableName(configuration);

                // The definitions...
                string definitions = "";
                foreach (string definition in configuration.getPreprocessorDefinitions())
                {
                    definitions += String.Format("-D {0} ", definition);
                }

                // We write the variable...
                m_file.WriteLine("{0}={1}", variableName, definitions);
            }

            m_file.WriteLine("");
        }

        /// <summary>
        /// Creates variables to hold the collection of object files to implicitly
        /// link into some libraries.
        /// </summary>
        private void createImplicitlyLinkedObjectsVariables()
        {
            // We create an collection of implicitly linked object files
            // for each configuration...
            m_file.WriteLine("# Implictly linked object files...");
            foreach (ProjectConfigurationInfo_CPP configuration in m_projectInfo.getConfigurationInfos())
            {
                // The variable name...
                string variableName = getImplicitlyLinkedObjectsVariableName(configuration);

                // The objects...
                string objectFiles = "";
                foreach (string objectFile in configuration.getImplicitlyLinkedObjectFiles())
                {
                    objectFiles += Utils.quoteAndSpace(objectFile);
                }

                // We write the variable...
                m_file.WriteLine("{0}={1}", variableName, objectFiles);
            }

            m_file.WriteLine("");
        }

        /// <summary>
        /// We create include path variables for the various configurations.
        /// </summary>
        private void createIncludePathVariables()
        {
            // We create an include path for each configuration...
            m_file.WriteLine("# Include paths...");
            foreach (ProjectConfigurationInfo_CPP configuration in m_projectInfo.getConfigurationInfos())
            {
                // The variable name...
                string variableName = getIncludePathVariableName(configuration);

                // The include path...
                string includePath = "";
                foreach (string path in configuration.getIncludePaths())
                {
                    includePath += String.Format("-I{0} ", Utils.quote(path));
                }

                // We write the variable...
                m_file.WriteLine("{0}={1}", variableName, includePath);
            }

            m_file.WriteLine("");
        }

        /// <summary>
        /// We create library path variables for the various configurations.
        /// </summary>
        private void createLibraryPathVariables()
        {
            // We create a library path for each configuration...
            m_file.WriteLine("# Library paths...");
            foreach (ProjectConfigurationInfo_CPP configuration in m_projectInfo.getConfigurationInfos())
            {
                // The variable name...
                string variableName = getLibraryPathVariableName(configuration);

                // The library path...
                string libraryPath = "";
                foreach (string path in configuration.getLibraryPaths())
                {
                    libraryPath += String.Format("-L{0} ", Utils.quote(path));
                }

                // We write the variable...
                m_file.WriteLine("{0}={1}", variableName, libraryPath);
            }

            m_file.WriteLine("");
        }

        /// <summary>
        /// Creates variables that hold the list of additional libraries
        /// for each configuration.
        /// </summary>
        private void createLibrariesVariables()
        {
            // We create a library path for each configuration...
            m_file.WriteLine("# Additional libraries...");
            foreach (ProjectConfigurationInfo_CPP configuration in m_projectInfo.getConfigurationInfos())
            {
                // The variable name...
                string variableName = getLibrariesVariableName(configuration);

                // The libraries...
                string libraries = "";
                foreach (string libraryName in configuration.getLibraryRawNames())
                {
                    libraries += String.Format("-l{0} ", libraryName);
                }

                // If we have some libraries, we surround them with start-group
                // and end-group tags. This is needed as otherwise gcc is sensitive
                // to the order than libraries are declared...
                if (libraries != "")
                {
                    libraries = String.Format("-Wl,--start-group {0} -Wl,--end-group", libraries);
                }

                // We write the variable...
                m_file.WriteLine("{0}={1}", variableName, libraries);
            }

            m_file.WriteLine("");
        }

        /// <summary>
        /// Returns the implictly-linked-objects variable name for the configuration passed in.
        /// For example "Debug_Implicitly_Linked_Objects".
        /// </summary>
        private string getImplicitlyLinkedObjectsVariableName(ProjectConfigurationInfo_CPP configuration)
        {
            return configuration.Name + "_Implicitly_Linked_Objects";
        }

        /// <summary>
        /// Returns the include-path variable name for the configuration passed in.
        /// For example "Debug_Include_Path".
        /// </summary>
        private string getIncludePathVariableName(ProjectConfigurationInfo_CPP configuration)
        {
            return configuration.Name + "_Include_Path";
        }

        /// <summary>
        /// Returns the library-path variable name for the configuration passed in.
        /// For example "Debug_Library_Path".
        /// </summary>
        private string getLibraryPathVariableName(ProjectConfigurationInfo_CPP configuration)
        {
            return configuration.Name + "_Library_Path";
        }

        /// <summary>
        /// Returns the libraries variable name for the configuration passed in.
        /// For example "Debug_Libraries".
        /// </summary>
        private string getLibrariesVariableName(ProjectConfigurationInfo_CPP configuration)
        {
            return configuration.Name + "_Libraries";
        }

        /// <summary>
        /// Returns the preprocessor-definitions variable name for the configuration passed in.
        /// For example "Debug_Preprocessor_Definitions".
        /// </summary>
        private string getPreprocessorDefinitionsVariableName(ProjectConfigurationInfo_CPP configuration)
        {
            return configuration.Name + "_Preprocessor_Definitions";
        }

        /// <summary>
        /// Returns the compiler-flags variable name for the configuration passed in.
        /// For example "Debug_Compiler_Flags".
        /// </summary>
        private string getCompilerFlagsVariableName(ProjectConfigurationInfo_CPP configuration)
        {
            return configuration.Name + "_Compiler_Flags";
        }

        /// <summary>
        /// Creates the default target, to build all configurations
        /// </summary>
        private void createAllConfigurationsTarget()
        {
            // We create a list of the configuration names...
            string strConfigurations = "";
            foreach (ProjectConfigurationInfo_CPP configuration in m_projectInfo.getConfigurationInfos())
            {
                strConfigurations += (configuration.Name + " ");
            }

            // And create a target that depends on both configurations...
            m_file.WriteLine("# Builds all configurations for this project...");
            m_file.WriteLine(".PHONY: build_all_configurations");
            m_file.WriteLine("build_all_configurations: {0}", strConfigurations);
            m_file.WriteLine("");
        }

        /// <summary>
        /// Creates a target for each configuration.
        /// </summary>
        private void createConfigurationTargets()
        {
            foreach (ProjectConfigurationInfo_CPP configuration in m_projectInfo.getConfigurationInfos())
            {
                // We create the configuration target...
                createConfigurationTarget(configuration);

                // We run any custom build rules...
                createCustomBuildRuleTargets(configuration);

                // We compile all files for this target...
                createFileTargets(configuration);
            }
        }

        /// <summary>
        /// We create targets to build any custom build rules associated with 
        /// this configuration.
        /// </summary>
        private void createCustomBuildRuleTargets(ProjectConfigurationInfo_CPP configuration)
        {
            foreach(CustomBuildRuleInfo_CPP ruleInfo in configuration.getCustomBuildRuleInfos())
            {
                createCustomBuildRuleTarget(configuration, ruleInfo);
            }
        }

        /// <summary>
        /// Creates a target for one custom build rule.
        /// </summary>
        private void createCustomBuildRuleTarget(ProjectConfigurationInfo_CPP configuration, CustomBuildRuleInfo_CPP ruleInfo)
        {
            m_file.WriteLine("# Custom build rule for " + ruleInfo.RelativePathToFile);

            string targetName = getCustomRuleTargetName(configuration, ruleInfo);
            m_file.WriteLine(".PHONY: " + targetName);
            m_file.WriteLine(targetName + ":");
            m_file.WriteLine("\t" + ruleInfo.getCommandLine());

            // We find the path to the executable for the rule...
            string executablePath = Path.Combine(configuration.ParentProjectInfo.RootFolderAbsolute, ruleInfo.RelativePathToExecutable);

            // The rule might be built by one of the other projects in this solution.
            // If so, we need to change the folder name to the adjusted output folder
            // name we generate. (This means that we need to know if the project that
            // generates it is a C++ or C# project.)
            ProjectInfo.ProjectTypeEnum projectType = m_projectInfo.ParentSolution.isOutputObject(executablePath);


            //string commandLine = m_relativePathToExecutable;
            //foreach (string parameter in m_parameters)
            //{
            //    commandLine += (" " + parameter);
            //}


            m_file.WriteLine("");
        }

        /// <summary>
        /// Gets the target name for the configuration and rule passed in.
        /// </summary>
        private string getCustomRuleTargetName(ProjectConfigurationInfo_CPP configuration, CustomBuildRuleInfo_CPP ruleInfo)
        {
            // The target-name has this form:
            //     [configuration]_CustomBuildRule_[rule-name]_[file-name]
            // For example:
            //     Release_CustomBuildRule_Splitter_TextUtils.code
            string fileName = Path.GetFileName(ruleInfo.RelativePathToFile);
            return String.Format("{0}_CustomBuildRule_{1}_{2}", configuration.Name, ruleInfo.RuleName, fileName);
        }

        /// <summary>
        /// Creates a configuration target.
        /// </summary>
        private void createConfigurationTarget(ProjectConfigurationInfo_CPP configuration)
        {
            // For example:
            //
            //   .PHONY: Debug
            //   Debug: debug/main.o debug/math.o debug/utility.o
            //       g++ debug/main.o debug/math.o debug/utility.o -o output/hello.exe

            // The target name...
            m_file.WriteLine("# Builds the {0} configuration...", configuration.Name);
            m_file.WriteLine(".PHONY: {0}", configuration.Name);

            // The targets that this target depends on...
            string dependencies = "create_folders ";

            // The object files the target depends on...
            string intermediateFolder = getIntermediateFolder(configuration);
            string objectFiles = "";
            foreach (string filename in m_projectInfo.getFiles())
            {
                string path = String.Format("{0}/{1}", intermediateFolder, filename);
                string objectPath = Path.ChangeExtension(path, ".o");
                objectFiles += (objectPath + " ");
                dependencies += (objectPath + " ");
            }

            // We add any custom build targets as dependencies...
            foreach (CustomBuildRuleInfo_CPP ruleInfo in configuration.getCustomBuildRuleInfos())
            {
                string ruleTargetName = getCustomRuleTargetName(configuration, ruleInfo);
                dependencies += (ruleTargetName + " ");
            }

            // We write the dependencies...
            m_file.WriteLine("{0}: {1}", configuration.Name, dependencies);

            // We find variables needed for the link step...
            string outputFolder = getOutputFolder(configuration);
            string implicitlyLinkedObjectFiles = String.Format("$({0})", getImplicitlyLinkedObjectsVariableName(configuration));

            // The link step...
            switch (m_projectInfo.ProjectType)
            {
                // Creates a C++ executable...
                case ProjectInfo_CPP.ProjectTypeEnum.CPP_EXECUTABLE:
                    string libraryPath = getLibraryPathVariableName(configuration);
                    string libraries = getLibrariesVariableName(configuration);
                    m_file.WriteLine("\tg++ {0} $({1}) $({2}) -Wl,-rpath,./ -o {3}/{4}.exe", objectFiles, libraryPath, libraries, outputFolder, m_projectInfo.Name);
                    break;


                // Creates a static library...
                case ProjectInfo_CPP.ProjectTypeEnum.CPP_STATIC_LIBRARY:
                    m_file.WriteLine("\tar rcs {0}/lib{1}.a {2} {3}", outputFolder, m_projectInfo.Name, objectFiles, implicitlyLinkedObjectFiles);
                    break;


                // Creates a DLL (shared-objects) library...
                case ProjectInfo_CPP.ProjectTypeEnum.CPP_DLL:
                    string dllName, pic;
                    if(MakeItSoConfig.Instance.IsCygwinBuild == true)
                    {
                        dllName = String.Format("lib{0}.dll", m_projectInfo.Name);
                        pic = "";
                    }
                    else
                    {
                        dllName = String.Format("lib{0}.so", m_projectInfo.Name);
                        pic = "-fPIC";
                    }
                
                    m_file.WriteLine("\tg++ {0} -shared -Wl,-soname,{1} -o {2}/{1} {3} {4}", pic, dllName, outputFolder, objectFiles, implicitlyLinkedObjectFiles);
                    break;
            }

            m_file.WriteLine("");
        }

        /// <summary>
        /// Creates targets to compile the files for the configuration passed in.
        /// </summary>
        private void createFileTargets(ProjectConfigurationInfo_CPP configuration)
        {
            // For example:
            //
            //   -include debug/main.d
            //   main.o: main.cpp
            //       g++ -c main.cpp [include-path] -o debug/main.o
            //       g++ -MM main.cpp [include-path] > debug/main.d

            // We find settings that aply to all files in the configuration...
            string intermediateFolder = getIntermediateFolder(configuration);
            string includePath = String.Format("$({0})", getIncludePathVariableName(configuration));
            string preprocessorDefinitions = String.Format("$({0})", getPreprocessorDefinitionsVariableName(configuration));
            string compilerFlags = String.Format("$({0})", getCompilerFlagsVariableName(configuration));

            // We write a section of the makefile to compile each file...
            foreach (string filename in m_projectInfo.getFiles())
            {
                // We work out the filename, the object filename and the 
                // dependencies filename...
                string path = String.Format("{0}/{1}", intermediateFolder, filename);
                string objectPath = Path.ChangeExtension(path, ".o");
                string dependenciesPath = Path.ChangeExtension(path, ".d");

                // We decide which compiler to use...
                string compiler = "$(CPP_COMPILER)";
                if (Path.GetExtension(filename).ToLower() == ".c")
                {
                    compiler = "$(C_COMPILER)";
                }

                // We create the target...
                m_file.WriteLine("# Compiles file {0} for the {1} configuration...", filename, configuration.Name);
                m_file.WriteLine("-include {0}", dependenciesPath);
                m_file.WriteLine("{0}: {1}", objectPath, filename);
                m_file.WriteLine("\t{0} {1} {2} -c {3} {4} -o {5}", compiler, preprocessorDefinitions, compilerFlags, filename, includePath, objectPath);
                m_file.WriteLine("\t{0} {1} {2} -MM {3} {4} > {5}", compiler, preprocessorDefinitions, compilerFlags, filename, includePath, dependenciesPath);
                m_file.WriteLine("");
            }
        }

        /// <summary>
        /// Creates a target that creates the intermediate and output folders.
        /// </summary>
        private void createCreateFoldersTarget()
        {
            m_file.WriteLine("# Creates the intermediate and output folders for each configuration...");
            m_file.WriteLine(".PHONY: create_folders");
            m_file.WriteLine("create_folders:");
            foreach (ProjectConfigurationInfo_CPP configuration in m_projectInfo.getConfigurationInfos())
            {
                string intermediateFolder = getIntermediateFolder(configuration);
                string outputFolder = getOutputFolder(configuration);
                m_file.WriteLine("\tmkdir -p {0}", intermediateFolder);
                if (outputFolder != intermediateFolder)
                {
                    m_file.WriteLine("\tmkdir -p {0}", getOutputFolder(configuration));
                }
            }
            m_file.WriteLine("");
        }

        /// <summary>
        /// Creates the 'clean' target that removes intermediate files.
        /// </summary>
        private void createCleanTarget()
        {
            m_file.WriteLine("# Cleans intermediate and output files (objects, libraries, executables)...");
            m_file.WriteLine(".PHONY: clean");
            m_file.WriteLine("clean:");
            foreach (ProjectConfigurationInfo_CPP configuration in m_projectInfo.getConfigurationInfos())
            {
                string intermediateFolder = getIntermediateFolder(configuration);
                string outputFolder = getOutputFolder(configuration);

                // Object files...
                m_file.WriteLine("\trm -f {0}/*.o", intermediateFolder);

                // Dependencies files...
                m_file.WriteLine("\trm -f {0}/*.d", intermediateFolder);

                // Static libraries...
                m_file.WriteLine("\trm -f {0}/*.a", outputFolder);

                // Shared object libraries (.so on Linux, .dll on cygwin)...
                m_file.WriteLine("\trm -f {0}/*.so", outputFolder);
                m_file.WriteLine("\trm -f {0}/*.dll", outputFolder);

                // Executables...
                m_file.WriteLine("\trm -f {0}/*.exe", outputFolder);

            }
            m_file.WriteLine("");
        }

        /// <summary>
        /// Returns the folder to use for intermediate files, such as object files.
        /// </summary>
        private string getIntermediateFolder(ProjectConfigurationInfo_CPP configuration)
        {
            string prefix = MakeItSoConfig.Instance.getProjectConfig(m_projectInfo.Name).CPPFolderPrefix;
            return Utils.addPrefixToFolderPath(configuration.IntermediateFolder, prefix);
        }

        /// <summary>
        /// Returns the folder to use for intermediate files.
        /// </summary>
        private string getOutputFolder(ProjectConfigurationInfo_CPP configuration)
        {
            string prefix = MakeItSoConfig.Instance.getProjectConfig(m_projectInfo.Name).CPPFolderPrefix;
            return Utils.addPrefixToFolderPath(configuration.OutputFolder, prefix);
        }

        #endregion

        #region Private data

        // The parsed project data that we are creating the makefile from...
        private ProjectInfo_CPP m_projectInfo = null;

        // The file we write to...
        private StreamWriter m_file = null;

        #endregion
    }
}
