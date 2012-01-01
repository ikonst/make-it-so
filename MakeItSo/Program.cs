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
                if(config.ConvertSolution == false)
                {
                    // Most likely because of a bad command-line, or /help reques t...
                    return;
                }

                // We get the name of the .sln file to parse...
                string solutionFilename = config.SolutionFile;
                if (solutionFilename == "")
                {
                    Log.log("No solution file found.");
                    return;
                }

                // We find the Visual Studio version, and create a parser for it...
                Log.log("Parsing " + solutionFilename);
                int version = getSolutionVersion(solutionFilename);
                SolutionParserBase parser = null;
                switch (version)
                {
                    case 10:    // VS2008
                        parser = new SolutionParser_VS2008.SolutionParser();
                        break;

                    case 11:    // VS2010
                        parser = new SolutionParser_VS2010.SolutionParser();
                        break;

                    default:
                        throw new Exception("MakeItSo does not support this version of Visual Studio");
                }
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

        /// <summary>
        /// Returns the solution version as a number, for example
        /// 10 = VS2008, 11 = VS2010,
        /// </summary>
        static int getSolutionVersion(string solutionFilename)
        {
            // One of the first lines in the solution file should look like this:
            // "Microsoft Visual Studio Solution File, Format Version 10.00"
            // We look for the integer part of the Version.
            string[] lines = File.ReadAllLines(solutionFilename);
            foreach (string line in lines)
            {
                if (line.Contains("Version") == false)
                {
                    continue;
                }
                int index = line.IndexOf("Version");
                string strVersion = line.Substring(index + 8, 2);
                int iVersion = Convert.ToInt32(strVersion);
                return iVersion;
            }

            throw new Exception("Could not find Version from the solution file");
        }
    }
}
