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
        /// Gets or sets the absolute path to the reference.
        /// </summary>
        public string AbsolutePath { get; set; }

        /// <summary>
        /// Gets or sets whether the referenced assembly should be copied to
        /// the project's output folder.
        /// </summary>
        public bool CopyLocal { get; set; }

    }
}
