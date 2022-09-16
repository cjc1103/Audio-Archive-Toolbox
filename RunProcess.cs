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
                        RedirectStandardError = true,
                        Arguments = ExternalArguments
                    }
                };
                try
                {
                    p.Start();
                    ExternalOutput = p.StandardOutput.ReadToEnd();
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
            // prints output stream if the word "error" is detected
            Match ErrorMatch;
            string[] DataList;
            bool ErrorFound = false;

            // print output stream for debugging only
            if (Debug)
                Log.WriteLine(ExternalOutput);
            // split error stream up into an array
            DataList = ExternalError.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            // parse error stream
            foreach (string li in DataList)
            {
                if (Verbose)
                    Log.WriteLine(li);
                else
                {
                    // print line if it contains the word "error" (case insensitive)
                    ErrorMatch = Regex.Match(li, "error", RegexOptions.IgnoreCase);
                    if (ErrorMatch.Success
                        && li.Length > 0)
                    {
                        ErrorFound = true;
                        Log.Write("\n" + li);
                    }
                }
            }
            // log line feed if any error found
            if (ErrorFound) Log.WriteLine();
        } // end PrintOutputStream
    }
}