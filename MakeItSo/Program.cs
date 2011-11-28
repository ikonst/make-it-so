using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MakeItSoLib;

namespace MakeItSo
{
    /// <summary>
    /// The main class.
    /// </summary>
    class Program
    {
        /// <summary>
        /// The entry point 
        /// </summary>
        static void Main(string[] args)
        {
            try
            {
                Log.clear();

                // We parse the config file and the command-line...
                MakeItSoConfig config = MakeItSoConfig.Instance;
                config.initialize(args);

                // We get the name of the .sln file to parse...
                string solutionFilename = config.SolutionFile;
                if (solutionFilename == "")
                {
                    Log.log("No solution file found.");
                    return;
                }

                // We create an object to parse the solution...
                Log.log("Parsing " + solutionFilename);
                SolutionParserBase parser = new SolutionParser_VS2008.SolutionParser();
                parser.parse(solutionFilename);
                Log.log("Parsing succeeded.");

                // We make any changes to the project that are specified in 
                // the MakeItSo.config file...
                parser.updateSolutionFromConfig();

                // We create the makefile...
                Log.log("Creating makefile...");
                MakefileBuilder.createMakefile(parser.ParsedSolution);
                Log.log("Creating makefile succeeded.");
            }
            catch (Exception ex)
            {
                Log.log("Fatal error: " + ex.Message);
            }
        }
    }
}
