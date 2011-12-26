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

                // We create an 'all configurations' root target...
                //createAllConfigurationsTarget();

                // We create one target for each configuration...
                //createConfigurationTargets();

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
        /// Creates variables for the compiler flags for each 
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
