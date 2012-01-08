using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace MakeItSoLib
{
    /// <summary>
    /// Holds MakeItSo's config for a specific project.
    /// </summary>
    public class MakeItSoConfig_Project
    {
        #region Public methods and properties

        /// <summary>
        /// Constructor
        /// </summary>
        public MakeItSoConfig_Project(MakeItSoConfig solutionConfig)
        {
            m_solutionConfig = solutionConfig;
        }

        /// <summary>
        /// Gets the 'parent' solution config.
        /// </summary>
        public MakeItSoConfig SolutionConfig
        {
            get { return m_solutionConfig; }
        }

        /// <summary>
        /// Returns true if the library passed in is in the to-remove collection.
        /// </summary>
        public bool libraryShouldBeRemoved(string libraryName)
        {
            return m_librariesToRemove.Contains(libraryName.ToLower());
        }

        /// <summary>
        /// Returns true if the library-path passed in is one that
        /// needs to be removed from the project.
        /// </summary>
        public bool libraryPathShouldBeRemoved(string fullLibraryPath)
        {
            // We make sure the path is formatted in the same way as the
            // stored paths so that we can check if we've got it in the
            // collection of libraries to remove...
            fullLibraryPath = Path.GetFullPath(fullLibraryPath);
            fullLibraryPath = fullLibraryPath.ToLower();
            return m_libraryPathsToRemove.Contains(fullLibraryPath);
        }

        /// <summary>
        /// Returns true if the include-path passed in is one that
        /// needs to be removed from the project.
        /// </summary>
        public bool includePathShouldBeRemoved(string fullIncludePath)
        {
            // We make sure the path is formatted in the same way as the
            // stored paths so that we can check if we've go it in the
            // collection of libraries to remove...
            fullIncludePath = Path.GetFullPath(fullIncludePath);
            fullIncludePath = fullIncludePath.ToLower();
            return m_includePathsToRemove.Contains(fullIncludePath);
        }

        /// <summary>
        /// Returns true if the preprocessor-definition passed in should
        /// be removed from the project.
        /// </summary>
        public bool preprocessorDefinitionShouldBeRemoved(string definition)
        {
            return m_preprocessorDefinitionsToRemove.Contains(definition);
        }

        /// <summary>
        /// Returns true if the compiler flag passed in should
        /// be removed from the project.
        /// </summary>
        public bool compilerFlagShouldBeRemoved(string flag)
        {
            return m_compilerFlagsToRemove.Contains(flag);
        }

        /// <summary>
        /// Returns the config for the configuration passed in.
        /// </summary>
        public MakeItSoConfig_Configuration getConfiguration(string configurationName)
        {
            if (m_configurations.ContainsKey(configurationName) == false)
            {
                m_configurations.Add(configurationName, new MakeItSoConfig_Configuration(this));
            }
            return m_configurations[configurationName];
        }

        /// <summary>
        /// Gets the C# compiler to use when building this project.
        /// </summary>
        public string CSharpCompiler
        {
            get 
            {
                if (m_solutionConfig.IsCygwinBuild == true)
                {
                    return m_cygwinCSharpCompiler;
                }
                else
                {
                    return m_csharpCompiler;
                }
            }
        }

        /// <summary>
        /// Gets the C compiler to use when building this project.
        /// </summary>
        public string CCompiler
        {
            get { return m_cCompiler; }
        }

        /// <summary>
        /// Gets the C++ compiler to use when building this project.
        /// </summary>
        public string CPPCompiler
        {
            get { return m_cppCompiler; }
        }

        /// <summary>
        /// Gets whether we should convert this project to a shared-objects
        /// library if it is a static library.
        /// </summary>
        public bool ConvertStaticLibraryToSharedObjects
        {
            get { return m_convertStaticLibraryToSharedObjects; }
        }

        /// <summary>
        /// Parses the config file to read config for this project.
        /// </summary>
        public void parseConfig(XmlNode configNode)
        {
            parseConfig_Libraries(configNode);
            parseConfig_LibraryPaths(configNode);
            parseConfig_IncludePaths(configNode);
            parseConfig_PreprocessorDefinitions(configNode);
            parseConfig_CompilerFlags(configNode);
            parseConfig_Compilers(configNode);
            parseConfig_Misc(configNode);
        }

        #endregion

        #region Private functions

        /// <summary>
        /// Parses the config for miscellaneous settings.
        /// </summary>
        private void parseConfig_Misc(XmlNode configNode)
        {
            // We look for the setting to convert static libraries to shared-objects...
            XmlNode convertNode = configNode.SelectSingleNode("ConvertStaticLibraryToSharedObjects");
            if (convertNode != null)
            {
                XmlAttribute convertAttribute = convertNode.Attributes["convert"];
                if (convertAttribute != null)
                {
                    m_convertStaticLibraryToSharedObjects = (convertAttribute.Value == "true");
                }
            }
        }

        /// <summary>
        /// Parses the config file for which compilers to use.
        /// </summary>
        private void parseConfig_Compilers(XmlNode configNode)
        {
            findCompiler(configNode, "CPPCompiler", ref m_cppCompiler);
            findCompiler(configNode, "CCompiler", ref m_cCompiler);
            findCompiler(configNode, "CSharpCompiler", ref m_csharpCompiler);
            findCompiler(configNode, "CSharpCompiler", ref m_cygwinCSharpCompiler);
        }

        /// <summary>
        /// Finds a compiler variable from the config file and sets a member
        /// variable from it.
        /// </summary>
        private void findCompiler(XmlNode configNode, string compilerName, ref string memberVariable)
        {
            XmlNode compilerNode = configNode.SelectSingleNode(compilerName);
            if (compilerNode != null)
            {
                XmlAttribute compilerAttribute = compilerNode.Attributes["compiler"];
                if (compilerAttribute != null)
                {
                    memberVariable = compilerAttribute.Value;
                }
            }
        }

        /// <summary>
        /// Parses the config file for compiler flags to be added or removed
        /// for this project.
        /// </summary>
        private void parseConfig_CompilerFlags(XmlNode configNode)
        {
            // We find 'RemoveCompilerFlag' nodes...
            XmlNodeList removeCompilerFlagNodes = configNode.SelectNodes("RemoveCompilerFlag");
            foreach (XmlNode removeCompilerFlagNode in removeCompilerFlagNodes)
            {
                XmlAttribute flagAttribute = removeCompilerFlagNode.Attributes["flag"];
                if (flagAttribute == null) continue;
                m_compilerFlagsToRemove.Add(flagAttribute.Value);
            }

            // We find 'AddCompilerFlag' nodes...
            XmlNodeList addCompilerFlagNodes = configNode.SelectNodes("AddCompilerFlag");
            foreach (XmlNode addCompilerFlagNode in addCompilerFlagNodes)
            {
                XmlAttribute configurationAttribute = addCompilerFlagNode.Attributes["configuration"];
                XmlAttribute flagAttribute = addCompilerFlagNode.Attributes["flag"];
                if (flagAttribute == null || configurationAttribute == null) continue;
                getConfiguration(configurationAttribute.Value).addCompilerFlagToAdd(flagAttribute.Value);
            }
        }

        /// <summary>
        /// Parses the config file for preprocessor definitions to be added or removed
        /// for this project.
        /// </summary>
        private void parseConfig_PreprocessorDefinitions(XmlNode configNode)
        {
            // We find 'RemovePreprocessorDefinition' nodes...
            XmlNodeList removePreprocessorDefinitionNodes = configNode.SelectNodes("RemovePreprocessorDefinition");
            foreach (XmlNode removePreprocessorDefinitionNode in removePreprocessorDefinitionNodes)
            {
                XmlAttribute definitionAttribute = removePreprocessorDefinitionNode.Attributes["definition"];
                if (definitionAttribute == null) continue;
                m_preprocessorDefinitionsToRemove.Add(definitionAttribute.Value);
            }

            // We find 'AddPreprocessorDefinition' nodes...
            XmlNodeList addPreprocessorDefinitionNodes = configNode.SelectNodes("AddPreprocessorDefinition");
            foreach (XmlNode addPreprocessorDefinitionNode in addPreprocessorDefinitionNodes)
            {
                XmlAttribute configurationAttribute = addPreprocessorDefinitionNode.Attributes["configuration"];
                XmlAttribute definitionAttribute = addPreprocessorDefinitionNode.Attributes["definition"];
                if (definitionAttribute == null || configurationAttribute == null) continue;
                getConfiguration(configurationAttribute.Value).addPreprocessorDefinitionToAdd(definitionAttribute.Value);
            }
        }

        /// <summary>
        /// Parses the config file for library paths to be added or removed
        /// for this project.
        /// </summary>
        private void parseConfig_LibraryPaths(XmlNode configNode)
        {
            // We find 'RemoveLibraryPath' nodes...
            XmlNodeList removeLibraryPathNodes = configNode.SelectNodes("RemoveLibraryPath");
            foreach (XmlNode removeLibraryPathNode in removeLibraryPathNodes)
            {
                XmlAttribute pathAttribute = removeLibraryPathNode.Attributes["path"];
                if (pathAttribute == null) continue;
                addLibraryPathToRemove(pathAttribute.Value);
            }

            // We find 'AddLibraryPath' nodes...
            XmlNodeList addLibraryPathNodes = configNode.SelectNodes("AddLibraryPath");
            foreach (XmlNode addLibraryPathNode in addLibraryPathNodes)
            {
                XmlAttribute configurationAttribute = addLibraryPathNode.Attributes["configuration"];
                XmlAttribute pathAttribute = addLibraryPathNode.Attributes["path"];
                if (pathAttribute == null || configurationAttribute == null) continue;
                getConfiguration(configurationAttribute.Value).addLibraryPathToAdd(pathAttribute.Value);
            }
        }

        /// <summary>
        /// Parses the config file for include paths to be added or removed
        /// for this project.
        /// </summary>
        private void parseConfig_IncludePaths(XmlNode configNode)
        {
            // We find 'RemoveIncludePath' nodes...
            XmlNodeList removeIncludePathNodes = configNode.SelectNodes("RemoveIncludePath");
            foreach (XmlNode removeIncludePathNode in removeIncludePathNodes)
            {
                XmlAttribute pathAttribute = removeIncludePathNode.Attributes["path"];
                if (pathAttribute == null) continue;
                addIncludePathToRemove(pathAttribute.Value);
            }

            // We find 'AddIncludePath' nodes...
            XmlNodeList addIncludePathNodes = configNode.SelectNodes("AddIncludePath");
            foreach (XmlNode addIncludePathNode in addIncludePathNodes)
            {
                XmlAttribute configurationAttribute = addIncludePathNode.Attributes["configuration"];
                XmlAttribute pathAttribute = addIncludePathNode.Attributes["path"];
                if (pathAttribute == null || configurationAttribute == null) continue;
                getConfiguration(configurationAttribute.Value).addIncludePathToAdd(pathAttribute.Value);
            }
        }

        /// <summary>
        /// Parses the config file for libraries to be added or removed
        /// for this project.
        /// </summary>
        private void parseConfig_Libraries(XmlNode configNode)
        {
            // We find 'RemoveLibrary' nodes...
            XmlNodeList removeLibraryNodes = configNode.SelectNodes("RemoveLibrary");
            foreach (XmlNode removeLibraryNode in removeLibraryNodes)
            {
                XmlAttribute libraryAttribute = removeLibraryNode.Attributes["library"];
                if (libraryAttribute == null) continue;
                addLibraryToRemove(libraryAttribute.Value);
            }

            // We find 'AddLibrary' nodes...
            XmlNodeList addLibraryNodes = configNode.SelectNodes("AddLibrary");
            foreach (XmlNode addLibraryNode in addLibraryNodes)
            {
                XmlAttribute configurationAttribute = addLibraryNode.Attributes["configuration"];
                XmlAttribute libraryAttribute = addLibraryNode.Attributes["library"];
                if (libraryAttribute == null || configurationAttribute == null) continue;
                getConfiguration(configurationAttribute.Value).addLibraryToAdd(libraryAttribute.Value);
            }
        }

        /// <summary>
        /// Adds the library passed in to the list to remove from the
        /// project we're holding config for.
        /// </summary>
        private void addLibraryToRemove(string libraryName)
        {
            string libraryRawName = Path.GetFileNameWithoutExtension(libraryName);
            m_librariesToRemove.Add(libraryRawName.ToLower());
        }

        /// <summary>
        /// Adds the library-path passed in to the list to remove from the
        /// project we're holding config for.
        /// </summary>
        private void addLibraryPathToRemove(string libraryPath)
        {
            // We store the absolute path. (This makes paths easier to compare later.)
            string absolutePath = Path.Combine(SolutionConfig.SolutionRootFolder, libraryPath);
            absolutePath = Path.GetFullPath(absolutePath);
            absolutePath = absolutePath.ToLower();
            m_libraryPathsToRemove.Add(absolutePath);
        }

        /// <summary>
        /// Adds the include-path passed in to the list to remove from the
        /// project we're holding config for.
        /// </summary>
        private void addIncludePathToRemove(string includePath)
        {
            // We store the absolute path. (This makes paths easier to compare later.)
            string absolutePath = Path.Combine(SolutionConfig.SolutionRootFolder, includePath);
            absolutePath = Path.GetFullPath(absolutePath);
            absolutePath = absolutePath.ToLower();
            m_includePathsToRemove.Add(absolutePath);
        }

        #endregion

        #region Private data

        // The 'parent' config...
        private MakeItSoConfig m_solutionConfig = null;

        // Collection of libraries to remove...
        private HashSet<string> m_librariesToRemove = new HashSet<string>();

        // Collection of library paths to remove. These are stored as full
        // paths and in lower-case, to make it easier to comapre them...
        private HashSet<string> m_libraryPathsToRemove = new HashSet<string>();

        // Collection of include paths to remove. These are stored as full
        // paths and in lower-case, to make it easier to comapre them...
        private HashSet<string> m_includePathsToRemove = new HashSet<string>();

        // Collection of preprocessor-definitions to remove...
        private HashSet<string> m_preprocessorDefinitionsToRemove = new HashSet<string>();

        // Collection of compiler flags to remove...
        private HashSet<string> m_compilerFlagsToRemove = new HashSet<string>();

        // Config for specific configurations (debug, release) in this project as a map of:
        // Configuration-name => config for the configuration
        private Dictionary<string, MakeItSoConfig_Configuration> m_configurations = new Dictionary<string, MakeItSoConfig_Configuration>();

        // The C# compiler to use when building this project...
        private string m_csharpCompiler = "gmcs";
        private string m_cygwinCSharpCompiler = "/cygdrive/c/Windows/Microsoft.NET/Framework/v3.5/Csc.exe";

        // The C compiler to use when building this project...
        private string m_cCompiler = "gcc";

        // The C++ compiler to use when building this project...
        private string m_cppCompiler = "g++";

        // Whether we will convert static libraries to shared-objects libraries
        // for this project...
        private bool m_convertStaticLibraryToSharedObjects = false;

        #endregion
    }
}
