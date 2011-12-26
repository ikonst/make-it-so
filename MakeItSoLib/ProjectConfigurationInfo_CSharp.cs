using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MakeItSoLib
{
    /// <summary>
    /// Holds information parsed from one configuration (Debug, Release etc)
    /// in a C# project.
    /// </summary>
    public class ProjectConfigurationInfo_CSharp
    {
        #region Public methods and properties

        /// <summary>
        /// Constructor.
        /// </summary>
        public ProjectConfigurationInfo_CSharp()
        {
            // By default, we ignore these warnings...
            m_warningsToIgnore.Add("1701");
            m_warningsToIgnore.Add("1702");
        }

        /// <summary>
        /// The project that holds this configuration.
        /// </summary>
        public ProjectInfo_CSharp ParentProjectInfo
        {
            get { return m_parentProjectInfo; }
            set { m_parentProjectInfo = value; }
        }

        /// <summary>
        /// The configuration's name.
        /// Note that we strip out any spaces in the configuration name.
        /// </summary>
        public string Name
        {
            get { return m_name; }
            set { m_name = value.Replace(" ", ""); }
        }

        /// <summary>
        /// Gets or sets whether to generate optimized code or not.
        /// </summary>
        public bool Optimize
        {
            get { return m_optimize; }
            set { m_optimize = value; }
        }

        /// <summary>
        /// Gets or sets the output folder, relative to the project's root folder.
        /// </summary>
        public string OutputFolder
        {
            get { return m_outputFolder; }
            set { m_outputFolder = value.Replace(" ", ""); }
        }

        /// <summary>
        /// Gets or sets the intermediate folder, relative to the project's root folder.
        /// </summary>
        public string IntermediateFolder
        {
            get { return m_intermediateFolder; }
            set { m_intermediateFolder = value.Replace(" ", ""); }
        }

        /// <summary>
        /// Gets or sets whether we treat warnings as errors.
        /// </summary>
        public bool ThreatWarningsAsErrors
        {
            get { return m_treatWarningsAsErrors; }
            set { m_treatWarningsAsErrors = value; }
        }

        /// <summary>
        /// Adds a defined constant, e.g. DEBUG, TRACE.
        /// </summary>
        public void addDefinedConstant(string definedConstant)
        {
            m_definedConstants.Add(definedConstant);
        }

        /// <summary>
        /// Gets the collection of defined constants (DEBUG, TRACE etc)
        /// </summary>
        public List<string> getDefinedConstants()
        {
            return m_definedConstants.ToList();
        }

        /// <summary>
        /// Gets or sets whether we generate debug symbols.
        /// </summary>
        public bool Debug
        {
            get { return m_debug; }
            set { m_debug = value; }
        }

        /// <summary>
        /// Adds a warning number (as a string) to ignore.
        /// </summary>
        public void addWarningToIgnore(string warningToIgnore)
        {
            m_warningsToIgnore.Add(warningToIgnore);
        }

        /// <summary>
        /// Returns the collection of warnings to ignore.
        /// </summary>
        public List<string> getWarningsToIgnore()
        {
            return m_warningsToIgnore.ToList();
        }

        /// <summary>
        /// Gets or sets the debug info type, e.g. "full".
        /// Empty string if we do not need debug info.
        /// </summary>
        public string DebugInfo
        {
            get { return m_debugInfo; }
            set { m_debugInfo = value; }
        }

        /// <summary>
        /// The file alignment.
        /// </summary>
        public int FileAlignment
        {
            get { return m_fileAlignment; }
            set { m_fileAlignment = value; }
        }

        /// <summary>
        /// The warning level.
        /// </summary>
        public int WarningLevel
        {
            get { return m_warningLevel; }
            set { m_warningLevel = value; }
        }

        /// <summary>
        /// Adds a reference to this configuration.
        /// </summary>
        public void addReference(ReferenceInfo referenceInfo)
        {
            m_referenceInfos.Add(referenceInfo);
        }

        /// <summary>
        /// Returns the collection of references for this configuration.
        /// </summary>
        public List<ReferenceInfo> getReferenceInfos()
        {
            return m_referenceInfos.ToList();
        }

        #endregion

        #region Private data

        // The configuration name...
        private string m_name = "";

        // The parent project...
        private ProjectInfo_CSharp m_parentProjectInfo = null;

        // Whether to generate optimized code or not...
        private bool m_optimize = false;

        // The output and intermediate folders for built objects 
        // such as libraries and executables...
        private string m_outputFolder = "";
        private string m_intermediateFolder = "";

        // Treat warnings as errors...
        private bool m_treatWarningsAsErrors = false;

        // The collection of defined constants, e.g. DEBUG, TRACE...
        private HashSet<string> m_definedConstants = new HashSet<string>();

        // Whether we generate debug symbols...
        private bool m_debug = false;

        // The type of debug info, e.g. "full"...
        private string m_debugInfo = "";

        // The collections of warnings to ignore...
        private HashSet<string> m_warningsToIgnore = new HashSet<string>();

        // The file-alignment...
        private int m_fileAlignment = 512;

        // The warning level...
        private int m_warningLevel = 4;

        // The ollectin of references for this configuration...
        private HashSet<ReferenceInfo> m_referenceInfos = new HashSet<ReferenceInfo>();

        #endregion
    }
}
