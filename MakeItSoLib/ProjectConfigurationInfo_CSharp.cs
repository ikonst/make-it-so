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

        #endregion

        #region Private data

        // The configuration name...
        private string m_name = "";

        // The parent project...
        private ProjectInfo_CSharp m_parentProjectInfo = null;

        #endregion
    }
}
