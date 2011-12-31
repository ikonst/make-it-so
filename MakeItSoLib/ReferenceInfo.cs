using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MakeItSoLib
{
    /// <summary>
    /// Holds information about one reference for a C# project.
    /// </summary>
    public class ReferenceInfo
    {
        /// <summary>
        /// Creates a copy of the data.
        /// </summary>
        public ReferenceInfo clone()
        {
            ReferenceInfo result = new ReferenceInfo();
            result.AbsolutePath = AbsolutePath;
            result.ConfigurationInfo = ConfigurationInfo;
            result.CopyLocal = CopyLocal;
            result.ReferenceType = ReferenceType;
            result.RelativePath = RelativePath;
            return result;
        }

        /// <summary>
        /// The reference type.
        /// </summary>
        public enum ReferenceTypeEnum
        {
            // The default (unset) value...
            INVALID,

            // A reference to another project in the solution...
            PROJECT_REFERENCE,

            // A reference to an 'external', ie an assembly not build
            // by this solution...
            EXTERNAL_REFERENCE
        }

        /// <summary>
        /// Gets or sets the absolute path to the reference.
        /// </summary>
        public string AbsolutePath { get; set; }

        /// <summary>
        /// Gets or sets the relative path to the reference.
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// Gets or sets whether the referenced assembly should be copied to
        /// the project's output folder.
        /// </summary>
        public bool CopyLocal { get; set; }

        /// <summary>
        /// Gets or sets the type of the reference (project reference or
        /// external reference).
        /// </summary>
        public ReferenceTypeEnum ReferenceType
        {
            get { return m_referenceType; }
            set { m_referenceType = value; }
        }

        /// <summary>
        /// Gets or sets the project-configuration that this reference points to
        /// if it is a project-reference.
        /// </summary><remarks>
        /// We use this when working out which files to copy into the output folder,
        /// as we need to copy files from referenced projects as well as from 'our
        /// own' project.
        /// </remarks>
        public ProjectConfigurationInfo_CSharp ConfigurationInfo { get; set; }

        // The type of reference, e.g. project-reference or external-reference...
        private ReferenceTypeEnum m_referenceType = ReferenceTypeEnum.INVALID;
    }
}
