using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MakeItSoLib;
using System.IO;

namespace MakeItSo
{
    /// <summary>
    /// Creates a makefile for one C# project in the solution.
    /// </summary><remarks>
    /// We create:
    /// - One target for each configuration (e.g. Debug, Release) in the project.
    /// - One all_configurations target to build all targets.
    /// - A 'clean' target.
    /// 
    /// We create a number of variables to hold settings such as the collection
    /// of files to build, compiler flags and so on. Some of these variables (such
    /// as the list of files) will be shared across all configurations. Others
    /// (such as the flags) will be specific to a configuration.
    /// 
    /// Note on Linux vs cygwin builds
    /// ------------------------------
    /// For cygwin builds:
    /// - We use the Microsoft CSC compiler instead of mono's gmcs.
    /// - Path names need to have \\ separators.
    /// </remarks>
    class MakefileBuilder_Project_CSharp
    {
        #region Public methods and properties

        /// <summary>
        /// We create a makefile for the project passed in.
        /// </summary>
        public static void createMakefile(ProjectInfo_CSharp project)
        {
            new MakefileBuilder_Project_CSharp(project);
        }

        #endregion

        #region Private functions

        /// <summary>
        /// Constructor
        /// </summary>
        private MakefileBuilder_Project_CSharp(ProjectInfo_CSharp project)
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
                createCompilerVariable();
                createFilesVariable();
                createReferencesVariables();
                createFlagsVariables();
                createOutputVariables();

                // We create an 'all configurations' root target...
                m_file.WriteLine("");
                createAllConfigurationsTarget();

                // We create one target for each configuration...
                createConfigurationTargets();

                // We create a target to create the intermediate and output folders...
                //createCreateFoldersTarget();

                // Creates the target that cleans intermediate files...
                //createCleanTarget();
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
        /// Creates the default target, to build all configurations
        /// </summary>
        private void createAllConfigurationsTarget()
        {
            // We create a list of the configuration names...
            string strConfigurations = "";
            foreach (ProjectConfigurationInfo_CSharp configurationInfo in m_projectInfo.getConfigurationInfos())
            {
                strConfigurations += (configurationInfo.Name + " ");
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
            foreach (ProjectConfigurationInfo_CSharp configurationInfo in m_projectInfo.getConfigurationInfos())
            {
                // We create the configuration target...
                createConfigurationTarget(configurationInfo);
            }
        }

        /// <summary>
        /// Creates a configuration target.
        /// </summary>
        private void createConfigurationTarget(ProjectConfigurationInfo_CSharp configuration)
        {
            // For example:
            //
            //   .PHONY: Debug
            //   Debug: $(FILES)
            //       gmcs $(REFERENCES) $(Debug_FLAGS) -out:$(Debug_OUTPUT) -target:library $(FILES)

            // The target name...
            m_file.WriteLine("# Builds the {0} configuration...", configuration.Name);
            m_file.WriteLine(".PHONY: {0}", configuration.Name);
            m_file.WriteLine("{0}: create_folders $(FILES)", configuration.Name);

            //// We find variables needed for the link step...
            //string outputFolder = getOutputFolder(configuration);
            //string implicitlyLinkedObjectFiles = String.Format("$({0})", getImplicitlyLinkedObjectsVariableName(configuration));

            //// The link step...
            //switch (m_projectInfo.ProjectType)
            //{
            //    // Creates a C++ executable...
            //    case ProjectInfo_CPP.ProjectTypeEnum.CPP_EXECUTABLE:
            //        string libraryPath = getLibraryPathVariableName(configuration);
            //        string libraries = getLibrariesVariableName(configuration);
            //        m_file.WriteLine("\tg++ {0} $({1}) $({2}) -Wl,-rpath,./ -o {3}/{4}.exe", objectFiles, libraryPath, libraries, outputFolder, m_projectInfo.Name);
            //        break;


            //    // Creates a static library...
            //    case ProjectInfo_CPP.ProjectTypeEnum.CPP_STATIC_LIBRARY:
            //        m_file.WriteLine("\tar rcs {0}/lib{1}.a {2} {3}", outputFolder, m_projectInfo.Name, objectFiles, implicitlyLinkedObjectFiles);
            //        break;


            //    // Creates a DLL (shared-objects) library...
            //    case ProjectInfo_CPP.ProjectTypeEnum.CPP_DLL:
            //        string dllName, pic;
            //        if (MakeItSoConfig.Instance.IsCygwinBuild == true)
            //        {
            //            dllName = String.Format("lib{0}.dll", m_projectInfo.Name);
            //            pic = "";
            //        }
            //        else
            //        {
            //            dllName = String.Format("lib{0}.so", m_projectInfo.Name);
            //            pic = "-fPIC";
            //        }

            //        m_file.WriteLine("\tg++ {0} -shared -Wl,-soname,{1} -o {2}/{1} {3} {4}", pic, dllName, outputFolder, objectFiles, implicitlyLinkedObjectFiles);
            //        break;
            //}

            m_file.WriteLine("");
        }

        /// <summary>
        /// Creates variables for the OUTPUT paths for each configuration.
        /// </summary>
        private void createOutputVariables()
        {
            foreach (ProjectConfigurationInfo_CSharp configurationInfo in m_projectInfo.getConfigurationInfos())
            {
                string variableName = getOutputVariableName(configurationInfo);
                string outputPath = getOutputPath(configurationInfo);
                m_file.WriteLine(variableName + " = " + outputPath);
            }
        }

        /// <summary>
        /// Returns the output path (including the output file name) for the
        /// configuration passed in. This is relative to its own project's
        /// root folder.
        /// </summary>
        private string getOutputPath(ProjectConfigurationInfo_CSharp configurationInfo)
        {
            // We find the Windows output path, and convert it to our output
            // path, e.g. bin/Debug/MyLib.dll ==> bin/monoDebug/MyLib.dll
            string linuxOutputFolder = Utils.addPrefixToFolder(configurationInfo.OutputFolder, "mono");
            string linuxPath = linuxOutputFolder + "/" + m_projectInfo.OutputFileName;
            return linuxPath;
        }

        /// <summary>
        /// Returns the variable name for this data for this configuration.
        /// </summary>
        private string getOutputVariableName(ProjectConfigurationInfo_CSharp configurationInfo)
        {
            return configurationInfo.Name + "_OUTPUT";
        }

        /// <summary>
        /// Creates variables for the compiler FLAGS for each 
        /// configuration in the project.
        /// </summary>
        private void createFlagsVariables()
        {
            foreach (ProjectConfigurationInfo_CSharp configurationInfo in m_projectInfo.getConfigurationInfos())
            {
                createConfigurationFlagsVariable(configurationInfo);
            }
        }

        /// <summary>
        /// Creates compiler flags for the configuration passed in.
        /// </summary>
        private void createConfigurationFlagsVariable(ProjectConfigurationInfo_CSharp configurationInfo)
        {
            string variableName = getFlagsVariableName(configurationInfo);
            string flags = "";

            // Optimize...
            if (configurationInfo.Optimize == true)
            {
                flags += "-optimize+ ";
            }
            else
            {
                flags += "-optimize- ";
            }

            // Treat warnings as errors...
            if (configurationInfo.ThreatWarningsAsErrors == true)
            {
                flags += "-warnaserror+ ";
            }

            // Defined constants...
            foreach (string definedConstant in configurationInfo.getDefinedConstants())
            {
                flags += ("-define:" + definedConstant + " ");
            }

            // Debug build...
            if (configurationInfo.Debug == true)
            {
                flags += "-debug+ ";
            }

            // Type of debug info...
            if (configurationInfo.DebugInfo != "")
            {
                flags += ("-debug:" + configurationInfo.DebugInfo + " ");
            }

            // Warnings to ignore...
            List<string> warningsToIgnore = configurationInfo.getWarningsToIgnore();
            if (warningsToIgnore.Count > 0)
            {
                flags += "-nowarn:";
                foreach (string warningToIgnore in warningsToIgnore)
                {
                    flags += (warningToIgnore + ",");
                }
                flags = flags.TrimEnd(',') + " ";
            }

            // File alignment...
            flags += ("-filealign:" + configurationInfo.FileAlignment + " ");

            // Warning level...
            flags += ("-warn:" + configurationInfo.WarningLevel + " ");

            // We add the mono .net packages...
            flags += "-pkg:dotnet ";

            // We add the flags to the makefile...
            m_file.WriteLine(variableName + " = " + flags);
        }

        /// <summary>
        /// Returns the variable name for this data for this configuration.
        /// </summary>
        private string getFlagsVariableName(ProjectConfigurationInfo_CSharp configurationInfo)
        {
            return configurationInfo.Name + "_FLAGS";
        }

        /// <summary>
        /// Creates a REFERENCES variable for each configuration in the project.
        /// </summary>
        private void createReferencesVariables()
        {
            // We create one set of references for each configuration...
            foreach (ProjectConfigurationInfo_CSharp configurationInfo in m_projectInfo.getConfigurationInfos())
            {
                string variableName = getReferencesVariableName(configurationInfo);

                // The variable holds a comma-separated list of references...
                string value = "";
                List<ReferenceInfo> referenceInfos = configurationInfo.getReferenceInfos();
                if (referenceInfos.Count > 0)
                {
                    value += "-r:";
                }
                foreach (ReferenceInfo referenceInfo in referenceInfos)
                {
                    value += (referenceInfo.RelativePath + ",");
                }
                value = value.TrimEnd(',');

                m_file.WriteLine(variableName + " = " + value);
            }
        }

        /// <summary>
        /// Returns the variable name for this data for this configuration.
        /// </summary>
        private string getReferencesVariableName(ProjectConfigurationInfo_CSharp configurationInfo)
        {
            return configurationInfo.Name + "_REFERENCES";
        }

        /// <summary>
        /// Creates the FILES variable, with the list of files to compile.
        /// </summary>
        private void createFilesVariable()
        {
            string files = "";
            foreach (string file in m_projectInfo.getFiles())
            {
                string tmpFile = file;
                if (MakeItSoConfig.Instance.IsCygwinBuild == true)
                {
                    // For a cygwin build, we seem to need path separators to be 
                    // double backslashes. (Not sure why they need to be double - maybe
                    // some sort of escaping thing?)
                    tmpFile = tmpFile.Replace("/", @"\\");
                }
                files += (tmpFile + " ");
            }
            m_file.WriteLine("FILES = " + files.ToString());
        }

        /// <summary>
        /// Creates the COMPILER variable, which specifies which compiler to use.
        /// </summary>
        private void createCompilerVariable()
        {
            if (MakeItSoConfig.Instance.IsCygwinBuild == true)
            {
                // We are creating a cygwin build...
                m_file.WriteLine("COMPILER = /cygdrive/c/Windows/Microsoft.NET/Framework/v3.5/Csc.exe");
            }
            else
            {
                // We are creating a mono build...
                m_file.WriteLine("COMPILER = gmcs");
            }
        }

        #endregion

        #region Private data

        // The parsed project data that we are creating the makefile from...
        private ProjectInfo_CSharp m_projectInfo = null;

        // The file we write to...
        private StreamWriter m_file = null;

        #endregion
    }
}
