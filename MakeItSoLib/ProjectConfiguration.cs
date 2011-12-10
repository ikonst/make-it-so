using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MakeItSoLib
{
    /// <summary>
    /// Holds details about one configuration of a project,
    /// such as a release or debug build.
    /// </summary>
    public class ProjectConfiguration
    {
        #region Public methods and properties

        /// <summary>
        /// The project that holds this configuration.
        /// </summary>
        public Project ParentProject
        {
            get { return m_parentProject; }
            set { m_parentProject = value; }
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
        /// Gets or sets the intermediate folder, relative to the project's
        /// root folder.
        /// </summary><remarks>
        /// The intermediate folder is where object files and other 'intermediate' 
        /// parts of the build go. It may not be the same as the output folder.
        /// </remarks>
        public string IntermediateFolder
        {
            get { return m_intermediateFolder; }
            set { m_intermediateFolder = value.Replace(" ", ""); }
        }

        /// <summary>
        /// Returns the absolute path to the intermediate folder.
        /// </summary>
        public string IntermediateFolderAbsolute
        {
            get { return Path.Combine(ParentProject.RootFolderAbsolute, IntermediateFolder); }
        }

        /// <summary>
        /// Gets or sets the output folder, relative to the project's root
        /// folder.
        /// </summary>
        public string OutputFolder
        {
            get { return m_outputFolder; }
            set { m_outputFolder = value.Replace(" ", ""); }
        }

        /// <summary>
        /// Returns the absolute path to the output folder.
        /// </summary>
        public string OutputFolderAbsolute
        {
            get { return Path.Combine(ParentProject.RootFolderAbsolute, OutputFolder); }
        }

        /// <summary>
        /// Gets the collection of include paths, relative to the 
        /// project's root folder.
        /// </summary>
        public List<string> getIncludePaths()
        {
            return m_includePaths;
        }

        /// <summary>
        /// Adds the path passed in to the include path.
        /// </summary>
        public void addIncludePath(string path)
        {
            // We add to the include-path if the path passed in is
            // not already part of it...
            if (m_includePaths.Contains(path) == false)
            {
                m_includePaths.Add(path);
            }
        }

        /// <summary>
        /// Removes the include path from the collection we hold.
        /// </summary>
        public void removeIncludePath(string path)
        {
            m_includePaths.Remove(path);
        }

        /// <summary>
        /// Gets the collection of library paths, relative to the 
        /// project's root folder.
        /// </summary>
        public List<string> getLibraryPaths()
        {
            return m_libraryPaths;
        }

        /// <summary>
        /// Adds the path passed in to the library path.
        /// </summary>
        public void addLibraryPath(string path)
        {
            path = Utils.removeSpacesFromFolder(path);

            // We add to the library-path if the path passed in is
            // not already part of it...
            if (m_libraryPaths.Contains(path) == false)
            {
                m_libraryPaths.Add(path);
            }
        }

        /// <summary>
        /// Removes the library path from the collection we hold.
        /// </summary>
        public void removeLibraryPath(string path)
        {
            m_libraryPaths.Remove(path);
        }

        /// <summary>
        /// Gets the collection of libraries to link into the project.
        /// </summary>
        public HashSet<string> getLibraryRawNames()
        {
            return m_libraryRawNames;
        }

        /// <summary>
        /// Adds a library to the configuration.
        /// (Raw name is, for example, 'Math' rather than 'Math.lib' or 'libMath.a')
        /// </summary>
        public void addLibraryRawName(string rawName)
        {
            rawName = rawName.Replace(" ", "");
            m_libraryRawNames.Add(rawName);
        }

        /// <summary>
        /// Removes the library passed in from the collection we hold.
        /// </summary>
        public void removeLibraryRawName(string rawName)
        {
            m_libraryRawNames.Remove(rawName);
        }

        /// <summary>
        /// Gets the collection of include preprocessor definitions.
        /// </summary>
        public HashSet<string> getPreprocessorDefinitions()
        {
            return m_preprocessorDefinitions;
        }

        /// <summary>
        /// Adds a preprocessor definition to the configuation.
        /// </summary>
        public void addPreprocessorDefinition(string definition)
        {
            m_preprocessorDefinitions.Add(definition);
        }

        /// <summary>
        /// Remove a preprocessor definition from the collection we hold.
        /// </summary>
        public void removePreprocessorDefinition(string definition)
        {
            m_preprocessorDefinitions.Remove(definition);
        }

        /// <summary>
        /// Returns the collection of implicitly-linked object files.
        /// </summary>
        public HashSet<string> getImplicitlyLinkedObjectFiles()
        {
            return m_implicitlyLinkedObjectFiles;
        }

        /// <summary>
        /// Adds an ojbect-file to the collection.
        /// </summary>
        public void addImplicitlyLinkedObjectFile(string objectFile)
        {
            m_implicitlyLinkedObjectFiles.Add(objectFile);
        }

        /// <summary>
        /// Adds a compiler flag.
        /// </summary>
        public void addCompilerFlag(string flag)
        {
            m_compilerFlags.Add(flag);
        }

        /// <summary>
        /// Removes the flag passed from the collection we're managing.
        /// </summary>
        public void removeCompilerFlag(string flag)
        {
            m_compilerFlags.Remove(flag);
        }

        /// <summary>
        /// Gets the collection of compiler flags.
        /// </summary>
        public HashSet<string> getCompilerFlags()
        {
            return m_compilerFlags; 
        }

        #endregion

        #region Private data

        // The configuration name, e.g. 'Debug'
        private string m_name = "";

        // The project that this configuration is part of...
        private Project m_parentProject = null;

        // The folder for intermediate objects, such as object files...
        private string m_intermediateFolder = "";

        // The output folder for built objects such as libraries and executables...
        private string m_outputFolder = "";
        
        // The collection of include paths. (These are a list, as the order
        // may be important.)
        private List<string> m_includePaths = new List<string>();

        // The collection of library paths. (These are a list, as the order
        // may be important.)
        private List<string> m_libraryPaths = new List<string>();

        // The collection of objects files to link into a library if the library
        // is set to link library dependencies...
        private HashSet<string> m_implicitlyLinkedObjectFiles = new HashSet<string>();

        // The collection of preprocessor definitions for this configuration...
        private HashSet<string> m_preprocessorDefinitions = new HashSet<string>();

        // The collection of libraries to link into this configuration.
        // These are held as 'raw names', e.g. 'Math' instead of 'Math.lib' or 'libMath.a'
        private HashSet<string> m_libraryRawNames = new HashSet<string>();

        // The collection of compliler flags...
        private HashSet<string> m_compilerFlags = new HashSet<string>();

        #endregion
    }
}
