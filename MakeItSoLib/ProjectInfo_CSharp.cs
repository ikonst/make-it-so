using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MakeItSoLib
{
    /// <summary>
    /// Holds information parsed from one C# project in the solution.
    /// </summary><remarks>
    /// 
    /// Note on project references vs configuration references
    /// ------------------------------------------------------
    /// When we create makefiles, we use reference info that is stored
    /// in the configurations, rather than here at the project level. But
    /// when projects are first parsed, we store the references in the 
    /// project-info first. This is because Visual Studio only gives us
    /// reference info at the project level, but for references that are
    /// set up to other projects in the solution, we need to use different
    /// references for the Release and Debug configurations.
    /// 
    /// We use the reference-info that we hold here to try to work out which 
    /// references are to other projects in the solution and which are 'external'
    /// references. We also discard any core .NET references, as makefiles will
    /// be built wth the mono -pkg:dotnet option.
    /// 
    /// </remarks>
    public class ProjectInfo_CSharp : ProjectInfo
    {
        #region Public methods and properties

        /// <summary>
        /// Adds a source file to the project.
        /// </summary>
        public void addFile(string file)
        {
            if (MakeItSoConfig.Instance.IsCygwinBuild == true)
            {
                // For a cygwin build, we seem to need path separators to be 
                // double backslashes. (Not sure why they need to be double - maybe
                // some sort of escaping thing?)
                file = file.Replace("/", @"\\");
            }
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
        /// Gets or sets the output file name.
        /// </summary>
        public string OutputFileName
        {
            get { return m_outputFileName; }
            set { m_outputFileName = value; }
        }

        /// <summary>
        /// Adds a cofiguration to the collection for this project.
        /// </summary>
        public void addConfigurationInfo(ProjectConfigurationInfo_CSharp configurationInfo)
        {
            m_configurationInfos.Add(configurationInfo);
        }

        /// <summary>
        /// Returns the collection of configurations for the project.
        /// </summary>
        public List<ProjectConfigurationInfo_CSharp> getConfigurationInfos()
        {
            return m_configurationInfos;
        }

        /// <summary>
        /// Adds a reference to the project.
        /// </summary>
        public void addReference(string fullPath, bool copyLocal)
        {
            // We do not add references to the core .NET assmeblies,
            // as they will be added to our mono projects with the 
            // -pkg:dotnet option.
            string filename = Path.GetFileName(fullPath);
            if (filename.StartsWith("System.") == true
                ||
                filename == "mscorlib.dll")
            {
                return;
            }

            // We add the reference to the project. (But see the
            // heading-comment notes)...
            ReferenceInfo referenceInfo = new ReferenceInfo();
            referenceInfo.AbsolutePath = fullPath;
            referenceInfo.CopyLocal = copyLocal;
            m_referenceInfos.Add(referenceInfo);
        }

        /// <summary>
        /// Returns the collection of references.
        /// </summary>
        public List<ReferenceInfo> getReferences()
        {
            return m_referenceInfos.ToList();
        }

        /// <summary>
        /// Sets up references.
        /// </summary>
        public void setupReferences()
        {
            // Here we do the 'second-pass' at setting up the references. 
            // This is called after all projects have been parsed. In particular
            // we are doing two main things:
            // - Trying to work out if references are 'project references'
            //   ie, to other projects in the solution, or whether they are
            //   'external references'.
            // - Setting up references per configuration, rather than per project,
            //   as they may be different.

            // Here's what we do:
            // 1. For each reference, we check if its path is the output path
            //    of one of the other projects. 
            //    1a. If so, we have found a 'project reference'
            //    1b. If not, we have found an 'external reference'
            //
            // 2. For external references, we find the relative path to the 
            //    assembly and add it to each configuration.
            //
            // 3. For project references, we want to try to link our configurations
            //    to the equvalent configurations in the other project. For example, 
            //    to link our Release build to its Release build.
            //    3a. For each configuration in this project we find the equivalent
            //        configuration in the other project.
            //    3b. We find the relative path to the other configuration's output 
            //        folder, and set theat as the reference.

            // We loop through the references
            foreach (ReferenceInfo referenceInfo in m_referenceInfos)
            {
                setupReference(referenceInfo);
            }
        }

        #endregion

        #region Private functions

        /// <summary>
        /// We set up the reference in all configurations of this project.
        /// (See notes in setupReferences above.)
        /// </summary>
        private void setupReference(ReferenceInfo referenceInfo)
        {
            ProjectInfo_CSharp projectReference = findProjectReference(referenceInfo);
        }

        /// <summary>
        /// Checks if the reference-info passed in comes from another project
        /// in the solution.
        /// Returns the referenced project if there is one, or null if the
        /// reference is not to another project in the solution.
        /// </summary>
        private ProjectInfo_CSharp findProjectReference(ReferenceInfo referenceInfo)
        {
            ProjectInfo_CSharp result = null;

            // We look through each project...
            foreach (ProjectInfo projectInfo in ParentSolution.getProjectInfos())
            {
                // We are only interested in C# projects...
                ProjectInfo_CSharp csProjectInfo = projectInfo as ProjectInfo_CSharp;
                if (csProjectInfo == null)
                {
                    continue;
                }

                // We check each configuration for the project...
                foreach (ProjectConfigurationInfo_CSharp configurationInfo in csProjectInfo.getConfigurationInfos())
                {
                    // We find the absolute path to the output for this configuration...
                    string fullPath = csProjectInfo.RootFolderAbsolute + "/" + configurationInfo.OutputFolder + "/" + csProjectInfo.OutputFileName;
                    
                    // And we check if the reference passed points to the same assembly...
                    if (Utils.isSamePath(fullPath, referenceInfo.AbsolutePath) == true)
                    {
                        // We've found a match, so we return the project that this
                        // configuration is a part of, as the reference appears to
                        // be a 'project reference' to this project...
                        return csProjectInfo;
                    }
                }
            }

            return result;
        }

        #endregion

        #region Private data

        // The collection of source files in the project...
        protected HashSet<string> m_files = new HashSet<string>();

        // The output file name...
        private string m_outputFileName = "";

        // The collection of references for the project (see note in header comment)...
        private HashSet<ReferenceInfo> m_referenceInfos = new HashSet<ReferenceInfo>();

        // The collection of configurations (Debug, Release etc) for this project...
        private List<ProjectConfigurationInfo_CSharp> m_configurationInfos = new List<ProjectConfigurationInfo_CSharp>();

        #endregion
    }
}
