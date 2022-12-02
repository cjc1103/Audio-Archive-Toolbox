using System;
using System.Text.RegularExpressions;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AATB
{
    public partial class AATB_Main
    {
        static string RunProcess(string ExternalProgram, string ExternalArguments)
        {
            /* Creates a process to run an external program
             * Inputs
             *   ExternalProgram     External program to run in process
             *   ExternalArguments    String containing arguments
             * Note: Process sends normal output to StandardError stream so "RedirectStandardError"
             *   option is set to false
             * Returns:
             *   StandardOutput string from external program
             */
            Process p;
            string
                ExternalOutput = null,
                ExternalError = null;

            if (ExternalProgram != null)
            {
                p = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ExternalProgram,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = Verbose,
                        Arguments = ExternalArguments
                    }
                };
                try
                {
                    p.Start();
                    // read standard output stream
                    ExternalOutput = p.StandardOutput.ReadToEnd();
                    // read standard error stream if verbose flag is set
                    if (Verbose)
                        ExternalError = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                }
                catch (Exception e)
                {
                    Log.WriteLine(); // flush output buffer
                    Log.WriteLine("*** Fatal program exception: " + ExternalProgram);
                    if (Debug) Log.WriteLine(ExternalArguments);
                    if (Debug) Log.WriteLine(e.Message);
                    Environment.Exit(0);
                }
            }
            else
                Log.WriteLine("*** External program not specified");

            PrintOutputStream(ExternalOutput, ExternalError);
            return ExternalOutput;
        } // end RunProcess

        static void PrintOutputStream(string ExternalOutput, string ExternalError)
        {
            /* Prints output and error streams for debugging and error reporting
             * The ExternalError stream contains verbose output from external processes,
             * so this is only printed for debugging, or if the word "error" is detected
             */
            Match ErrorMatch;
            string[] DataList;

            // print ExternalOutput stream for debugging only
            // typically stream has only a single line with a CR/LF at end
            if (Debug && ExternalOutput.Length > 0)
                Log.Write("dbg: " + ExternalOutput);

            // ExternalError stream is null except in verbose mode
            // An exception will be generated if attempting to read null string'
            try
            {
                if (Verbose && ExternalError != null)
                {
                    // split stream into lines, ignoring blank lines
                    DataList = SplitDataByLine(ExternalError);
                    // parse each line in the list
                    foreach (string li in DataList)
                    {
                        // print ExternalError line for debugging, each line has a linefeed/cr
                        if (Debug)
                            Log.WriteLine("dbg: " + li);

                        // otherwise print line only if it contains the word "error" (case insensitive)
                        else
                        {
                            ErrorMatch = Regex.Match(li, @"error", RegexOptions.IgnoreCase);
                            if (ErrorMatch.Success)
                                Log.WriteLine("\n" + li);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(); // flush output buffer
                Log.WriteLine("*** Fatal program exception");
                if (Debug) Log.WriteLine(e.Message);
                Environment.Exit(0);
            }

        } // end PrintOutputStream
    }
}