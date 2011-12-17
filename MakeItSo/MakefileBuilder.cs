using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MakeItSoLib;

namespace MakeItSo
{
    /// <summary>
    /// Creates a Linux makefile from the parsed solution passed to
    /// the constuctor.
    /// </summary><remarks>
    /// This class creates a master makefile in the solution folder. This 
    /// has the standard 'Makefile' name. 
    /// 
    /// We then create a makefile for each project in the solution in the
    /// project's own folder. These have the name '[project-name].makefile'
    /// The project makefiles are created using MakefileBuilder_Project objects.
    /// 
    /// The master makefile just sets up the dependencies between the projects
    /// in the solution, and invokes each project's own makefile to build it.
    /// 
    /// We also set up a 'clean' target that calls 'make clean' on all the
    /// projects in the solution.
    /// 
    /// .PHONY targets
    /// --------------
    /// All project sections in this makefile are .PHONY targets, as we don't 
    /// know what files (if any) they produce. 
    /// 
    /// </remarks>
    class MakefileBuilder
    {
        #region Public methods and properties

        /// <summary>
        /// Creates a makefile for the solution passed in.
        /// </summary>
        public static void createMakefile(SolutionInfo solution)
        {
            new MakefileBuilder(solution);
        }

        #endregion

        #region Private functions

        /// <summary>
        /// Constructor.
        /// </summary>
        private MakefileBuilder(SolutionInfo solution)
        {
            m_solution = solution;

            // We create the 'master' solution makefile...
            createSolutionMakefile();

            // We create a makefile for each project in the solution...
            createProjectMakefiles();
        }

        /// <summary>
        /// Creates the 'master' solution makefile.
        /// </summary>
        private void createSolutionMakefile()
        {
            try
            {
                // We create the file 'Makefile', and set it to use unix-style line endings...
                string path = m_solution.RootFolderAbsolute + "/Makefile";
                m_file = new StreamWriter(path, false);
                m_file.NewLine = "\n";

                // We create an 'all projects' root target...
                createAllProjectsTarget();

                // We create one target for each project...
                createProjectTargets();

                // We create the 'clean' target, to clean all projects...
                createCleanTarget();
            }
            finally
            {
                if(m_file != null)
                {
                    m_file.Close();
                    m_file.Dispose();
                }
            }
        }

        /// <summary>
        /// We create a makefile for each project in the solution.
        /// </summary>
        private void createProjectMakefiles()
        {
            foreach (ProjectInfo project in m_solution.getProjectInfos())
            {
                // We build a different makefile, depending on the
                // project type...
                if (project is ProjectInfo_CPP)
                {
                    MakefileBuilder_Project_CPP.createMakefile(project as ProjectInfo_CPP);
                }
                if (project is ProjectInfo_CSharp)
                {
                    MakefileBuilder_Project_CSharp.createMakefile(project as ProjectInfo_CSharp);
                }
            }
        }

        /// <summary>
        /// Writes an 'all_projects' target that depends on all the 
        /// individual projects.
        /// </summary>
        private void createAllProjectsTarget()
        {
            // We create a target like:
            //   .PHONY: all_projects
            //   all_projects: [project1] [project2] [project3] 
            m_file.WriteLine("# Builds all the projects in the solution...");
            m_file.WriteLine(".PHONY: all_projects");

            string target = "all_projects: ";
            foreach (ProjectInfo project in m_solution.getProjectInfos())
            {
                target += (project.Name + " ");
            }
            m_file.WriteLine(target);
            m_file.WriteLine("");
        }


        /// <summary>
        /// Creates individual targets for each project in the solution,
        /// including their depenencies on other projects.
        /// </summary>
        private void createProjectTargets()
        {
            // We write a section for each project in the solution...
            foreach (ProjectInfo project in m_solution.getProjectInfos())
            {
                writeProjectSection(project);
            }
        }

        /// <summary>
        /// We create a 'clean' target that cleans all the projects.
        /// </summary>
        private void createCleanTarget()
        {
            m_file.WriteLine("# Cleans all projects...");
            m_file.WriteLine(".PHONY: clean");
            m_file.WriteLine("clean:");
            foreach (ProjectInfo project in m_solution.getProjectInfos())
            {
                string directory = Utils.quote(project.RootFolderRelative);
                string makefile = project.Name + ".makefile";
                m_file.WriteLine("\tmake --directory={0} --file={1} clean", directory, makefile);
            }
            m_file.WriteLine("");
        }

        /// <summary>
        /// Writes a section of the master Makefile for the project passed in.
        /// </summary>
        private void writeProjectSection(ProjectInfo project)
        {
            // We create a target like:
            //   .PHONY: [project-name]
            //   [project-name]: [required-project-1] [required-project-2]
            //       make --directory=[project-folder] -f [makefile-name]

            m_file.WriteLine("# Builds project '{0}'...", project.Name);
            m_file.WriteLine(".PHONY: {0}", project.Name);

            string dependencies = String.Format("{0}: ", project.Name);
            foreach (ProjectInfo requiredProject in project.getRequiredProjects())
            {
                dependencies += (requiredProject.Name + " ");
            }
            m_file.WriteLine(dependencies);

            string directory = Utils.quote(project.RootFolderRelative);
            string makefile = project.Name + ".makefile";
            m_file.WriteLine("\tmake --directory={0} --file={1}", directory, makefile);
            m_file.WriteLine("");
        }

        #endregion

        #region Private data

        // The parsed solution data...
        private SolutionInfo m_solution = null;

        // The file for the 'master' makefile...
        private StreamWriter m_file = null;

        #endregion
    }
}
