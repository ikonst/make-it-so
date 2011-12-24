using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE80;
using EnvDTE;
using System.IO;
using Microsoft.VisualStudio.VCProjectEngine;
using MakeItSoLib;
using Solution = MakeItSoLib.SolutionInfo;
using VSLangProj80;

namespace SolutionParser_VS2008
{
    /// <summary>
    /// Parses a Visual Studio solution.
    /// </summary><remarks>
    /// When we parse a solution file we are looking for:
    /// - C++ projects
    /// - The source files in each project, e.g. *.c, *.cpp
    /// - The dependencies between projects
    /// 
    /// We use the EnvDTE COM libraries to automate access to Visual Studio.
    /// We will create an instance of Visual Studio (devenv.exe), load a 
    /// specified solution, and use the EnvDTE libraries to query its properties.
    /// </remarks>
    public class SolutionParser : SolutionParserBase
    {
        #region Public methods and properties

        /// <summary>
        /// Parses the solution passed in.
        /// </summary>
        public override void parse(string solutionFilename)
        {
            try
            {
                m_parsedSolution.Name = solutionFilename;

                // We create the COM automation objects to open the solution...
                openSolution();

                // We get the root collection of projects and parse them.
                EnvDTE.Projects rootProjects = Utils.call(() => (m_dteSolution.Projects));
                parseProjects(rootProjects);

                // We find the dependencies between projects...
                parseDependencies();
            }
            catch (Exception ex)
            {
                // There was an error parsing this solution...
                string message = String.Format("Failed to parse solution {0} [{1}].", solutionFilename, ex.Message);
                throw new Exception(message);
            }
            finally
            {
                // We always quit the DTE object, to make sure that the instance
                // of Visual Studio we are automating is closed down...
                if (m_dte != null)
                {
                    m_dte.Quit();
                }
            }
        }

        #endregion

        #region Private functions

        /// <summary>
        /// Creates COM automation objects and opens the solution...
        /// </summary>
        private void openSolution()
        {
            // We create a DTE object to automate our interaction
            // with Visual Studio.
            Type type = Type.GetTypeFromProgID("VisualStudio.DTE.9.0");
            Object obj = System.Activator.CreateInstance(type, true);
            m_dte = (DTE2)obj;

            // We open the solution. (This needs to be a full path.)
            string path = Path.GetFullPath(m_parsedSolution.Name);
            m_dteSolution = m_dte.Solution;
            m_dteSolution.Open(path);

            // We get the root folder for the solution...
            m_parsedSolution.RootFolderAbsolute = Path.GetDirectoryName(path) + "\\";
        }

        /// <summary>
        /// Parses projects in the collection of projects passed in.
        /// </summary>
        private void parseProjects(EnvDTE.Projects projects)
        {
            // We parse each project in the collection.
            // Note that this may end up recursing back into this function, 
            // as there may be projects nested in other projects...
            int numProjects = Utils.call(() => (projects.Count));
            for (int i = 1; i <= numProjects; ++i)
            {
                EnvDTE.Project project = Utils.call(() => (projects.Item(i)));
                parseProject(project);
            }
        }

        /// <summary>
        /// Parses the project passed in.
        /// </summary>
        private void parseProject(EnvDTE.Project project)
        {
            // We get the project name...
            string projectName = Utils.call(() => (project.Name));

            // We check if this project is a kind we know how to convert...
            string strProjectType = Utils.call(() => (project.Kind));
            ProjectType eProjectType = convertProjectTypeToEnum(strProjectType);
            switch (eProjectType)
            {
                // It's a C++ project...
                case ProjectType.CPP_PROJECT:
                {
                    // We get the Visual Studio project, parse it and store the 
                    // parsed project in our collection of results...
                    VCProject vcProject = Utils.call(() => (project.Object as VCProject));
                    ProjectParser_CPP parser = new ProjectParser_CPP(vcProject, m_parsedSolution.RootFolderAbsolute);
                    m_parsedSolution.addProjectInfo(projectName, parser.Project);
                }
                break;

                // It's a C# project...
                case ProjectType.CSHARP_PROJECT:
                {
                    // We get the Visual Studio project, parse it and store the 
                    // parsed project in our collection of results...
                    VSProject2 vsProject = Utils.call(() => (project.Object as VSProject2));
                    ProjectParser_CSharp parser = new ProjectParser_CSharp(vsProject, m_parsedSolution.RootFolderAbsolute);
                    m_parsedSolution.addProjectInfo(projectName, parser.Project);
                }
                break;
            }

            // We parse the project's items, to check whether there are any nested
            // projects...
            EnvDTE.ProjectItems projectItems = Utils.call(() => (project.ProjectItems));
            parseProjectItems(projectItems);
        }

        /// <summary>
        /// Parses a collection of project-items checking for sub-projects.
        /// </summary><remarks>
        /// Project items can be things like files and folders, or sub-projects.
        /// So when we parse a project, we need to drill into the items as there
        /// may be projects nested inside folders etc.
        /// </remarks>
        private void parseProjectItems(EnvDTE.ProjectItems projectItems)
        {
            // We look through the items...
            int numProjectItems = Utils.call(() => (projectItems.Count));
            for (int i = 1; i <= numProjectItems; ++i)
            {
                EnvDTE.ProjectItem projectItem = Utils.call(() => (projectItems.Item(i)));

                // We see if the item itself has sub-items...
                EnvDTE.ProjectItems subItems = Utils.call(() => (projectItem.ProjectItems));
                if (subItems != null)
                {
                    parseProjectItems(subItems);
                }

                // We see if this item has a sub-project...
                EnvDTE.Project subProject = Utils.call(() => (projectItem.SubProject));
                if (subProject != null)
                {
                    parseProject(subProject);
                }
            }
        }

        /// <summary>
        /// Finds the dependencies between the projects in this solution.
        /// </summary><remarks>
        /// The dependencies are stored in the Solution.SolutionBuild.BuildDependencies
        /// object rather than in the projects themselves. So we need to parse them
        /// here rather than in the project parser.
        /// </remarks>
        private void parseDependencies()
        {
            // The dependencies for each project are stored in a BuildDependency object.
            // This holds a reference to the project, and references to all the projects
            // that it depends on (called 'required projects').

            // We get the Solution.SolutionBuild.BuildDependencies object...
            SolutionBuild solutionBuild = Utils.call(() => (m_dteSolution.SolutionBuild));
            BuildDependencies buildDependencies = Utils.call(() => (solutionBuild.BuildDependencies));

            // We loop through the 'BuildDependencies'. Each one of these holds dependency 
            // information for one project...
            int numBuildDependencies = Utils.call(() => (buildDependencies.Count));
            for (int i = 1; i <= numBuildDependencies; ++i)
            {
                BuildDependency buildDependency = Utils.call(() => (buildDependencies.Item(i)));

                // We get the project's name...
                EnvDTE.Project project = Utils.call(() => (buildDependency.Project));
                string projectName = Utils.call(() => (project.Name));

                // We loop through the required-projects, getting the name of each one...
                object[] requiredProjects = Utils.call(() => (buildDependency.RequiredProjects as object[]));
                int numRequiredProjects = requiredProjects.Length;
                for (int j = 0; j < numRequiredProjects; ++j)
                {
                    EnvDTE.Project requiredProject = requiredProjects[j] as EnvDTE.Project;
                    string requiredProjectName = Utils.call(() => (requiredProject.Name));

                    // We store the dependency with the parsed project...
                    m_parsedSolution.addRequiredProjectToProject(projectName, requiredProjectName);
                }
            }

            // We set up any implicit linking that needs to be done because 
            // of project dependencies (this may be needed for C++ projects)...
            m_parsedSolution.setupImplicitLinking();

            // We set up references for C# projects...
            m_parsedSolution.setupReferences();
        }

        /// <summary>
        /// Converts a project type string to an enum.
        /// </summary>
        private ProjectType convertProjectTypeToEnum(string projectType)
        {
            ProjectType result = ProjectType.UNKNOWN;

            // The project type is a GUID representing a Visual Studio 
            // project type...
            switch (projectType)
            {
                case "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}":
                    result = ProjectType.CPP_PROJECT;
                    break;

                case "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}":
                    result = ProjectType.CSHARP_PROJECT;
                    break;
            }

            return result;
        }

        #endregion

        #region Private data

        // The DTE object that we use to automate Visual Studio...
        private DTE2 m_dte = null;

        // The automation object representing the solution...
        private EnvDTE.Solution m_dteSolution = null;

        // Types of Visual Studio project that we know how to parse
        // and convert...
        private enum ProjectType
        {
            UNKNOWN,
            CPP_PROJECT,
            CSHARP_PROJECT
        }

        #endregion
    }
}


