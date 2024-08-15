using System;
using System.Text.RegularExpressions;
using System.Diagnostics;

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
             * Note: Some external programs like flac send normal output to StandardError stream,
             *   so "RedirectStandardError" is toggled with the Verbose flag, which is disabled
             *   by default. If additional output is needed the user can enable Verbose mode.
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
            bool ErrorFound = false;

            // print ExternalOutput stream for debugging or verbose output modes
            // typically stream has only a single line with a CR/LF at end
            if ((Debug || Verbose) && ExternalOutput.Length > 0)
                Log.Write("dbg: External Output: " + ExternalOutput);

            // ExternalError stream is null except in verbose mode
            // An exception will be generated if attempting to read null string'
            try
            {
                if (Verbose && ExternalError != null)
                {
                    // split stream into lines, ignoring blank lines
                    DataList = ExternalError.Split(LineDelimeters, StringSplitOptions.RemoveEmptyEntries);
                    // parse each line in the list
                    foreach (string li in DataList)
                    {
                        // print ExternalError line for debugging
                        // flush output buffer first so error message starts on a new line
                        if (Debug)
                            Log.Write("\n" + li);

                        // otherwise print line only if it contains "error" or "fail" (case insensitive)
                        // flush output buffer first so error message starts on a new line
                        else
                        {
                            ErrorMatch = Regex.Match(li, @"error|fail", RegexOptions.IgnoreCase);
                            if (ErrorMatch.Success)
                            {
                                ErrorFound = true;
                                Log.Write("\n" + li);
                            }
                        }
                    }
                    if (Debug || ErrorFound)
                        // flush output buffer
                        Log.WriteLine();
                }
            }
            catch (Exception e)
            {
                // flush output buffer, write exception message
                Log.WriteLine();
                Log.WriteLine("*** " + e.Message);
            }

        } // end PrintOutputStream
    }
}