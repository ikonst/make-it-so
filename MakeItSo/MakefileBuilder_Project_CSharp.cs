using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MakeItSoLib;
using System.IO;
using FileInfo = MakeItSoLib.FileInfo;

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
                createTargetVariable();

                // We create an 'all configurations' root target...
                m_file.WriteLine("");
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
        /// Creates the 'clean' target that removes intermediate files.
        /// </summary>
        private void createCleanTarget()
        {
            m_file.WriteLine("# Cleans output files...");
            m_file.WriteLine(".PHONY: clean");
            m_file.WriteLine("clean:");
            foreach (ProjectConfigurationInfo_CSharp configurationInfo in m_projectInfo.getConfigurationInfos())
            {
                string outputFolder = getOutputFolderVariableName(configurationInfo);
                m_file.WriteLine("\trm -f $({0})/*.*", outputFolder);
            }
            m_file.WriteLine("");
        }

        /// <summary>
        /// Creates a target that creates the output folders, and which also
        /// copies any required references from other projects.
        /// </summary>
        private void createCreateFoldersTarget()
        {
            // We create the output folder and copy references for each 
            // configuration in the project...
            m_file.WriteLine("# Creates the output folders for each configuration, and copies references...");
            m_file.WriteLine(".PHONY: create_folders");
            m_file.WriteLine("create_folders:");
            foreach (ProjectConfigurationInfo_CSharp configurationInfo in m_projectInfo.getConfigurationInfos())
            {
                // We create the output folder...
                string outputFolderVariable = getOutputFolderVariableName(configurationInfo);
                m_file.WriteLine("\tmkdir -p $({0})", outputFolderVariable);

                // We copy any files that need to copied to the output folder...
                foreach (FileInfo fileInfo in configurationInfo.getFilesToCopyToOutputFolder())
                {
                    // We get the path to the file. If it is from a project output folder,
                    // we need to convert the name to our version of the folder name...
                    string relativePath = fileInfo.RelativePath;
                    if (fileInfo.IsFromAProjectOutputFolder == true)
                    {
                        string prefix = MakeItSoConfig.Instance.getProjectConfig(m_projectInfo.Name).CSharpFolderPrefix;
                        relativePath = Utils.addPrefixToFilePath(relativePath, prefix);
                    }

                    // We only copy the file if the source and output are not the same...
                    string folder = Path.GetDirectoryName(relativePath);
                    string outputFolder = getOutputFolder(configurationInfo);
                    if (Utils.isSamePath(folder, outputFolder) == false)
                    {
                        m_file.WriteLine("\tcp {0} $({1})", relativePath, outputFolderVariable);
                    }
                }
            }
            m_file.WriteLine("");
        }

        /// <summary>
        /// Gets the relative path to the reference. Converts the output folder to our 
        /// folder-style if it is a reference to another project in the solution.
        /// </summary>
        private string getReferenceRelativePath(ReferenceInfo referenceInfo)
        {
            switch(referenceInfo.ReferenceType)
            {
                case ReferenceInfo.ReferenceTypeEnum.EXTERNAL_REFERENCE:
                    return referenceInfo.RelativePath;

                case ReferenceInfo.ReferenceTypeEnum.PROJECT_REFERENCE:
                {
                    string prefix = MakeItSoConfig.Instance.getProjectConfig(m_projectInfo.Name).CSharpFolderPrefix;
                    return Utils.addPrefixToFilePath(referenceInfo.RelativePath, prefix);
                }
            }
            return "(reference-path-not-found)";
        }

        /// <summary>
        /// Creates a TARGET variable to hold the type of target (exe, library)
        /// that we are building.
        /// </summary>
        private void createTargetVariable()
        {
            // We work out the target-type...
            switch (m_projectInfo.ProjectType)
            {
                case ProjectInfo.ProjectTypeEnum.CSHARP_EXECUTABLE:
                    m_file.WriteLine("TARGET = exe");
                    break;

                case ProjectInfo.ProjectTypeEnum.CSHARP_LIBRARY:
                    m_file.WriteLine("TARGET = library");
                    break;

                case ProjectInfo.ProjectTypeEnum.CSHARP_WINFORMS_EXECUTABLE:
                    m_file.WriteLine("TARGET = winexe");
                    break;
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
            // We create a target for each configuration, for example:
            //
            //   .PHONY: Debug
            //   Debug: $(FILES)
            //       gmcs $(REFERENCES) $(Debug_FLAGS) -out:$(Debug_OUTPUT) $(TARGET) $(FILES)
            //
            foreach (ProjectConfigurationInfo_CSharp configurationInfo in m_projectInfo.getConfigurationInfos())
            {
                // The target name...
                m_file.WriteLine("# Builds the {0} configuration...", configurationInfo.Name);
                m_file.WriteLine(".PHONY: {0}", configurationInfo.Name);
                m_file.WriteLine("{0}: create_folders $(FILES)", configurationInfo.Name);

                // We find the variable names...
                string references = getReferencesVariableName(configurationInfo);
                string flags = getFlagsVariableName(configurationInfo);
                string outputFolder = getOutputFolderVariableName(configurationInfo);

                // The command-line to build the project...
                m_file.WriteLine("\t$(CSHARP_COMPILER) $({0}) $({1}) -out:$({2})/$(OUTPUT_FILE) -target:$(TARGET) $(FILES)", references, flags, outputFolder);

                m_file.WriteLine("");
            }
        }

        /// <summary>
        /// Creates variables for the OUTPUT_FILE for the project 
        /// and OUTPUT_FOLDER paths for each configuration.
        /// </summary>
        private void createOutputVariables()
        {
            // We create an output-file vairable...
            m_file.WriteLine("OUTPUT_FILE = " + m_projectInfo.OutputFileName);

            // We create output-folder variables for each configuration...
            foreach (ProjectConfigurationInfo_CSharp configurationInfo in m_projectInfo.getConfigurationInfos())
            {
                string variableName = getOutputFolderVariableName(configurationInfo);
                string outputPath = getOutputFolder(configurationInfo);
                m_file.WriteLine(variableName + " = " + outputPath);
            }
        }

        /// <summary>
        /// Returns the output folder for the configuration passed in.
        /// </summary>
        private string getOutputFolder(ProjectConfigurationInfo_CSharp configurationInfo)
        {
            string prefix = MakeItSoConfig.Instance.getProjectConfig(m_projectInfo.Name).CSharpFolderPrefix;
            return Utils.addPrefixToFolderPath(configurationInfo.OutputFolder, prefix);
        }

        /// <summary>
        /// Returns the variable name for this data for this configuration.
        /// </summary>
        private string getOutputFolderVariableName(ProjectConfigurationInfo_CSharp configurationInfo)
        {
            return configurationInfo.Name + "_OUTPUT_FOLDER";
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

            // We add the mono .net packages (if we are not in a cygwin build)...
            if (MakeItSoConfig.Instance.IsCygwinBuild == false)
            {
                flags += "-pkg:dotnet ";
            }

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
                    value += (getReferenceRelativePath(referenceInfo) + ",");
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
            MakeItSoConfig_Project projectConfig = MakeItSoConfig.Instance.getProjectConfig(m_projectInfo.Name);
            m_file.WriteLine("CSHARP_COMPILER = " + projectConfig.CSharpCompiler);
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
